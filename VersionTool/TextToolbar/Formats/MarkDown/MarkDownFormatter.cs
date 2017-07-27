﻿// ******************************************************************
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THE CODE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH
// THE CODE OR THE USE OR OTHER DEALINGS IN THE CODE.
// ******************************************************************

using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Linq;
using TextToolbarTemp.TextToolbarButtons;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace TextToolbarTemp.TextToolbarFormats.MarkDown
{
    public class MarkDownFormatter : Formatter
    {
        public MarkDownFormatter(TextToolbar model)
            : base(model)
        {
            ButtonActions = new MarkDownButtonActions(this);
        }

        public void StyleHeader(ToolbarButton button)
        {
            var list = new ListBox { Margin = new Thickness(0), Padding = new Thickness(0) };
            headerFlyout = new Flyout { Content = list };

            string headerVal = "#";
            for (int i = 1; i <= 5; i++)
            {
                string val = string.Concat(Enumerable.Repeat(headerVal, i));
                var item = new ListBoxItem
                {
                    Content = new MarkdownTextBlock
                    {
                        Text = val + Model.Labels.HeaderLabel,
                        IsTextSelectionEnabled = false
                    },
                    Tag = val,
                    Padding = new Thickness(5, 2, 5, 2),
                    Margin = new Thickness(0)
                };
                item.Tapped += HeaderSelected;
                list.Items.Add(item);
            }

            headerFlyout.ShowAt(button);
        }

        private void HeaderSelected(object sender, TappedRoutedEventArgs e)
        {
            var item = sender as FrameworkElement;
            EnsureAtNewLine();
            SetSelection(item.Tag as string, string.Empty, false);
            headerFlyout?.Hide();
        }

        public void FormatCode(ToolbarButton button)
        {
            if (DetermineSimpleReverse("`", "`"))
            {
                return;
            }
            else if (!Selected.Text.Contains(Return))
            {
                SetSelection("`", "`");
            }
            else
            {
                Func<string> codeLines = () =>
                {
                    return ListLineIterator == 1 || ReachedEndLine ? "```" : string.Empty;
                };

                SetList(codeLines, button, wrapNewLines: true, enableToggle: false);
            }
        }

        public void FormatQuote(ToolbarButton button)
        {
            SetList(() => "> ", button);
        }

        /// <summary>
        /// Applies formatting to Selected Text, or Removes formatting if already applied.
        /// </summary>
        /// <param name="start">Formatting in front of Text</param>
        /// <param name="end">Formatting at end of Text</param>
        /// <param name="reversible">Is the Text reversible?</param>
        /// <param name="contents">Text to insert between Start and End (Overwrites Current Text)</param>
        public virtual void SetSelection(string start, string end, bool reversible = true, string contents = null)
        {
            if (Model.Editor == null)
            {
                return;
            }

            if (!reversible || !DetermineSimpleReverse(start, end))
            {
                int originalStartPos = Selected.StartPosition;

                string originalText = contents ?? Selected.Text;

                if (!string.IsNullOrWhiteSpace(originalText) && originalText.Last() == Return.First())
                {
                    originalText = originalText.Remove(originalText.Length - 1, 1);
                }

                Selected.Text = start + originalText + end;

                if (string.IsNullOrWhiteSpace(originalText))
                {
                    Selected.StartPosition = originalStartPos + start.Length;
                    Selected.EndPosition = Selected.StartPosition;
                }
                else
                {
                    Selected.StartPosition = originalStartPos + start.Length;
                    Selected.EndPosition = Selected.StartPosition + originalText.Length;
                }
            }
        }

        /// <summary>
        /// Determines if formatting is to be reversed
        /// </summary>
        /// <param name="start">Formatting in front of Text</param>
        /// <param name="end">Formatting at end of Text</param>
        /// <returns>True if formatting is reversing, otherwise false</returns>
        protected virtual bool DetermineSimpleReverse(string start, string end)
        {
            if (!DetermineSimpleInlineReverse(start, end))
            {
                try
                {
                    int startpos = Selected.StartPosition - start.Length;
                    int endpos = Selected.EndPosition;

                    string text = string.Empty;
                    Model.Editor.Document.GetText(TextGetOptions.NoHidden, out text);
                    if (text.Substring(startpos, start.Length) == start)
                    {
                        string endofstring = text.Substring(endpos, end.Length);
                        if (endofstring == end)
                        {
                            text = text.Remove(startpos, start.Length);
                            endpos -= start.Length;
                            Model.Editor.Document.SetText(TextSetOptions.None, text.Remove(endpos, end.Length));

                            Selected.StartPosition = startpos;
                            Selected.EndPosition = endpos;
                            return true;
                        }
                    }
                }
                catch
                {
                }

                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Determines if formatting is to be reversed, when the formatting is located inside the Selectors.
        /// </summary>
        /// <param name="start">Formatting in front of Text</param>
        /// <param name="end">Formatting at end of Text</param>
        /// <returns>True if formatting is reversing, otherwise false</returns>
        protected virtual bool DetermineSimpleInlineReverse(string start, string end)
        {
            try
            {
                if (Selected.Text.Substring(0, start.Length) == start)
                {
                    if (Selected.Text.Substring(Selected.Text.Length - end.Length, end.Length) == end)
                    {
                        Selected.Text = Selected.Text.Substring(start.Length, Selected.Text.Length - end.Length - start.Length);
                        return true;
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        /// <summary>
        /// Iterates a new line char if an enter was pressed.
        /// </summary>
        /// <param name="button">Button linked to List Event</param>
        /// <param name="listChar">Line Char function.</param>
        private void SetListTextChanged(ToolbarButton button, Func<string> listChar)
        {
            Selected.StartPosition -= 1;
            var lastEntered = Selected.Text;
            Selected.StartPosition += 1;

            if (Model.LastKeyPress == Windows.System.VirtualKey.Back)
            {
                var indexer = listChar();
                var line = GetLastLine();

                if (!line.StartsWith(indexer))
                {
                    button.IsToggled = false;
                }
            }

            if (lastEntered == Return)
            {
                ListLineIterator++;
                Selected.Text += listChar();

                Selected.StartPosition = Selected.EndPosition;
            }
        }

        /// <summary>
        ///  This function will either add List Characters to lines of text, or Remove List Characters from Lines of Text, if already applied.
        /// </summary>
        /// <param name="listChar">A function for generating a List Character, use ListLineIterator to generate a Numbered Style List, or return a string Result, e.g. () => "- "</param>
        /// <param name="button">Button that activated the Set List</param>
        /// <param name="wrapNewLines">Adds New Lines to Start and End of Selected Text</param>
        /// <param name="enableToggle">Is this a Toggleable element?</param>
        public virtual void SetList(Func<string> listChar, ToolbarButton button, bool wrapNewLines = false, bool enableToggle = true)
        {
            if (Model.Editor == null)
            {
                return;
            }
            else if (enableToggle)
            {
                if (button.TextChangedEvent == null)
                {
                    button.TextChangedEvent = new RoutedEventHandler((s, e) => SetListTextChanged(button, listChar));
                }

                if (!button.IsToggled)
                {
                    button.IsToggled = true;
                }
                else
                {
                    button.IsToggled = false;
                    return;
                }
            }

            ListLineIterator = 1;
            ReachedEndLine = false;
            if (!DetermineListReverse(listChar, wrapNewLines))
            {
                ListLineIterator = 1;
                ReachedEndLine = false;

                EnsureAtNewLine();
                string text = listChar();

                var lines = Selected.Text.Split(new string[] { Return }, StringSplitOptions.None).ToList();
                if (!wrapNewLines)
                {
                    lines.RemoveAt(lines.Count - 1); // remove last escape as selected end of last line
                }
                else
                {
                    lines.Insert(0, string.Empty);
                }

                for (int i = 0; i < lines.Count; i++)
                {
                    ListLineIterator++;

                    var element = lines[i];
                    text += element;

                    ReachedEndLine = i + 1 >= lines.Count;

                    if (lines.Count > 1 && !ReachedEndLine)
                    {
                        text += Return;
                    }

                    if (!ReachedEndLine || wrapNewLines)
                    {
                        text += listChar();

                        if (ReachedEndLine)
                        {
                            text += Return;
                        }
                    }
                }

                Selected.Text = text;

                if (!lines.Any(line => !string.IsNullOrWhiteSpace(line)))
                {
                    Selected.StartPosition = Selected.EndPosition;
                }
            }
        }

        /// <summary>
        /// Determines whether a List already has the formatting applied.
        /// </summary>
        /// <param name="listChar">Function to generate the List Character</param>
        /// <param name="wrapNewLines">Adds New Lines to Start and End of Selected Text</param>
        /// <returns>True if List formatting is reversing, otherwise false</returns>
        protected virtual bool DetermineListReverse(Func<string> listChar, bool wrapNewLines)
        {
            if (string.IsNullOrWhiteSpace(Selected.Text))
            {
                return false;
            }

            if (wrapNewLines && DetermineInlineWrapListReverse(listChar))
            {
                return true;
            }

            string text = string.Empty;
            int startpos = Selected.StartPosition;

            var lines = Selected.Text.Split(new string[] { Return }, StringSplitOptions.None).ToList();
            if (wrapNewLines)
            {
                lines.RemoveAt(lines.Count - 1); // removes the line kept from Wrapping with NewLines.
            }

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];

                ReachedEndLine = i + 1 >= lines.Count;
                string listchar = listChar();

                try
                {
                    if (line.Substring(0, listchar.Length) == listchar)
                    {
                        text += line.Remove(0, listchar.Length);
                        if (lines.Count > 1)
                        {
                            text += Return;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                catch
                {
                    return false;
                }

                ListLineIterator++;
            }

            if (wrapNewLines)
            {
                text = text.Remove(0, 1);
                text = text.Remove(text.Length - 1);
            }

            Selected.Text = text;
            return true;
        }

        /// <summary>
        /// Determines if a reverse is requested, if the list characters are inside the Selection.
        /// </summary>
        /// <param name="listChar">List character generating function</param>
        /// <returns>Is it reversing?</returns>
        protected virtual bool DetermineInlineWrapListReverse(Func<string> listChar)
        {
            try
            {
                ListLineIterator = 1;
                string start = listChar();
                int startpos = Selected.StartPosition - start.Length - 1; // removing newline char as well

                ReachedEndLine = true;
                string end = listChar();

                string text = string.Empty;
                Model.Editor.Document.GetText(TextGetOptions.None, out text);

                string startText = text.Substring(startpos, start.Length);
                if (startText == start)
                {
                    string endText = text.Substring(Selected.EndPosition + end.Length - 3, end.Length);
                    if (endText == end)
                    {
                        return true; // works if Line Chars are only on the first and last lines, this would need to check all the other line chars for other NewLine Wrap methods that change the line char for all lines.
                    }
                }
            }
            catch
            {
            }

            ListLineIterator = 1;
            ReachedEndLine = false;

            return false;
        }

        internal string OrderedListIterate()
        {
            return ListLineIterator + ". ";
        }

        public override string Text
        {
            get
            {
                string currentvalue = string.Empty;
                Model.Editor.Document.GetText(TextGetOptions.UseCrlf, out currentvalue);
                return currentvalue.Replace('\n', '\r'); // Converts CRLF into double Return for Markdown new line.
            }
        }

        public override ButtonMap DefaultButtons
        {
            get
            {
                ListButton = ListButton ?? Model.CommonButtons.List;
                OrderedListButton = OrderedListButton ?? Model.CommonButtons.OrderedList;
                QuoteButton = new ToolbarButton
                {
                    Name = TextToolbar.QuoteElement,
                    ToolTip = Model.Labels.QuoteLabel,
                    Icon = new SymbolIcon { Symbol = Symbol.Message },
                    Activation = FormatQuote
                };

                return new ButtonMap
                {
                    Model.CommonButtons.Bold,
                    Model.CommonButtons.Italics,
                    Model.CommonButtons.Strikethrough,

                    new ToolbarSeparator(),

                    new ToolbarButton
                    {
                        Name = TextToolbar.HeadersElement,
                        Icon = new SymbolIcon { Symbol = Symbol.FontSize },
                        ToolTip = Model.Labels.HeaderLabel,
                        Activation = StyleHeader
                    },
                    new ToolbarButton
                    {
                        Name = TextToolbar.CodeElement,
                        ToolTip = Model.Labels.CodeLabel,
                        Icon = new FontIcon { Glyph = "{}", FontFamily = new FontFamily("Segoe UI"), Margin = new Thickness(0, -5, 0, 0) },
                        Activation = FormatCode
                    },
                    QuoteButton,

                    Model.CommonButtons.Link,

                    new ToolbarSeparator(),

                    ListButton,
                    OrderedListButton
                };
            }
        }

        /// <summary>
        /// Gets the value of the Line Number Iterator. Use this for generating Numbered Lists.
        /// </summary>
        public int ListLineIterator { get; internal set; } = 1;

        /// <summary>
        /// Gets a value indicating whether gets whether it is the last line of the list.
        /// </summary>
        public bool ReachedEndLine { get; private set; } = false;

        internal ToolbarButton QuoteButton { get; set; }

        internal ToolbarButton ListButton { get; set; }

        internal ToolbarButton OrderedListButton { get; set; }

        private Flyout headerFlyout;
    }
}
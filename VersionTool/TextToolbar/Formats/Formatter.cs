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

namespace TextToolbarTemp.TextToolbarFormats
{
    using System;
    using TextToolbarTemp.TextToolbarButtons;
    using Windows.UI.Text;

    /// <summary>
    /// Manipulates Selected Text into an applied format according to default buttons.
    /// </summary>
    public abstract class Formatter
    {
        public Formatter(TextToolbar model)
        {
            Model = model;

            // Waits for the Editor to be realised.
            var editorFetch = model.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                Model.Editor.SelectionChanged += Editor_SelectionChanged;
            });
        }

        /// <summary>
        /// Shortcut to Carriage Return
        /// </summary>
        protected const string Return = "\r";

        /// <summary>
        /// Called for Changes to Selction (Requires unhook if switching RichEditBox).
        /// </summary>
        /// <param name="sender">Editor</param>
        /// <param name="e">Args</param>
        private void Editor_SelectionChanged(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            OnSelectionChanged();
        }

        /// <summary>
        /// Determines the Position of the Selector, if not at a New Line, it will move the Selector to a new line.
        /// </summary>
        public virtual void EnsureAtNewLine()
        {
            int val = Selected.StartPosition;
            int counter = 0;
            bool atNewLine = false;

            string docText = string.Empty;
            Model.Editor.Document.GetText(TextGetOptions.NoHidden, out docText);
            var lines = docText.Split(new string[] { Return }, StringSplitOptions.None);

            foreach (var line in lines)
            {
                if (counter == val)
                {
                    atNewLine = true;
                }

                foreach (var c in line)
                {
                    counter++;
                    if (counter >= val)
                    {
                        break;
                    }
                }

                counter++;
            }

            if (!atNewLine)
            {
                bool selectionEmpty = string.IsNullOrWhiteSpace(Selected.Text);
                Selected.Text = Selected.Text.Insert(0, Return);
                Selected.StartPosition += 1;

                if (selectionEmpty)
                {
                    Selected.EndPosition = Selected.StartPosition;
                }
            }
        }

        /// <summary>
        /// Gets an array of the Lines of Text in the Editor.
        /// </summary>
        /// <returns>Text Array</returns>
        public virtual string[] GetLines()
        {
            string doc;
            Model.Editor.Document.GetText(TextGetOptions.None, out doc);
            var lines = doc.Split(new string[] { Return }, StringSplitOptions.None);
            return lines;
        }

        /// <summary>
        /// Gets the line from the index provided (Skips last Carriage Return)
        /// </summary>
        /// <returns>Last line text</returns>
        public virtual string GetLine(int index)
        {
            return GetLines()[index];
        }

        /// <summary>
        /// Gets the last line (Skips last Carriage Return)
        /// </summary>
        /// <returns>Last line text</returns>
        public virtual string GetLastLine()
        {
            var lines = GetLines();
            return lines[lines.Length - 2];
        }

        /// <summary>
        /// Called after the Selected Text changes.
        /// </summary>
        public virtual void OnSelectionChanged()
        {
        }

        /// <summary>
        /// Gets the source Toolbar
        /// </summary>
        public TextToolbar Model { get; }

        /// <summary>
        /// Gets or sets a map of the Actions taken when a button is pressed. Required for Common Button actions (Unless you override both Activation and ShiftActivation)
        /// </summary>
        public ButtonActions ButtonActions { get; protected set; }

        /// <summary>
        /// Gets the default list of buttons
        /// </summary>
        public abstract ButtonMap DefaultButtons { get; }

        /// <summary>
        /// Gets the formatted version of the Editor's Text
        /// </summary>
        public abstract string Text { get; }

        /// <summary>
        /// Gets the current Editor Selection
        /// </summary>
        public ITextSelection Selected
        {
            get { return Model.Editor.Document.Selection; }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.VisualStudio.Text.Editor;

namespace Vim.UI.Wpf
{
    /// <summary>
    /// Displays a menu of previously yanked / deleted values at the caret of an
    /// ITextView, similar to the Visual Studio clipboard ring (Ctrl+Shift+V) menu.
    /// Entries can be chosen with the mouse, the arrow keys or by pressing the
    /// displayed number
    /// </summary>
    public static class PasteHistoryMenu
    {
        internal const int MaxPreviewLength = 60;

        /// <summary>
        /// Create the single line preview which is displayed in the menu for an entry
        /// </summary>
        internal static string CreatePreviewText(string text)
        {
            var trimmed = text.TrimStart();
            var preview = trimmed;
            var isShortened = false;

            var newLineIndex = trimmed.IndexOfAny(new[] { '\r', '\n' });
            if (newLineIndex >= 0)
            {
                preview = trimmed.Substring(0, newLineIndex);
                isShortened = true;
            }

            if (preview.Length > MaxPreviewLength)
            {
                preview = preview.Substring(0, MaxPreviewLength);
                isShortened = true;
            }

            if (preview.Length == 0)
            {
                return "<whitespace>";
            }

            return isShortened ? preview + "..." : preview;
        }

        /// <summary>
        /// Get the single character access key used to quickly select the entry at the
        /// specified index, or null when the index is beyond the quick select range
        /// </summary>
        internal static string GetAccessKey(int index)
        {
            if (index < 9)
            {
                return (index + 1).ToString();
            }

            if (index == 9)
            {
                return "0";
            }

            return null;
        }

        /// <summary>
        /// Show the menu at the caret of the given ITextView.  The onSelected callback
        /// is invoked with the index of the chosen entry.  Nothing is invoked when the
        /// menu is dismissed
        /// </summary>
        public static void Show(IWpfTextView textView, IReadOnlyList<string> entries, Action<int> onSelected)
        {
            if (entries.Count == 0)
            {
                return;
            }

            var menu = new ContextMenu
            {
                PlacementTarget = textView.VisualElement,
                Placement = PlacementMode.Relative,
            };

            try
            {
                var caret = textView.Caret;
                menu.HorizontalOffset = caret.Left - textView.ViewportLeft;
                menu.VerticalOffset = caret.Bottom - textView.ViewportTop;
            }
            catch (InvalidOperationException)
            {
                // The caret position isn't available before the view has a layout.
                // Let the menu display at the default position
            }

            for (var i = 0; i < entries.Count; i++)
            {
                var index = i;
                var preview = CreatePreviewText(entries[i]).Replace("_", "__");
                var accessKey = GetAccessKey(i);
                var header = accessKey != null
                    ? string.Format("_{0}  {1}", accessKey, preview)
                    : preview;

                var menuItem = new MenuItem { Header = header };
                menuItem.Click += (sender, e) => onSelected(index);
                menu.Items.Add(menuItem);
            }

            menu.Opened += (sender, e) =>
            {
                if (menu.Items.Count > 0 && menu.Items[0] is MenuItem firstItem)
                {
                    firstItem.Focus();
                }
            };

            // Make sure focus goes back to the editor when the menu goes away
            menu.Closed += (sender, e) => Keyboard.Focus(textView.VisualElement);

            menu.IsOpen = true;
        }
    }
}

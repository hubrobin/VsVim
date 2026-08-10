using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Editor;

namespace Vim.UI.Wpf
{
    /// <summary>
    /// The set of colors used to draw the paste history menu.  Typically provided by
    /// the host so the menu matches the host theme
    /// </summary>
    public sealed class PasteHistoryMenuColors
    {
        public Color Background { get; set; }
        public Color Foreground { get; set; }
        public Color Border { get; set; }
        public Color HighlightBackground { get; set; }
        public Color HighlightForeground { get; set; }
    }

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

        private static SolidColorBrush CreateBrush(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        /// <summary>
        /// Create the style used for the menu items.  The default WPF MenuItem control
        /// template hard codes its highlight visuals so a simple replacement template
        /// is used to ensure the themed highlight colors are rendered
        /// </summary>
        private static Style CreateMenuItemStyle(PasteHistoryMenuColors colors)
        {
            var foregroundBrush = CreateBrush(colors.Foreground);
            var highlightBrush = CreateBrush(colors.HighlightBackground);
            var highlightTextBrush = CreateBrush(colors.HighlightForeground);

            var borderFactory = new FrameworkElementFactory(typeof(Border), "Bd");
            borderFactory.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            borderFactory.SetValue(Border.PaddingProperty, new Thickness(24, 4, 24, 4));
            borderFactory.SetValue(Border.SnapsToDevicePixelsProperty, true);

            var contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentFactory.SetValue(ContentPresenter.ContentSourceProperty, "Header");
            contentFactory.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);
            borderFactory.AppendChild(contentFactory);

            var template = new ControlTemplate(typeof(MenuItem))
            {
                VisualTree = borderFactory,
            };

            var highlightTrigger = new Trigger
            {
                Property = MenuItem.IsHighlightedProperty,
                Value = true,
            };
            highlightTrigger.Setters.Add(new Setter(Border.BackgroundProperty, highlightBrush, "Bd"));
            highlightTrigger.Setters.Add(new Setter(Control.ForegroundProperty, highlightTextBrush));
            template.Triggers.Add(highlightTrigger);

            var style = new Style(typeof(MenuItem));
            style.Setters.Add(new Setter(Control.ForegroundProperty, foregroundBrush));
            style.Setters.Add(new Setter(Control.TemplateProperty, template));
            return style;
        }

        private static void ApplyMenuColors(ContextMenu menu, PasteHistoryMenuColors colors)
        {
            menu.Background = CreateBrush(colors.Background);
            menu.Foreground = CreateBrush(colors.Foreground);
            menu.BorderBrush = CreateBrush(colors.Border);
            menu.BorderThickness = new Thickness(1);
            menu.Padding = new Thickness(0, 2, 0, 2);
        }

        /// <summary>
        /// Show the menu at the caret of the given ITextView.  The onSelected callback
        /// is invoked with the index of the chosen entry.  Nothing is invoked when the
        /// menu is dismissed
        /// </summary>
        public static void Show(IWpfTextView textView, IReadOnlyList<string> entries, Action<int> onSelected, PasteHistoryMenuColors colors = null)
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

            var itemStyle = colors != null ? CreateMenuItemStyle(colors) : null;
            if (colors != null)
            {
                ApplyMenuColors(menu, colors);
            }

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
                if (itemStyle != null)
                {
                    menuItem.Style = itemStyle;
                }

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

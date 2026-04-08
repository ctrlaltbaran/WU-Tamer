using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace Wu_change.Views
{
    /// <summary>
    /// A themed Yes/No/Cancel dialog that matches the app's current dark or light mode.
    /// Replaces MessageBox.Show() so the popup respects the app theme.
    /// </summary>
    public class ThemedMessageBox : Window
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        private MessageBoxResult _result = MessageBoxResult.Cancel;

        /// <summary>
        /// Show a themed Yes/No/Cancel dialog. Returns the user's choice.
        /// </summary>
        public static MessageBoxResult Show(string message, string title, bool isDarkMode)
        {
            var dlg = new ThemedMessageBox(message, title, isDarkMode)
            {
                Owner = Application.Current.MainWindow
            };
            dlg.ShowDialog();
            return dlg._result;
        }

        private ThemedMessageBox(string message, string title, bool isDarkMode)
        {
            Title = title;
            Width = 420;
            SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ShowInTaskbar = false;

            // Load the matching theme so all DynamicResource keys resolve
            var themeDict = new ResourceDictionary
            {
                Source = new Uri(
                    isDarkMode ? "Themes/Dark.xaml" : "Themes/Light.xaml",
                    UriKind.Relative)
            };
            Resources.MergedDictionaries.Add(themeDict);

            // Apply dark/light title bar via DWM
            SourceInitialized += (s, e) =>
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                int value = isDarkMode ? 1 : 0;
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
            };

            // Window background from theme
            SetResourceReference(BackgroundProperty, "BgPrimary");

            // ── Layout ──────────────────────────────────────────────────
            var root = new Grid { Margin = new Thickness(28, 24, 28, 24) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // message
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) }); // spacer
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // buttons

            // Icon + message
            var msgPanel = new StackPanel { Orientation = Orientation.Horizontal };

            var icon = new TextBlock
            {
                Text = "💾",
                FontSize = 30,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 16, 0)
            };

            var msg = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                MaxWidth = 310,
                VerticalAlignment = VerticalAlignment.Center
            };
            msg.SetResourceReference(ForegroundProperty, "FgPrimary");

            msgPanel.Children.Add(icon);
            msgPanel.Children.Add(msg);
            Grid.SetRow(msgPanel, 0);
            root.Children.Add(msgPanel);

            // Buttons — use custom template so WPF's default blue ButtonChrome never shows
            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var yesBtn    = MakeButton("Yes",    accent: true,  isDarkMode);
            var noBtn     = MakeButton("No",     accent: false, isDarkMode);
            var cancelBtn = MakeButton("Cancel", accent: false, isDarkMode);

            yesBtn.Click    += (s, e) => { _result = MessageBoxResult.Yes;    Close(); };
            noBtn.Click     += (s, e) => { _result = MessageBoxResult.No;     Close(); };
            cancelBtn.Click += (s, e) => { _result = MessageBoxResult.Cancel; Close(); };

            btnPanel.Children.Add(yesBtn);
            btnPanel.Children.Add(noBtn);
            btnPanel.Children.Add(cancelBtn);

            Grid.SetRow(btnPanel, 2);
            root.Children.Add(btnPanel);

            Content = root;
        }

        // ── Button factory ───────────────────────────────────────────────

        private static Button MakeButton(string text, bool accent, bool isDarkMode)
        {
            // Resolved colours
            Color baseBg    = accent ? Color.FromRgb(0x5A, 0x2D, 0x82)
                                     : (isDarkMode ? Color.FromRgb(0x3A, 0x3A, 0x3A)
                                                   : Color.FromRgb(0xFF, 0xFF, 0xFF));
            Color hoverBg   = accent ? Color.FromRgb(0x7B, 0x40, 0xB0)
                                     : (isDarkMode ? Color.FromRgb(0x55, 0x55, 0x55)
                                                   : Color.FromRgb(0xE0, 0xE0, 0xE0));
            Color pressedBg = accent ? Color.FromRgb(0x4A, 0x20, 0x70)
                                     : (isDarkMode ? Color.FromRgb(0x48, 0x48, 0x48)
                                                   : Color.FromRgb(0xCC, 0xCC, 0xCC));
            Color fg        = accent ? Colors.White
                                     : (isDarkMode ? Color.FromRgb(0xF0, 0xF0, 0xF0)
                                                   : Color.FromRgb(0x1A, 0x1A, 0x1A));
            Color border    = accent ? Color.FromRgb(0x5A, 0x2D, 0x82)
                                     : (isDarkMode ? Color.FromRgb(0x44, 0x44, 0x44)
                                                   : Color.FromRgb(0xCC, 0xCC, 0xCC));

            var btn = new Button
            {
                Content         = text,
                Width           = 80,
                Height          = 30,
                Margin          = new Thickness(8, 0, 0, 0),
                Cursor          = Cursors.Hand,
                FontSize        = 13,
                Background      = new SolidColorBrush(baseBg),
                Foreground      = new SolidColorBrush(fg),
                BorderBrush     = new SolidColorBrush(border),
                BorderThickness = new Thickness(1),
                Padding         = new Thickness(0),
                Template        = BuildButtonTemplate()
            };

            // Hover / pressed via events — avoids WPF's default ButtonChrome entirely
            btn.MouseEnter += (s, e) => btn.Background = new SolidColorBrush(hoverBg);
            btn.MouseLeave += (s, e) => btn.Background = new SolidColorBrush(baseBg);
            btn.PreviewMouseDown += (s, e) => btn.Background = new SolidColorBrush(pressedBg);
            btn.PreviewMouseUp   += (s, e) => btn.Background = new SolidColorBrush(hoverBg);

            return btn;
        }

        /// <summary>
        /// Minimal button ControlTemplate — just a Border + ContentPresenter.
        /// Replaces WPF's default ButtonChrome which hard-codes the blue hover gradient.
        /// </summary>
        private static ControlTemplate BuildButtonTemplate()
        {
            var template = new ControlTemplate(typeof(Button));

            var border = new FrameworkElementFactory(typeof(Border));
            border.SetBinding(Border.BackgroundProperty,
                new Binding { RelativeSource = RelativeSource.TemplatedParent,
                              Path = new PropertyPath(BackgroundProperty) });
            border.SetBinding(Border.BorderBrushProperty,
                new Binding { RelativeSource = RelativeSource.TemplatedParent,
                              Path = new PropertyPath(BorderBrushProperty) });
            border.SetBinding(Border.BorderThicknessProperty,
                new Binding { RelativeSource = RelativeSource.TemplatedParent,
                              Path = new PropertyPath(BorderThicknessProperty) });
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));

            var content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            content.SetValue(ContentPresenter.VerticalAlignmentProperty,   VerticalAlignment.Center);
            border.AppendChild(content);

            template.VisualTree = border;
            return template;
        }
    }
}

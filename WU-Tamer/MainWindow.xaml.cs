using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Wu_change.ViewModels;

namespace Wu_change
{
    public partial class MainWindow : Window
    {
        private MainViewModel? _vm;

        public MainWindow()
        {
            InitializeComponent();
            _vm = new MainViewModel();
            _vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.IsDarkMode))
                    ApplyTheme(_vm.IsDarkMode);
            };
            DataContext = _vm;
            ApplyTheme(_vm.IsDarkMode);

            // Re-apply title bar tint once the HWND is available
            SourceInitialized += (s, e) => ApplyTheme(_vm.IsDarkMode);

            // Wire up RadioButton selections for history view toggle and sort
            Loaded += (s, e) => WireUpHistoryControls();
        }

        private void WireUpHistoryControls()
        {
            // Wire up view toggle RadioButtons (Grouped / Flat List)
            var viewRadioButtons = this.FindLogicalChildren<System.Windows.Controls.RadioButton>(
                rb => rb.GroupName == "HistoryView");
            foreach (var rb in viewRadioButtons)
            {
                rb.Checked += (s, e) =>
                {
                    if (_vm != null && s is System.Windows.Controls.RadioButton radioButton)
                        _vm.HistoryIsGrouped = (string)radioButton.Content != "Flat List";
                };
            }
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        private void ApplyTheme(bool dark)
        {
            var dict = Application.Current.Resources.MergedDictionaries;
            dict.Clear();
            var theme = new ResourceDictionary
            {
                Source = new System.Uri(
                    dark ? "Themes/Dark.xaml" : "Themes/Light.xaml",
                    System.UriKind.Relative)
            };
            dict.Add(theme);

            // Apply dark/light title bar via DWM
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                int value = dark ? 1 : 0;
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
            }
        }

        private List<T> FindLogicalChildren<T>(Func<T, bool> predicate) where T : DependencyObject
        {
            var result = new List<T>();
            FindLogicalChildrenRecursive(this, predicate, result);
            return result;
        }

        private void FindLogicalChildrenRecursive<T>(DependencyObject parent, Func<T, bool> predicate, List<T> result) where T : DependencyObject
        {
            int childrenCount = LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>().Count();
            for (int i = 0; i < childrenCount; i++)
            {
                var child = LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>().ElementAt(i);
                if (child is T typedChild && predicate(typedChild))
                    result.Add(typedChild);

                FindLogicalChildrenRecursive(child, predicate, result);
            }
        }
    }
}

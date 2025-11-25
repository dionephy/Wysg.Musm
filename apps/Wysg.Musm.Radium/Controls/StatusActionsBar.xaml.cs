using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;

namespace Wysg.Musm.Radium.Controls
{
    public partial class StatusActionsBar : UserControl
    {
        public StatusActionsBar()
        {
            InitializeComponent();
        }
        
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // If parent hasn't provided DarkToggleButtonStyle, assign local fallback
            var darkStyle = TryFindResource("DarkToggleButtonStyle") as Style;
            if (darkStyle == null)
            {
                var fallback = (Style)FindResource("_LocalDarkToggleButtonStyle");
                ApplyFallbackStyle(this, fallback);
            }
            
            // Initialize Always on Top checkbox from settings
            InitializeAlwaysOnTopCheckbox();
        }

        private static void ApplyFallbackStyle(DependencyObject root, Style style)
        {
            if (root is ToggleButton tb)
            {
                tb.Style = style;
            }
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                ApplyFallbackStyle(child, style);
            }
        }

        // Bubble actions to MainWindow handlers so existing logic remains.
        private void OnForceGhost_Click(object sender, RoutedEventArgs e) => RaiseEventToWindow("OnForceGhost", sender, e);
        private void OnOpenSettings_Click(object sender, RoutedEventArgs e) => RaiseEventToWindow("OnOpenSettings", sender, e);
        private void OnOpenSpy_Click(object sender, RoutedEventArgs e) => RaiseEventToWindow("OnOpenSpy", sender, e);
        private void OnAlignRight_Toggled(object sender, RoutedEventArgs e) => RaiseEventToWindow("OnAlignRightToggled", sender, e);
        private void OnReverseReports_Toggled(object sender, RoutedEventArgs e) => RaiseEventToWindow("OnReverseReportsChecked", sender, e);
        private void OnLogout_Click(object sender, RoutedEventArgs e) => RaiseEventToWindow("OnLogout", sender, e);
        private void OnAlwaysOnTop_Checked(object sender, RoutedEventArgs e) => RaiseEventToWindow("OnAlwaysOnTopChecked", sender, e);
        private void OnAlwaysOnTop_Unchecked(object sender, RoutedEventArgs e) => RaiseEventToWindow("OnAlwaysOnTopUnchecked", sender, e);

        private static void RaiseEventToWindow(string method, object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow((DependencyObject)sender) is Window win)
            {
                var mi = win.GetType().GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                mi?.Invoke(win, new object[] { sender, e });
            }
        }
        
        private void InitializeAlwaysOnTopCheckbox()
        {
            try
            {
                var app = (App)Application.Current;
                var local = app.Services.GetService<Services.IRadiumLocalSettings>();
                if (local != null)
                {
                    chkAlwaysOnTop.IsChecked = local.AlwaysOnTop;
                }
            }
            catch
            {
                // Silently fail - checkbox will remain unchecked
            }
        }
    }
}

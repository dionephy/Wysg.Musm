using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using Wysg.Musm.Radium.ViewModels;
using Wysg.Musm.Editor.Controls;
using Wysg.Musm.Radium.Services;
using Wysg.Musm.Radium.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace Wysg.Musm.Radium.Views
{
    public partial class MainWindow : Window
    {
        private bool _reportsReversed = false;
        private PacsService? _pacs;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            SizeChanged += OnWindowSizeChanged;
        }

        private void InitEditor(MainViewModel vm, EditorControl ctl)
        {
            vm.InitializeEditor(ctl);
            ctl.EnableGhostDebugAnchors(false);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            TryEnableDarkTitleBar();

            _pacs ??= new PacsService();

            // Show current user email
            try
            {
                var storage = ((App)Application.Current).Services.GetRequiredService<IAuthStorage>();
                txtUserEmail.Text = string.IsNullOrWhiteSpace(storage.Email) ? "(unknown)" : storage.Email;
            }
            catch { txtUserEmail.Text = string.Empty; }

            if (DataContext is not MainViewModel vm) return;
            InitEditor(vm, EditorHeader);
            InitEditor(vm, EditorFindings);
            InitEditor(vm, EditorConclusion);
            InitEditor(vm, EditorPreviousHeader);
            InitEditor(vm, EditorPreviousFindings);

            UpdateGridCenterSize();
            UpdateGridCenterPositioning();
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateGridCenterSize();
            UpdateGridCenterPositioning();
        }

        private void UpdateGridCenterSize()
        {
            if (gridCenter?.Parent is not Grid parent) return;

            int col = Grid.GetColumn(gridCenter);
            int row = Grid.GetRow(gridCenter);

            double availW = parent.ColumnDefinitions[col].ActualWidth;
            double availH = parent.RowDefinitions[row].ActualHeight;

            if (double.IsNaN(availW) || double.IsNaN(availH) || availW <= 0 || availH <= 0)
                return;

            double side = Math.Min(availW, availH);
            if (side < 0) side = 0;

            gridCenter.Width = side;
            gridCenter.Height = side;
        }

        private void UpdateGridCenterPositioning()
        {
            bool isLandscape = ActualWidth >= ActualHeight;
            if (isLandscape)
            {
                gridCenter.VerticalAlignment = VerticalAlignment.Center;
                gridCenter.HorizontalAlignment = (tglAlignRight?.IsChecked == true)
                    ? HorizontalAlignment.Right
                    : HorizontalAlignment.Left;
            }
            else
            {
                gridCenter.HorizontalAlignment = HorizontalAlignment.Center;
                gridCenter.VerticalAlignment = VerticalAlignment.Center;
            }
        }

        private void OnAlignRightToggled(object sender, RoutedEventArgs e)
        {
            UpdateGridCenterPositioning();
        }

        private void OnForceGhost(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                EditorFindings.DebugSeedGhosts();
            }
        }

        private void OnOpenSettings(object sender, RoutedEventArgs e)
        {
            var win = new SettingsWindow { Owner = this };
            win.ShowDialog();
        }

        private void OnReverseReportsChecked(object sender, RoutedEventArgs e)
        {
            _reportsReversed = chkReverseReports.IsChecked == true;
            SwapReportEditors(_reportsReversed);
        }

        private void SwapReportEditors(bool reversed)
        {
            var children = gridCenter.Children;
            var currentReportGrid = children[0] as UIElement;
            var previousReportGrid = children[2] as UIElement;
            if (currentReportGrid == null || previousReportGrid == null) return;
            if (reversed)
            {
                Grid.SetColumn(currentReportGrid, 2);
                Grid.SetColumn(previousReportGrid, 0);
            }
            else
            {
                Grid.SetColumn(currentReportGrid, 0);
                Grid.SetColumn(previousReportGrid, 2);
            }
        }

        private void OnReadBannerByHwnd(object sender, RoutedEventArgs e)
        {
            if (_pacs == null) { txtStatus.Text = "PACS service not available"; return; }
            if (!long.TryParse(txtPaneHwnd.Text?.Trim(), out long val))
            { txtStatus.Text = "Invalid HWND"; return; }

            var hwnd = new IntPtr(val);
            var title = _pacs.TryReadViewerBannerFromPane(hwnd) ?? "(null)";
            txtStatus.Text = $"HWND banner: {title}";
        }

        private void OnReadBannerFirstViewer(object sender, RoutedEventArgs e)
        {
            if (_pacs == null) { txtStatus.Text = "PACS service not available"; return; }
            var title = _pacs.TryReadFirstViewerBanner() ?? "(null)";
            txtStatus.Text = $"First viewer banner: {title}";
        }

        private void OnDumpStrings(object sender, RoutedEventArgs e)
        {
            if (_pacs == null) { txtStatus.Text = "PACS service not available"; return; }
            if (!long.TryParse(txtPaneHwnd.Text?.Trim(), out long val)) { txtStatus.Text = "Invalid HWND"; return; }
            var hwnd = new IntPtr(val);
            var arr = _pacs.DumpAllStrings(hwnd);
            if (arr.Length == 0) { txtStatus.Text = "No strings"; return; }
            var sample = string.Join("\n", arr.Take(50));
            MessageBox.Show(sample, $"Strings ({arr.Length})");
            txtStatus.Text = $"Dumped {arr.Length} strings";
        }

        private void OnWatchToggled(object sender, RoutedEventArgs e)
        {
            if (_pacs == null) { txtStatus.Text = "PACS service not available"; return; }
            if (tglWatch.IsChecked == true)
            {
                if (!long.TryParse(txtPaneHwnd.Text?.Trim(), out long val)) { txtStatus.Text = "Invalid HWND"; tglWatch.IsChecked = false; return; }
                var hwnd = new IntPtr(val);
                _pacs.StartWatchingPane(hwnd, arr => Dispatcher.Invoke(() =>
                {
                    txtStatus.Text = $"Strings changed: {arr.Length}";
                }));
                txtStatus.Text = "Watching¡¦";
            }
            else
            {
                _pacs.StopWatchingPane();
                txtStatus.Text = "Watch stopped";
            }
        }

        private async void OnReadBannerOcr(object sender, RoutedEventArgs e)
        {
            if (_pacs == null) { txtStatus.Text = "PACS service not available"; return; }
            if (!long.TryParse(txtPaneHwnd.Text?.Trim(), out long val)) { txtStatus.Text = "Invalid HWND"; return; }
            var hwnd = new IntPtr(val);
            txtStatus.Text = "OCR reading¡¦";
            try
            {
                var (engineAvailable, text) = await _pacs.OcrReadTopStripDetailedAsync(hwnd, 160);
                if (!engineAvailable)
                {
                    txtStatus.Text = "OCR not running (enable Windows OCR)";
                    return;
                }
                txtStatus.Text = string.IsNullOrWhiteSpace(text) ? "OCR: empty" : $"OCR: {text}";
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"OCR error: {ex.Message}";
            }
        }

        private async void OnReadBannerOcrFast(object sender, RoutedEventArgs e)
        {
            if (_pacs == null) { txtStatus.Text = "PACS service not available"; return; }
            if (!long.TryParse(txtPaneHwnd.Text?.Trim(), out long val)) { txtStatus.Text = "Invalid HWND"; return; }
            var hwnd = new IntPtr(val);

            // Defaults: top strip center area
            int L = TryParse(txtCropL.Text, 40);
            int T = TryParse(txtCropT.Text, 10);
            int W = TryParse(txtCropW.Text, 1200);
            int H = TryParse(txtCropH.Text, 80);

            // Clamp negative values to zero
            if (L < 0) L = 0; if (T < 0) T = 0; if (W < 10) W = 10; if (H < 10) H = 10;

            txtStatus.Text = "OCR(Fast) reading¡¦";
            try
            {
                var (engineAvailable, text) = await Wysg.Musm.MFCUIA.OcrReader.OcrTryReadRegionDetailedAsync(hwnd, new System.Drawing.Rectangle(L, T, W, H));
                if (!engineAvailable)
                {
                    txtStatus.Text = "OCR not running (enable Windows OCR)";
                    return;
                }
                txtStatus.Text = string.IsNullOrWhiteSpace(text) ? "OCR(Fast): empty" : $"OCR(Fast): {text}";
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"OCR(Fast) error: {ex.Message}";
            }
        }

        private static int TryParse(string s, int fallback)
        {
            return int.TryParse(s?.Trim(), out int v) ? v : fallback;
        }

        private void OnAutoLocate(object sender, RoutedEventArgs e)
        {
            if (_pacs == null) { txtStatus.Text = "PACS service not available"; return; }
            var (hwnd, text) = _pacs.TryAutoLocateBanner();
            if (hwnd == IntPtr.Zero)
            {
                txtStatus.Text = "Auto locate failed";
                return;
            }
            txtPaneHwnd.Text = hwnd.ToInt64().ToString();
            txtStatus.Text = string.IsNullOrWhiteSpace(text) ? $"Auto locate: hwnd={hwnd}" : $"Auto locate: {text} (hwnd={hwnd})";
        }

        private void OnBookmarkSave(object sender, RoutedEventArgs e)
        {
            var name = (txtBookmarkName.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name)) { txtStatus.Text = "Bookmark: enter a name"; return; }
            if (!long.TryParse(txtPaneHwnd.Text?.Trim(), out long val)) { txtStatus.Text = "Bookmark: invalid HWND"; return; }
            var hwnd = new System.IntPtr(val);

            // For now, just save a flat bookmark with process name and a single node containing class and index from FlaUI
            try
            {
                using var app = FlaUI.Core.Application.Attach("INFINITT");
                using var automation = new FlaUI.UIA3.UIA3Automation();
                var main = app.GetMainWindow(automation, System.TimeSpan.FromMilliseconds(800));
                if (main == null) { txtStatus.Text = "Bookmark: cannot attach app"; return; }
                var el = automation.FromHandle(hwnd);
                if (el == null) { txtStatus.Text = "Bookmark: cannot wrap hwnd"; return; }

                // Build simple chain: climb to main via parent steps, then record class/controltype for the element itself.
                var store = UiBookmarks.Load();
                var b = new UiBookmarks.Bookmark { Name = name, ProcessName = "INFINITT" };
                b.Chain.Add(new UiBookmarks.Node
                {
                    ClassName = el.ClassName,
                    ControlTypeId = (int)el.ControlType,
                    AutomationId = el.AutomationId,
                    IndexAmongMatches = 0
                });
                store.Bookmarks.RemoveAll(x => x.Name.Equals(name, System.StringComparison.OrdinalIgnoreCase));
                store.Bookmarks.Add(b);
                UiBookmarks.Save(store);
                txtStatus.Text = $"Bookmark saved: {name} -> hwnd={hwnd}";
            }
            catch (System.Exception ex)
            {
                txtStatus.Text = $"Bookmark save error: {ex.Message}";
            }
        }

        private void OnBookmarkResolve(object sender, RoutedEventArgs e)
        {
            var name = (txtBookmarkName.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name)) { txtStatus.Text = "Bookmark: enter a name"; return; }
            var (hwnd, el) = UiBookmarks.Resolve(name);
            if (hwnd == System.IntPtr.Zero)
            {
                txtStatus.Text = "Bookmark: not found or stale";
                return;
            }
            txtPaneHwnd.Text = hwnd.ToInt64().ToString();
            txtStatus.Text = $"Bookmark resolved: {name} -> hwnd={hwnd}";
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            // Shortcut: Ctrl+Alt+S opens the Spy
            this.PreviewKeyDown += (s, ev) =>
            {
                if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) &&
                    (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)) &&
                    ev.Key == Key.S)
                {
                    ev.Handled = true;
                    var win = new SpyWindow { Owner = this };
                    win.Show();
                }
            };
        }

        private void OnOpenSpy(object sender, RoutedEventArgs e)
        {
            var win = new SpyWindow { Owner = this };
            win.Show();
        }

        private async void OnLogout(object sender, RoutedEventArgs e)
        {
            try
            {
                var app = (App)Application.Current;
                var storage = app.Services.GetRequiredService<IAuthStorage>();
                var auth = app.Services.GetRequiredService<IAuthService>();

                storage.RefreshToken = null;
                storage.Email = null;
                storage.DisplayName = null;
                storage.RememberMe = false;

                txtUserEmail.Text = string.Empty;

                await auth.SignOutAsync();
            }
            catch { }

            var appRef = (App)Application.Current;
            this.Close();
            await appRef.ShowSplashLoginAsync();
        }

        // Try to enable dark title bar on supported Windows versions
        private void TryEnableDarkTitleBar()
        {
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd == IntPtr.Zero) return;

                int useImmersiveDarkMode = 1;
                // Windows 10 1809 (17763) uses 19, 1903+ uses 20
                const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
                const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

                // Try 20 first
                if (DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useImmersiveDarkMode, sizeof(int)) != 0)
                {
                    // Fallback to 19
                    DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref useImmersiveDarkMode, sizeof(int));
                }
            }
            catch { }
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
    }
}
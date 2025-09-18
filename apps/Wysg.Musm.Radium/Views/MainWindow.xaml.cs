using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using Wysg.Musm.Radium.ViewModels;
using Wysg.Musm.Editor.Controls;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.Views
{
    public partial class MainWindow : Window
    {
        private bool _reportsReversed = false;
        private MfcPacsService? _pacs;

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
            _pacs ??= new MfcPacsService();

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
    }
}
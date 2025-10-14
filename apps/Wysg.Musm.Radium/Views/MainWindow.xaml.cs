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
        private bool _alignRight = false;
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
            System.Diagnostics.Debug.WriteLine("[MainWindow] OnLoaded START");
            TryEnableDarkTitleBar();

            _pacs ??= new PacsService();

            // Show current user email (optional: bind in VM/UI instead)
            try { var _ = ((App)Application.Current).Services.GetRequiredService<IAuthStorage>(); } catch { }

            if (DataContext is not MainViewModel vm) return;
            
            // Initialize PACS profiles
            vm.InitializePacsProfilesForMain();
            
            try { System.Diagnostics.Debug.WriteLine("[MainWindow] InitEditor Header"); InitEditor(vm, EditorHeader); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[MainWindow][EX] InitEditor Header: " + ex); throw; }
            try { System.Diagnostics.Debug.WriteLine("[MainWindow] InitEditor Findings"); InitEditor(vm, EditorFindings); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[MainWindow][EX] InitEditor Findings: " + ex); throw; }
            try { System.Diagnostics.Debug.WriteLine("[MainWindow] InitEditor Conclusion"); InitEditor(vm, EditorConclusion); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[MainWindow][EX] InitEditor Conclusion: " + ex); throw; }
            try { System.Diagnostics.Debug.WriteLine("[MainWindow] InitEditor PrevHeader"); InitEditor(vm, EditorPreviousHeader); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[MainWindow][EX] InitEditor PrevHeader: " + ex); throw; }
            try { System.Diagnostics.Debug.WriteLine("[MainWindow] InitEditor PrevFindings"); InitEditor(vm, EditorPreviousFindings); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[MainWindow][EX] InitEditor PrevFindings: " + ex); throw; }

            UpdateGridCenterSize();
            UpdateGridCenterPositioning();
            System.Diagnostics.Debug.WriteLine("[MainWindow] OnLoaded COMPLETE");
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
                gridSide.VerticalAlignment = VerticalAlignment.Center;
                gridSide.HorizontalAlignment = _alignRight
                    ? HorizontalAlignment.Left
                    : HorizontalAlignment.Right;

                gridCenter.VerticalAlignment = VerticalAlignment.Center;
                var horiz = _alignRight ? HorizontalAlignment.Right : HorizontalAlignment.Left;
                gridCenter.HorizontalAlignment = horiz;

                gridStatusPortrait.Visibility = Visibility.Collapsed;
                gridStatusLandscape.Visibility = Visibility.Visible;


                if (gridBottom != null) gridBottom.HorizontalAlignment = horiz; // mirror alignment for bottom overlay grid
                if (gridTop != null) gridTop.HorizontalAlignment = horiz; // mirror alignment for top overlay grid
            }
            else
            {
                gridCenter.HorizontalAlignment = HorizontalAlignment.Center;
                gridCenter.VerticalAlignment = VerticalAlignment.Center;
                if (gridTop != null) gridTop.HorizontalAlignment = HorizontalAlignment.Center;
                if (gridBottom != null) gridBottom.HorizontalAlignment = HorizontalAlignment.Center;

                gridStatusPortrait.Visibility = Visibility.Visible;
                gridStatusLandscape.Visibility = Visibility.Collapsed;
            }
        }

        private void OnAlignRightToggled(object sender, RoutedEventArgs e)
        {
            _alignRight = !_alignRight;
            UpdateGridCenterPositioning();
            UpdateGridSideLayout(_alignRight);
        }

        private void UpdateGridSideLayout(bool right)
        {
            // Toggle Reverse on side panels only (landscape mode)
            // Align Right should NOT affect top/bottom panels
            try { gridSideTop.Reverse = right; } catch { }
            try { gridSideBottom.Reverse = right; } catch { }
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
            _reportsReversed = !_reportsReversed;
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

            // Reverse Reports should ONLY affect top/bottom panels
            try { gridTopChild.Reverse = reversed; } catch { }
            try { gridBottomControl.Reverse = !reversed; } catch { }

            // Side panels are controlled by Align Right in UpdateGridSideLayout and are NOT affected here
        }

        private void OnReadBannerByHwnd(object sender, RoutedEventArgs e) { }

        private void OnReadBannerFirstViewer(object sender, RoutedEventArgs e) { }

        private void OnDumpStrings(object sender, RoutedEventArgs e) { }

        private void OnWatchToggled(object sender, RoutedEventArgs e) { }

        private async void OnReadBannerOcr(object sender, RoutedEventArgs e) { }

        private async void OnReadBannerOcrFast(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm) vm.StatusText = "OCR(Fast) reading...";
            // No crop values available, so just show status or disable button
        }

        private void OnAutoLocate(object sender, RoutedEventArgs e) { }

        private void OnBookmarkSave(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm) vm.StatusText = "Bookmark save not available.";
        }

        private void OnBookmarkResolve(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm) vm.StatusText = "Bookmark resolve not available.";
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

        // Test helper: add a dummy previous study with tabs
        private void OnAddDummyPrevious(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;
            var tab = new MainViewModel.PreviousStudyTab
            {
                Id = Guid.NewGuid(),
                StudyDateTime = DateTime.Now.AddDays(-1),
                Modality = "CT",
                Title = $"{DateTime.Now.AddDays(-1):yyyy-MM-dd} CT",
                OriginalHeader = string.Empty,
                OriginalFindings = "Dummy findings A\nLine 2",
                OriginalConclusion = "Dummy conclusion A"
            };
            tab.Reports.Add(new MainViewModel.PreviousReportChoice
            {
                ReportDateTime = DateTime.Now.AddHours(-12),
                CreatedBy = "tester",
                Studyname = "CT HEAD",
                Findings = "Dummy findings A\nLine 2",
                Conclusion = "Dummy conclusion A",
                _studyDateTime = tab.StudyDateTime
            });
            tab.Reports.Add(new MainViewModel.PreviousReportChoice
            {
                ReportDateTime = DateTime.Now.AddHours(-6),
                CreatedBy = "tester2",
                Studyname = "CT HEAD",
                Findings = "Dummy findings B",
                Conclusion = "Dummy conclusion B",
                _studyDateTime = tab.StudyDateTime
            });
            tab.SelectedReport = tab.Reports.FirstOrDefault();
            if (tab.SelectedReport != null)
            {
                tab.Findings = tab.SelectedReport.Findings;
                tab.Conclusion = tab.SelectedReport.Conclusion;
            }
            vm.PreviousStudies.Add(tab);
            vm.SelectedPreviousStudy = tab;
            vm.PreviousReportified = true;
            // initialize splitters to defaults
            if (vm.SelectedPreviousStudy != null)
            {
                var t = vm.SelectedPreviousStudy;
                t.HfHeaderFrom = 0; t.HfHeaderTo = 0;
                t.HfConclusionFrom = (t.Findings ?? string.Empty).Length; t.HfConclusionTo = (t.Findings ?? string.Empty).Length;
                t.FcHeaderFrom = 0; t.FcHeaderTo = 0;
                t.FcFindingsFrom = 0; t.FcFindingsTo = 0;
            }
            vm.StatusText = "Dummy previous study added";
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

                // Clear UI email if needed (removed direct TextBlock reference)

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

        private void OnExtractPhrases(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is not MainViewModel vm) return;
                var app = (App)Application.Current;
                
                // Use DI to get the view model with all dependencies properly injected
                var vmExtract = app.Services.GetService<PhraseExtractionViewModel>();
                if (vmExtract == null)
                {
                    MessageBox.Show("Phrase extraction service not available.", "Extract Phrases", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                var win = new PhraseExtractionWindow { Owner = this, DataContext = vmExtract };
                
                // Build dereportified / raw lines from current editors
                var (header, findings, conclusion) = vm.GetDereportifiedSections();
                vmExtract.LoadFromDeReportified(header, findings, conclusion);
                win.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Extraction window error: {ex.Message}", "Extract Phrases", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static int TryParse(string s, int fallback)
        {
            return int.TryParse(s?.Trim(), out int v) ? v : fallback;
        }
    }
}
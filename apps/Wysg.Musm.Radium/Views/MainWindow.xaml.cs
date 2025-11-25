using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Microsoft.Extensions.DependencyInjection;
using Wysg.Musm.Editor.Controls;
using Wysg.Musm.Radium.ViewModels;
using Wysg.Musm.Radium.Services;
using WF = System.Windows.Forms;
using Task = System.Threading.Tasks.Task;

namespace Wysg.Musm.Radium.Views
{
    public partial class MainWindow : Window
    {
        private bool _reportsReversed = false;
        private bool _alignRight = false;
        private PacsService? _pacs;
        
        // NEW: Editor autofocus service
        private EditorAutofocusService? _autofocusService;

        // Global hotkeys (system-wide)
        private const int HOTKEY_ID_OPEN_STUDY = 0xB001;
        private const int HOTKEY_ID_TOGGLE_SYNC_TEXT = 0xB002;
        private const int HOTKEY_ID_SEND_REPORT = 0xB003;
        private uint _openStudyMods;
        private uint _openStudyVk;
        private uint _toggleSyncTextMods;
        private uint _toggleSyncTextVk;
        private uint _sendReportMods;
        private uint _sendReportVk;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            SizeChanged += OnWindowSizeChanged;
            Closing += OnClosing;
            
            // Initialize triple-click paragraph selection support
            InitializeTripleClickSupport();
            
            // Bypass Alt key system menu behavior
            PreviewKeyDown += OnPreviewKeyDownBypassAlt;
        }
        
        /// <summary>
        /// Bypass Alt key system menu behavior to prevent title bar focus and menu dropdown.
        /// When Alt is pressed alone (without other keys), it normally activates the window menu system
        /// and gives focus to the title bar, causing the next key press to be ignored or trigger menu actions.
        /// This handler suppresses that behavior by marking the Alt key as handled.
        /// </summary>
        private void OnPreviewKeyDownBypassAlt(object sender, KeyEventArgs e)
        {
            // Only suppress Alt when it's pressed completely alone (not with arrow keys or other keys)
            // SystemKey is used for Alt combinations, Key is used for regular keys
            
            // Check if this is Alt being pressed alone
            bool isAltAlone = (e.Key == Key.System || e.Key == Key.LeftAlt || e.Key == Key.RightAlt) &&
                              (e.SystemKey == Key.LeftAlt || e.SystemKey == Key.RightAlt || e.SystemKey == Key.None);
            
            // Only handle if Alt is truly alone (no arrow keys or other keys)
            if (isAltAlone && e.KeyboardDevice.Modifiers == ModifierKeys.Alt)
            {
                // Don't suppress if this is part of a combination (check if SystemKey indicates a combination)
                // When Alt+Arrow is pressed, SystemKey will be the arrow key (Up, Down, Left, Right)
                if (e.SystemKey == Key.None || e.SystemKey == Key.LeftAlt || e.SystemKey == Key.RightAlt)
                {
                    e.Handled = true;
                    System.Diagnostics.Debug.WriteLine("[MainWindow] Suppressed Alt key (pressed alone)");
                }
            }
        }

        private void InitEditor(MainViewModel vm, EditorControl ctl)
        {
            vm.InitializeEditor(ctl);
            ctl.EnableGhostDebugAnchors(false);
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
System.Diagnostics.Debug.WriteLine("[MainWindow] OnLoaded START");
          TryEnableDarkTitleBar();

            _pacs ??= new PacsService();

            // Show current user email (optional: bind in VM/UI instead)
   try { var _ = ((App)Application.Current).Services.GetRequiredService<IAuthStorage>(); } catch { }

    if (DataContext is not MainViewModel vm) return;
            
     // PACS profiles are now managed via Settings -> PACS and ITenantContext.CurrentPacsKey.
            // Do not initialize legacy local profiles here.
            
      try { System.Diagnostics.Debug.WriteLine("[MainWindow] InitEditor Header"); InitEditor(vm, gridCenter.EditorHeader); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[MainWindow][EX] InitEditor Header: " + ex); throw; }
            try { System.Diagnostics.Debug.WriteLine("[MainWindow] InitEditor Findings"); InitEditor(vm, gridCenter.EditorFindings); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[MainWindow][EX] InitEditor Findings: " + ex); throw; }
            try { System.Diagnostics.Debug.WriteLine("[MainWindow] InitEditor Conclusion"); InitEditor(vm, gridCenter.EditorConclusion); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[MainWindow][EX] InitEditor Conclusion: " + ex); throw; }
 try { System.Diagnostics.Debug.WriteLine("[MainWindow] InitEditor PrevHeader"); InitEditor(vm, gridCenter.EditorPreviousHeader); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[MainWindow][EX] InitEditor PrevHeader: " + ex); throw; }
            try { System.Diagnostics.Debug.WriteLine("[MainWindow] InitEditor PrevFindings"); InitEditor(vm, gridCenter.EditorPreviousFindings); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[MainWindow][EX] InitEditor PrevFindings: " + ex); throw; }

   UpdateGridCenterSize();
        UpdateGridCenterPositioning();
      
            // Wire up Alt+Arrow navigation from ReportInputsAndJsonPanel to EditorFindings
            try
   {
         System.Diagnostics.Debug.WriteLine("[MainWindow] Wiring up Alt+Arrow navigation");
     gridTopChild.TargetEditor = gridCenter.EditorFindings;
   gridSideTop.TargetEditor = gridCenter.EditorFindings;
       
    // Inject orientation detection function into CenterEditingArea
   gridCenter.IsLandscapeMode = () => ActualWidth >= ActualHeight;
        
         // Inject Patient History textbox getter
      gridCenter.GetPatientHistoryTextBox = () =>
    {
                bool isLandscape = ActualWidth >= ActualHeight;
         TextBox? txtPatientHistory = null;
         
  System.Diagnostics.Debug.WriteLine($"[MainWindow] GetPatientHistoryTextBox called:");
            System.Diagnostics.Debug.WriteLine($"  - Window dimensions: {ActualWidth}x{ActualHeight}");
       System.Diagnostics.Debug.WriteLine($"  - isLandscape: {isLandscape}");
     System.Diagnostics.Debug.WriteLine($"  - gridSide.ActualWidth: {gridSide?.ActualWidth ?? -1}");
            System.Diagnostics.Debug.WriteLine($"  - gridTop.ActualHeight: {gridTop?.ActualHeight ?? -1}");
  
if (isLandscape)
 {
           // Landscape: gridSide should be visible, look in gridSideTop
 // Check if gridSide is actually the active panel by checking its visibility/position
                if (gridSide != null && gridSide.ActualWidth > 0)
        {
   System.Diagnostics.Debug.WriteLine($"  - Landscape: Checking gridSideTop (ActualWidth={gridSide.ActualWidth})");
          txtPatientHistory = gridSideTop?.FindName("txtPatientHistory") as TextBox;
            System.Diagnostics.Debug.WriteLine($"  - gridSideTop.FindName result: {txtPatientHistory != null}");
       if (txtPatientHistory != null)
                    {
    System.Diagnostics.Debug.WriteLine($"  - Found in gridSideTop: IsVisible={txtPatientHistory.IsVisible}, ActualWidth={txtPatientHistory.ActualWidth}");
       }
                }
                else
                {
   System.Diagnostics.Debug.WriteLine($"  - Landscape mode but gridSide.ActualWidth={gridSide?.ActualWidth ?? 0} (not active)");
       }
       }
      else
            {
           // Portrait: gridTop should be visible, look in gridTopChild
       if (gridTop != null && gridTop.ActualHeight > 0)
      {
  System.Diagnostics.Debug.WriteLine($"  - Portrait: Checking gridTopChild (ActualHeight={gridTop.ActualHeight})");
        txtPatientHistory = gridTopChild?.FindName("txtPatientHistory") as TextBox;
      System.Diagnostics.Debug.WriteLine($"  - gridTopChild.FindName result: {txtPatientHistory != null}");
   if (txtPatientHistory != null)
          {
             System.Diagnostics.Debug.WriteLine($"  - Found in gridTopChild: IsVisible={txtPatientHistory.IsVisible}, ActualWidth={txtPatientHistory.ActualWidth}");
               }
           }
                else
      {
        System.Diagnostics.Debug.WriteLine($"  - Portrait mode but gridTop.ActualHeight={gridTop?.ActualHeight ?? 0} (not active)");
      }
            }
            
            System.Diagnostics.Debug.WriteLine($"  - Final result: {(txtPatientHistory != null ? "FOUND" : "NULL")}");
        return txtPatientHistory;
        };
   }
        catch (Exception ex)
            {
  System.Diagnostics.Debug.WriteLine("[MainWindow] Alt+Arrow wiring EX: " + ex.Message);
            }
            
   // Listen for focus request from ViewModel (e.g., after text sync disable)
 vm.PropertyChanged += OnViewModelPropertyChanged;
        
   // NEW: Listen for Study Remark focus request from ViewModel (e.g., after SetCurrentInMainScreen completes)
            vm.RequestFocusStudyRemark += OnRequestFocusStudyRemark;
            
    try
    {
    await vm.LoadPhrasesAsync();
  }
            catch (Exception ex)
            {
    System.Diagnostics.Debug.WriteLine("[MainWindow] LoadPhrasesAsync EX: " + ex.Message);
            }

            System.Diagnostics.Debug.WriteLine("[MainWindow] OnLoaded COMPLETE");
            
            // NEW: Initialize editor autofocus service
            try
            {
                var app = (App)Application.Current;
                var localSettings = app.Services.GetService<IRadiumLocalSettings>();
                if (localSettings != null && localSettings.EditorAutofocusEnabled)
                {
                    _autofocusService = new EditorAutofocusService(
                        localSettings,
                        () =>
                        {
                            // Focus callback: focus the Findings editor
                            System.Diagnostics.Debug.WriteLine("[MainWindow] Autofocus callback triggered - focusing EditorFindings");
                            
                            // Activate window first
                            if (!this.IsActive)
                            {
                                this.Activate();
                            }
                            
                            // Focus the underlying MusmEditor
                            var editorControl = gridCenter.EditorFindings;
                            if (editorControl != null)
                            {
                                var musmEditor = editorControl.FindName("Editor") as ICSharpCode.AvalonEdit.TextEditor;
                                if (musmEditor != null)
                                {
                                    musmEditor.Focus();
                                    musmEditor.TextArea?.Caret.BringCaretToView();
                                }
                                else
                                {
                                    editorControl.Focus();
                                }
                            }
                        });
                    
                    _autofocusService.Start();
                    System.Diagnostics.Debug.WriteLine("[MainWindow] EditorAutofocusService started");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[MainWindow] EditorAutofocusService not started (disabled in settings)");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] Failed to initialize EditorAutofocusService: {ex.Message}");
            }
            
            // NEW: Initialize Always on Top from settings
            try
            {
                var app = (App)Application.Current;
                var localSettings = app.Services.GetService<IRadiumLocalSettings>();
                if (localSettings != null && localSettings.AlwaysOnTop)
                {
                    this.Topmost = true;
                    System.Diagnostics.Debug.WriteLine("[MainWindow] Always on Top enabled from settings");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] Failed to initialize Always on Top: {ex.Message}");
            }
        }
        
        private async void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.RequestFocusFindings))
            {
                // Focus EditorFindings when ViewModel requests it
                System.Diagnostics.Debug.WriteLine("[MainWindow] Focus request received - focusing EditorFindings");
                
                // Small delay to ensure foreign app has finished processing before we steal focus back
                await Task.Delay(50);
                
                // Activate this window first to ensure it's in foreground
                if (!this.IsActive)
                {
                    System.Diagnostics.Debug.WriteLine("[MainWindow] Window not active - calling Activate()");
                    this.Activate();
                }
                
                // Focus the underlying MusmEditor inside EditorControl
                // The EditorControl is a UserControl wrapper; we need to focus the actual MusmEditor named "Editor" for keyboard input
                var editorControl = gridCenter.EditorFindings;
                if (editorControl != null)
                {
                    // Find the MusmEditor child control named "Editor"
                    var musmEditor = editorControl.FindName("Editor") as ICSharpCode.AvalonEdit.TextEditor;
                    if (musmEditor != null)
                    {
                        System.Diagnostics.Debug.WriteLine("[MainWindow] Focusing underlying MusmEditor");
                        musmEditor.Focus();
                        
                        // Ensure caret is visible by scrolling to it
                        try
                        {
                            musmEditor.TextArea?.Caret.BringCaretToView();
                            System.Diagnostics.Debug.WriteLine($"[MainWindow] Caret at offset: {musmEditor.CaretOffset}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MainWindow] Caret scroll error: {ex.Message}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[MainWindow] MusmEditor not found - falling back to UserControl focus");
                        editorControl.Focus();
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[MainWindow] EditorControl is null");
                }
                
                System.Diagnostics.Debug.WriteLine("[MainWindow] Focus operation completed");
            }
        }
        
        private async void OnRequestFocusStudyRemark(object? sender, EventArgs e)
        {
            // Focus Study Remark textbox when ViewModel requests it (after SetCurrentInMainScreen completes)
            System.Diagnostics.Debug.WriteLine("[MainWindow] Focus request received - focusing Study Remark textbox");
            
            // Small delay to ensure UI has finished updating before focusing
            await Task.Delay(50);
            
            // Activate this window first to ensure it's in foreground
            if (!this.IsActive)
            {
                System.Diagnostics.Debug.WriteLine("[MainWindow] Window not active - calling Activate()");
                this.Activate();
            }
            
            // Find the Study Remark textbox in the top grid (gridTopChild or gridSideTop depending on orientation)
            // The ReportInputsAndJsonPanel is used in both locations, so we need to find the active one
            System.Windows.Controls.TextBox? txtStudyRemark = null;
            
            try
            {
                // Try to find txtStudyRemark from gridTopChild (portrait orientation)
                if (gridTopChild != null && gridTopChild.Visibility == Visibility.Visible)
                {
                    txtStudyRemark = gridTopChild.FindName("txtStudyRemark") as System.Windows.Controls.TextBox;
                    System.Diagnostics.Debug.WriteLine($"[MainWindow] Found txtStudyRemark in gridTopChild: {txtStudyRemark != null}");
                }
                
                // If not found in gridTopChild, try gridSideTop (landscape orientation)
                if (txtStudyRemark == null && gridSideTop != null && gridSideTop.Visibility == Visibility.Visible)
                {
                    txtStudyRemark = gridSideTop.FindName("txtStudyRemark") as System.Windows.Controls.TextBox;
                    System.Diagnostics.Debug.WriteLine($"[MainWindow] Found txtStudyRemark in gridSideTop: {txtStudyRemark != null}");
                }
                
                // Focus the textbox if found
                if (txtStudyRemark != null)
                {
                    txtStudyRemark.Focus();
                    txtStudyRemark.CaretIndex = txtStudyRemark.Text?.Length ?? 0; // Move caret to end
                    System.Diagnostics.Debug.WriteLine($"[MainWindow] Focused Study Remark textbox, caret at end (length={txtStudyRemark.Text?.Length ?? 0})");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[MainWindow] Study Remark textbox not found in either panel");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] Error focusing Study Remark textbox: {ex.Message}");
            }
            
            System.Diagnostics.Debug.WriteLine("[MainWindow] Focus Study Remark operation completed");
        }

        private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            TrySaveWindowPlacement();
        }

        private void TryRestoreWindowPlacement()
        {
            try
            {
                var app = (App)Application.Current;
                var local = app.Services.GetService<IRadiumLocalSettings>();
                var s = local?.MainWindowPlacement;
                if (string.IsNullOrWhiteSpace(s)) return;
                var parts = s.Split(',');
                if (parts.Length < 5) return;
                if (double.TryParse(parts[0], out double left)) Left = left;
                if (double.TryParse(parts[1], out double top)) Top = top;
                if (double.TryParse(parts[2], out double width)) Width = width;
                if (double.TryParse(parts[3], out double height)) Height = height;
                var stateStr = parts[4];
                WindowState st = WindowState.Normal;
                if (Enum.TryParse<WindowState>(stateStr, true, out var parsed)) st = parsed;
                // Multi-monitor, per?monitor DPI aware: convert DIP rect -> pixel rect and verify it intersects any screen
                Rect dipRect = new Rect(Left, Top, Width, Height);
                var dpi = VisualTreeHelper.GetDpi(this);
                var pxRect = new System.Drawing.Rectangle(
                    (int)Math.Floor(dipRect.X * dpi.DpiScaleX),
                    (int)Math.Floor(dipRect.Y * dpi.DpiScaleY),
                    Math.Max(1, (int)Math.Ceiling(dipRect.Width * dpi.DpiScaleX)),
                    Math.Max(1, (int)Math.Ceiling(dipRect.Height * dpi.DpiScaleY)));
                bool intersects = false;
                foreach (var screen in WF.Screen.AllScreens)
                {
                    if (pxRect.IntersectsWith(screen.WorkingArea) || pxRect.IntersectsWith(screen.Bounds)) { intersects = true; break; }
                }
                if (!intersects)
                {
                    // Fallback: place near primary screen working area top-left (keep size)
                    var pwa = WF.Screen.PrimaryScreen.WorkingArea;
                    // Convert a safe pixel point back to DIP
                    Left = (pwa.Left + 50) / dpi.DpiScaleX;
                    Top = (pwa.Top + 50) / dpi.DpiScaleY;
                }
                WindowState = st;
            }
            catch { }
        }

        private void TrySaveWindowPlacement()
        {
            try
            {
                var app = (App)Application.Current;
                var local = app.Services.GetService<IRadiumLocalSettings>();
                if (local == null) return;
                // If window is minimized, we save as Normal
                var st = this.WindowState == WindowState.Minimized ? WindowState.Normal : this.WindowState;
                // Use RestoreBounds when maximized
                Rect r = st == WindowState.Maximized ? this.RestoreBounds : new Rect(Left, Top, Width, Height);
                string value = string.Join(',', r.Left, r.Top, r.Width, r.Height, st.ToString());
                local.MainWindowPlacement = value;
            }
            catch { }
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
                gridCenter.EditorFindings.DebugSeedGhosts();
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
            // CenterEditingArea is a UserControl, not a Grid with Children.
            // Column swapping now happens inside CenterEditingArea if needed,
            // but for this initial refactoring we'll just update the overlay panels.
            
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

        private HwndSource? _hotkeyHwndSource;

        // NOTE (Fix): Previously we attempted to register the global hotkey in OnInitialized.
        // At that time the window HWND often isn't created yet, so RegisterHotKey silently fails.
        // We now keep OnInitialized only for local key handlers and move registration to OnSourceInitialized
        // (after the HwndSource exists) to ensure the OS-level hotkey is registered successfully.
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
                    SpyWindow.ShowInstance();
                }
            };
        }

        // Fix: Register the OS-level hotkey after the window handle is created.
        // This guarantees that RegisterHotKey receives a valid HWND (via HwndSource)
        // and the global shortcut (e.g., Ctrl+Alt+O) can be received by this window.
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            // Apply saved placement before first paint; set manual startup to avoid CenterScreen overriding.
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            TryRestoreWindowPlacement();
            // Register global hotkeys (if configured) after HWND exists
            TryRegisterOpenStudyHotkey();
            TryRegisterToggleSyncTextHotkey();
            TryRegisterSendReportHotkey();
        }

        private void OnOpenSpy(object sender, RoutedEventArgs e)
        {
            SpyWindow.ShowInstance();
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
        
        // NEW: Always on Top event handlers
        private void OnAlwaysOnTopChecked(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Topmost = true;
                
                // Save setting
                var app = (App)Application.Current;
                var localSettings = app.Services.GetService<IRadiumLocalSettings>();
                if (localSettings != null)
                {
                    localSettings.AlwaysOnTop = true;
                }
                
                System.Diagnostics.Debug.WriteLine("[MainWindow] Always on Top enabled");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] Failed to enable Always on Top: {ex.Message}");
            }
        }
        
        private void OnAlwaysOnTopUnchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Topmost = false;
                
                // Save setting
                var app = (App)Application.Current;
                var localSettings = app.Services.GetService<IRadiumLocalSettings>();
                if (localSettings != null)
                {
                    localSettings.AlwaysOnTop = false;
                }
                
                System.Diagnostics.Debug.WriteLine("[MainWindow] Always on Top disabled");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] Failed to disable Always on Top: {ex.Message}");
            }
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
                
                // Get or create singleton window instance
                var win = PhraseExtractionWindow.GetOrCreateInstance();
                win.Owner = this;
                win.DataContext = vmExtract;
                
                // Get content with proofread fallback mechanism (uses new helper method)
                var (header, findings, conclusion) = vm.GetProofreadOrRawSections();
                vmExtract.LoadFromDeReportified(header, findings, conclusion);
                
                // Show window (activate if already open)
                if (!win.IsVisible)
                {
                    win.Show();
                }
                else
                {
                    win.Activate();
                }
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

        // ------------- Global Hotkey Registration (Open Study) -------------
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // Modifiers (user32)
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;
        private const int WM_HOTKEY = 0x0312;

        private void TryRegisterOpenStudyHotkey()
        {
            try
            {
                var app = (App)Application.Current;
                var local = app.Services.GetService<IRadiumLocalSettings>();
                var text = local?.GlobalHotkeyOpenStudy;
                if (string.IsNullOrWhiteSpace(text)) return;

                // Parse persisted hotkey text (e.g., "Ctrl+Alt+O") into user32 modifiers + virtual key.
                // Previous issue: weak parsing and too-early registration led to no WM_HOTKEY events.
                if (!TryParseHotkey(text!, out _openStudyMods, out _openStudyVk)) return;

                // Acquire current HwndSource for this window; it exists only after OnSourceInitialized.
                _hotkeyHwndSource = (HwndSource?)PresentationSource.FromVisual(this);
                if (_hotkeyHwndSource == null) return;
                _hotkeyHwndSource.AddHook(WndProc);
                // Defensive: ensure previous registration is cleared to avoid duplicate-id registration.
                try { UnregisterHotKey(_hotkeyHwndSource.Handle, HOTKEY_ID_OPEN_STUDY); } catch { }
                var ok = RegisterHotKey(_hotkeyHwndSource.Handle, HOTKEY_ID_OPEN_STUDY, _openStudyMods, _openStudyVk);
                System.Diagnostics.Debug.WriteLine(ok
                    ? $"[Hotkey] Registered OpenStudy hotkey '{text}' mods=0x{_openStudyMods:X} vk=0x{_openStudyVk:X}"
                    : $"[Hotkey] Failed to register OpenStudy hotkey '{text}' (may be in use) mods=0x{_openStudyMods:X} vk=0x{_openStudyVk:X}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Hotkey] Register exception: " + ex.Message);
            }
        }

        // Parse hotkey text (e.g., "Ctrl+Alt+O") to user32 fsModifiers + vk.
        // Fix: Use WPF KeyConverter first, then explicit A?Z/0?9 fallback.
        // The previous fallback used Windows Forms constants incorrectly; this could yield vk=0 and prevent registration.
        private bool TryParseHotkey(string text, out uint mods, out uint vk)
        {
            mods = 0; vk = 0;
            try
            {
                var parts = text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length == 0) return false;
                string keyPart = parts[^1];
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    var p = parts[i];
                    if (p.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || p.Equals("Control", StringComparison.OrdinalIgnoreCase)) mods |= MOD_CONTROL;
                    else if (p.Equals("Alt", StringComparison.OrdinalIgnoreCase)) mods |= MOD_ALT;
                    else if (p.Equals("Shift", StringComparison.OrdinalIgnoreCase)) mods |= MOD_SHIFT; // NEW: Support Shift modifier
                    else if (p.Equals("Win", StringComparison.OrdinalIgnoreCase) || p.Equals("Windows", StringComparison.OrdinalIgnoreCase)) mods |= MOD_WIN;
                }
                // Convert key string to virtual key code
                var kc = new System.Windows.Input.KeyConverter();
                var keyObj = kc.ConvertFromString(keyPart);
                if (keyObj is Key key && key != Key.None)
                {
                    vk = (uint)KeyInterop.VirtualKeyFromKey(key);
                    return vk != 0;
                }
                // Fallback: try letters/digits explicitly
                if (keyPart.Length == 1)
                {
                    char c = char.ToUpperInvariant(keyPart[0]);
                    if (c >= 'A' && c <= 'Z') { vk = (uint)c; return true; }
                    if (c >= '0' && c <= '9') { vk = (uint)c; return true; }
                }
                return false;
            }
            catch { return false; }
        }

        private void TryRegisterToggleSyncTextHotkey()
        {
            try
            {
                var app = (App)Application.Current;
                var local = app.Services.GetService<IRadiumLocalSettings>();
                var text = local?.GlobalHotkeyToggleSyncText;
                if (string.IsNullOrWhiteSpace(text)) return;

                // Parse persisted hotkey text (e.g., "Ctrl+Alt+T") into user32 modifiers + virtual key
                if (!TryParseHotkey(text!, out _toggleSyncTextMods, out _toggleSyncTextVk)) return;

                // Acquire current HwndSource for this window; it exists only after OnSourceInitialized
                _hotkeyHwndSource = (HwndSource?)PresentationSource.FromVisual(this);
                if (_hotkeyHwndSource == null) return;
                // Defensive: ensure previous registration is cleared to avoid duplicate-id registration
                try { UnregisterHotKey(_hotkeyHwndSource.Handle, HOTKEY_ID_TOGGLE_SYNC_TEXT); } catch { }
                var ok = RegisterHotKey(_hotkeyHwndSource.Handle, HOTKEY_ID_TOGGLE_SYNC_TEXT, _toggleSyncTextMods, _toggleSyncTextVk);
                System.Diagnostics.Debug.WriteLine(ok
                    ? $"[Hotkey] Registered ToggleSyncText hotkey '{text}' mods=0x{_toggleSyncTextMods:X} vk=0x{_toggleSyncTextVk:X}"
                    : $"[Hotkey] Failed to register ToggleSyncText hotkey '{text}' (may be in use) mods=0x{_toggleSyncTextMods:X} vk=0x{_toggleSyncTextVk:X}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Hotkey] ToggleSyncText register exception: " + ex.Message);
            }
        }

        private void TryRegisterSendReportHotkey()
        {
            try
            {
                var app = (App)Application.Current;
                var local = app.Services.GetService<IRadiumLocalSettings>();
                var text = local?.GlobalHotkeySendStudy;
                if (string.IsNullOrWhiteSpace(text)) return;

                // Parse persisted hotkey text (e.g., "Ctrl+Decimal") into user32 modifiers + virtual key
                if (!TryParseHotkey(text!, out _sendReportMods, out _sendReportVk)) return;

                // Acquire current HwndSource for this window; it exists only after OnSourceInitialized
                _hotkeyHwndSource = (HwndSource?)PresentationSource.FromVisual(this);
                if (_hotkeyHwndSource == null) return;
                // Defensive: ensure previous registration is cleared to avoid duplicate-id registration
                try { UnregisterHotKey(_hotkeyHwndSource.Handle, HOTKEY_ID_SEND_REPORT); } catch { }
                var ok = RegisterHotKey(_hotkeyHwndSource.Handle, HOTKEY_ID_SEND_REPORT, _sendReportMods, _sendReportVk);
                System.Diagnostics.Debug.WriteLine(ok
                    ? $"[Hotkey] Registered SendReport hotkey '{text}' mods=0x{_sendReportMods:X} vk=0x{_sendReportVk:X}"
                    : $"[Hotkey] Failed to register SendReport hotkey '{text}' (may be in use) mods=0x{_sendReportMods:X} vk=0x{_sendReportVk:X}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Hotkey] SendReport register exception: " + ex.Message);
            }
        }

        // Global message hook to receive WM_HOTKEY from user32.
        // When our registered id fires, route to ViewModel methods
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (id == HOTKEY_ID_OPEN_STUDY)
                {
                    if (DataContext is MainViewModel vm)
                        vm.RunOpenStudyShortcut();
                    handled = true;
                }
                else if (id == HOTKEY_ID_TOGGLE_SYNC_TEXT)
                {
                    if (DataContext is MainViewModel vm)
                    {
                        // Toggle the TextSyncEnabled property
                        vm.TextSyncEnabled = !vm.TextSyncEnabled;
                        System.Diagnostics.Debugger.Log(0, "Hotkey", $"[Hotkey] ToggleSyncText executed - new state: {vm.TextSyncEnabled}\n");
                    }
                    handled = true;
                }
                else if (id == HOTKEY_ID_SEND_REPORT)
                {
                    if (DataContext is MainViewModel vm)
                    {
                        vm.RunSendReportShortcut();
                        System.Diagnostics.Debug.WriteLine("[Hotkey] SendReport executed");
                    }
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        // Properly unregister and unhook on close to avoid leaking a system-wide hotkey registration.
        protected override void OnClosed(EventArgs e)
        {
            try
            {
                if (_hotkeyHwndSource != null)
                {
                    UnregisterHotKey(_hotkeyHwndSource.Handle, HOTKEY_ID_OPEN_STUDY);
                    UnregisterHotKey(_hotkeyHwndSource.Handle, HOTKEY_ID_TOGGLE_SYNC_TEXT);
                    UnregisterHotKey(_hotkeyHwndSource.Handle, HOTKEY_ID_SEND_REPORT);
                    _hotkeyHwndSource.RemoveHook(WndProc);
                }
            }
            catch { }
            
            // NEW: Dispose autofocus service
            try
            {
                _autofocusService?.Dispose();
                System.Diagnostics.Debug.WriteLine("[MainWindow] EditorAutofocusService disposed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] Error disposing autofocus service: {ex.Message}");
            }
            
            base.OnClosed(e);
        }

        /// <summary>
        /// Public method to re-register global hotkeys without restart.
        /// Called from SettingsViewModel after saving new hotkey combinations.
        /// </summary>
        public void ReregisterGlobalHotkeys()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[MainWindow] Re-registering global hotkeys...");
                
                // Unregister existing hotkeys first
                if (_hotkeyHwndSource != null)
                {
                    try { UnregisterHotKey(_hotkeyHwndSource.Handle, HOTKEY_ID_OPEN_STUDY); } catch { }
                    try { UnregisterHotKey(_hotkeyHwndSource.Handle, HOTKEY_ID_TOGGLE_SYNC_TEXT); } catch { }
                    try { UnregisterHotKey(_hotkeyHwndSource.Handle, HOTKEY_ID_SEND_REPORT); } catch { }
                }
                
                // Re-register with new values
                TryRegisterOpenStudyHotkey();
                TryRegisterToggleSyncTextHotkey();
                TryRegisterSendReportHotkey();
                
                System.Diagnostics.Debug.WriteLine("[MainWindow] Global hotkeys re-registered successfully");
                
                // NEW: Restart autofocus service with new settings
                try
                {
                    var app = (App)Application.Current;
                    var localSettings = app.Services.GetService<IRadiumLocalSettings>();
                    if (localSettings != null)
                    {
                        // Stop existing service
                        _autofocusService?.Dispose();
                        _autofocusService = null;
                        
                        // Start new service if enabled
                        if (localSettings.EditorAutofocusEnabled)
                        {
                            _autofocusService = new EditorAutofocusService(
                                localSettings,
                                () =>
                                {
                                    // Focus callback
                                    System.Diagnostics.Debug.WriteLine("[MainWindow] Autofocus callback triggered");
                                    if (!this.IsActive) this.Activate();
                                    
                                    var editorControl = gridCenter.EditorFindings;
                                    if (editorControl != null)
                                    {
                                        var musmEditor = editorControl.FindName("Editor") as ICSharpCode.AvalonEdit.TextEditor;
                                        if (musmEditor != null)
                                        {
                                            musmEditor.Focus();
                                            musmEditor.TextArea?.Caret.BringCaretToView();
                                        }
                                        else
                                        {
                                            editorControl.Focus();
                                        }
                                    }
                                });
                        
                            _autofocusService.Start();
                            System.Diagnostics.Debug.WriteLine("[MainWindow] EditorAutofocusService restarted");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("[MainWindow] EditorAutofocusService disabled");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainWindow] Failed to restart EditorAutofocusService: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] Failed to re-register hotkeys: {ex.Message}");
                throw; // Let caller handle the error
            }
        }

        private void InitializeTripleClickSupport()
        {
            // Add global PreviewMouseLeftButtonDown handler for triple-click paragraph selection
            AddHandler(UIElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(OnPreviewMouseLeftButtonDown), true);
      }

        private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Only handle triple-click (ClickCount == 3)
         if (e.ClickCount != 3) return;

            // Find the TextBox ancestor from the original source
            TextBox? textBox = null;
            try
            {
                textBox = FindAncestor<TextBox>(e.OriginalSource as DependencyObject);
            }
            catch (InvalidOperationException)
            {
                // Non-visual OriginalSource (e.g., Run) can throw when walking visual tree
                return;
            }
            if (textBox == null) return;

            // Select paragraph at caret position
            SelectParagraphAtCaret(textBox);
    e.Handled = true; // Prevent default triple-click behavior (select all)
   }

      private void SelectParagraphAtCaret(TextBox textBox)
        {
if (string.IsNullOrEmpty(textBox.Text)) return;

        int caretIndex = textBox.CaretIndex;
            string text = textBox.Text;

            // Find line boundaries (delimited by single newlines or start/end of text)
            int lineStart = FindLineStart(text, caretIndex);
          int lineEnd = FindLineEnd(text, caretIndex);

      // Select the line
            textBox.Select(lineStart, lineEnd - lineStart);
        }

        private int FindLineStart(string text, int position)
        {
  // Search backwards for single newline or start of text
          for (int i = position - 1; i >= 0; i--)
      {
         // Check for newline (any style)
         if (text[i] == '\n')
    {
       // Return position after the newline
        return i + 1;
         }
   }
       // No line boundary found, return start of text
    return 0;
        }

        private int FindLineEnd(string text, int position)
    {
            // Search forwards for single newline or end of text
       for (int i = position; i < text.Length; i++)
            {
 // Check for newline (any style)
          if (text[i] == '\r' || text[i] == '\n')
  {
 // Return position before the newline
    return i;
                }
    }
         // No line boundary found, return end of text
            return text.Length;
        }

        private int FindParagraphStart(string text, int position)
      {
     // Search backwards for double newline or start of text
            for (int i = position - 1; i >= 0; i--)
   {
                // Check for double newline (paragraph boundary)
 if (i > 0 && text[i] == '\n' && text[i - 1] == '\n')
          {
       // Return position after the double newline
       return i + 1;
                }
    // Check for \r\n\r\n
          if (i > 2 && text[i] == '\n' && text[i - 1] == '\r' && text[i - 2] == '\n' && text[i - 3] == '\r')
           {
    return i + 1;
      }
}
  // No paragraph boundary found, return start of text
        return 0;
        }

        private int FindParagraphEnd(string text, int position)
    {
        // Search forwards for double newline or end of text
       for (int i = position; i < text.Length; i++)
            {
                // Check for double newline (paragraph boundary)
    if (i < text.Length - 1 && text[i] == '\n' && text[i + 1] == '\n')
                {
           // Return position before the double newline
           return i;
           }
        // Check for \r\n\r\n
   if (i < text.Length - 3 && text[i] == '\r' && text[i + 1] == '\n' && text[i + 2] == '\r' && text[i + 3] == '\n')
     {
return i;
        }
  }
        // No paragraph boundary found, return end of text
 return text.Length;
        }

        private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T match) return match;
                try
                {
                    current = GetParentObject(current);
                }
                catch (InvalidOperationException)
                {
                    // Defensive: VisualTreeHelper can throw for non-visuals (e.g., Run)
                    return null;
                }
            }
            return null;
        }

        // Safe parent lookup that supports non-Visuals (Run/Inline, FlowDocument, ContentElement)
        private static DependencyObject? GetParentObject(DependencyObject? child)
        {
            if (child == null) return null;

            // Visual or 3D visual
            if (child is Visual || child is Visual3D)
                return VisualTreeHelper.GetParent(child);

            // FrameworkContentElement (e.g. Run, Paragraph in FlowDocument)
            if (child is FrameworkContentElement fce)
                return fce.Parent;

            // ContentElement (non-visual); try logical parent first
            if (child is ContentElement ce)
            {
                var logical = ContentOperations.GetParent(ce);
                if (logical != null) return logical;
            }

            // Fallback to logical tree parent
            return LogicalTreeHelper.GetParent(child);
        }
    }
}
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Globalization;
using System.Windows.Data;
using Wysg.Musm.Radium.ViewModels;
using Wysg.Musm.Radium.Services;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;

namespace Wysg.Musm.Radium.Views
{
    /// <summary>
    /// Converter for Active boolean to button text.
    /// </summary>
    public class ActiveToButtonTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool active)
            {
                return active ? "Deactivate" : "Activate";
            }
            return "Toggle";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter for bool to Visibility with optional inverse parameter.
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = value is bool b && b;
            bool inverse = parameter?.ToString()?.Equals("Inverse", StringComparison.OrdinalIgnoreCase) == true;
            
            if (inverse)
                boolValue = !boolValue;
                
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public partial class SettingsWindow : Window, INotifyPropertyChanged
    {
        private Border? _dragGhost; private string? _dragItem; private ListBox? _dragSource; private Border? _dropIndicator;
        private readonly bool _databaseOnly;
        private readonly ITenantContext? _tenantContext;
        private string _currentPacsKey = "default_pacs";

        // Expose AccountId for binding
        public long AccountId => _tenantContext?.AccountId ?? 0;

        public event PropertyChangedEventHandler? PropertyChanged;
        public string CurrentPacsKey
        {
            get => _currentPacsKey;
            private set
            {
                if (!string.Equals(_currentPacsKey, value, StringComparison.Ordinal))
                {
                    _currentPacsKey = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentPacsKey)));
                }
            }
        }

        public SettingsWindow(bool databaseOnly = false)
        {
            _databaseOnly = databaseOnly;
            InitializeComponent();
            if (Application.Current is App app)
            {
                var vm = app.Services.GetRequiredService<SettingsViewModel>();
                DataContext = vm;
                _tenantContext = app.Services.GetService<ITenantContext>();
                
                // Load PACS profiles from DB
                _ = vm.LoadPacsProfilesAsync();
                
                // Point ProcedureExecutor to current PACS-specific spy path
                TryApplyCurrentPacsSpyPath();
                try { CurrentPacsKey = string.IsNullOrWhiteSpace(_tenantContext?.CurrentPacsKey) ? "default_pacs" : _tenantContext!.CurrentPacsKey; } catch { }

                // Subscribe to account changes to update visibility
                if (_tenantContext != null)
                {
                    _tenantContext.AccountIdChanged += OnAccountIdChanged;
                    _tenantContext.PacsKeyChanged += OnPacsKeyChanged;
                }
            }
            else DataContext = new SettingsViewModel();
            InitializeAutomationLists();
            if (_databaseOnly) ApplyDatabaseOnlyMode();
            
            // Subscribe to window closing event to refresh phrase data
            Closing += OnWindowClosing;
        }
        
        public SettingsWindow(SettingsViewModel vm, bool databaseOnly = false)
        {
            _databaseOnly = databaseOnly;
            InitializeComponent(); 
            DataContext = vm; 
            
            InitializeAutomationLists(); 
            if (_databaseOnly) ApplyDatabaseOnlyMode();
            if (Application.Current is App app)
            {
                _tenantContext = app.Services.GetService<ITenantContext>();
                
                // Load PACS profiles from DB
                _ = vm.LoadPacsProfilesAsync();
                
                TryApplyCurrentPacsSpyPath();
                try { CurrentPacsKey = string.IsNullOrWhiteSpace(_tenantContext?.CurrentPacsKey) ? "default_pacs" : _tenantContext!.CurrentPacsKey; } catch { }

                // Subscribe to account changes
                if (_tenantContext != null)
                {
                    _tenantContext.AccountIdChanged += OnAccountIdChanged;
                    _tenantContext.PacsKeyChanged += OnPacsKeyChanged;
                }
            }
            
            // Subscribe to window closing event to refresh phrase data
            Closing += OnWindowClosing;
        }

        private void TryApplyCurrentPacsSpyPath()
        {
            try
            {
                if (_tenantContext == null) return;
                var pacsKey = string.IsNullOrWhiteSpace(_tenantContext.CurrentPacsKey) ? "default_pacs" : _tenantContext.CurrentPacsKey;
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var dir = System.IO.Path.Combine(appData, "Wysg.Musm", "Radium", "Pacs", SanitizeFileName(pacsKey));
                System.IO.Directory.CreateDirectory(dir);
                var spyPath = System.IO.Path.Combine(dir, "ui-procedures.json");
                ProcedureExecutor.SetProcPathOverride(() => spyPath);
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine("[SettingsWindow] Failed to set spy path: " + ex.Message);
            }
        }

        private static string SanitizeFileName(string name)
        {
            var invalid = System.IO.Path.GetInvalidFileNameChars();
            return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }

        private void OnAccountIdChanged(long oldId, long newId)
        {
            // Notify that AccountId property has changed
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AccountId)));
            Debug.WriteLine($"[SettingsWindow] AccountId changed from {oldId} to {newId}");
        }

        private void OnPacsKeyChanged(string oldKey, string newKey)
        {
            Debug.WriteLine($"[SettingsWindow] PACS key changed from '{oldKey}' to '{newKey}'");
            CurrentPacsKey = string.IsNullOrWhiteSpace(newKey) ? "default_pacs" : newKey;
            TryApplyCurrentPacsSpyPath();
            try
            {
                if (DataContext is SettingsViewModel vm)
                {
                    vm.SelectedPacsForAutomation = CurrentPacsKey;
                }
            }
            catch { }
        }

        private void ApplyDatabaseOnlyMode()
        {
            try
            {
                if (FindName("tabsRoot") is TabControl tabs)
                {
                    if (FindName("tabGeneral") is TabItem db) tabs.SelectedItem = db;
                    void Disable(string name) { if (FindName(name) is TabItem ti) ti.IsEnabled = false; }
                    Disable("tabAutomation");
                    Disable("tabReportify");
                    Disable("tabPhrases");
                    Disable("tabSpy");
                    Disable("tabGlobalPhrases");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[SettingsWindow] ApplyDatabaseOnlyMode error: " + ex.Message);
            }
        }

        // After DataContext set, call LoadAutomation and bind ListBoxes to VM collections
        private void InitializeAutomationLists()
        {
            if (DataContext is not SettingsViewModel vm) return;
            if (FindName("lstLibrary") is ListBox lib) lib.ItemsSource = vm.AvailableModules;
            if (FindName("lstNewStudy") is ListBox ns) ns.ItemsSource = vm.NewStudyModules;
            if (FindName("lstAddStudy") is ListBox add) add.ItemsSource = vm.AddStudyModules;
            if (FindName("lstShortcutOpenNew") is ListBox s1) s1.ItemsSource = vm.ShortcutOpenNewModules;
            if (FindName("lstShortcutOpenAdd") is ListBox s2) s2.ItemsSource = vm.ShortcutOpenAddModules;
            if (FindName("lstShortcutOpenAfterOpen") is ListBox s3) s3.ItemsSource = vm.ShortcutOpenAfterOpenModules;
            
            // LoadAutomation is now handled by LoadAutomationForPacs in the ViewModel
        }

        // Public method for child tabs to initialize their ListBoxes
        public void InitializeAutomationListBoxes(ListBox newStudy, ListBox addStudy, ListBox library, 
            ListBox shortcutOpenNew, ListBox shortcutOpenAdd, ListBox shortcutOpenAfterOpen, ListBox sendReport,
            ListBox sendReportPreview, ListBox shortcutSendReportPreview, ListBox shortcutSendReportReportified, ListBox test)
        {
            if (DataContext is not SettingsViewModel vm) return;
            library.ItemsSource = vm.AvailableModules;
            newStudy.ItemsSource = vm.NewStudyModules;
            addStudy.ItemsSource = vm.AddStudyModules;
            shortcutOpenNew.ItemsSource = vm.ShortcutOpenNewModules;
            shortcutOpenAdd.ItemsSource = vm.ShortcutOpenAddModules;
            shortcutOpenAfterOpen.ItemsSource = vm.ShortcutOpenAfterOpenModules;
            sendReport.ItemsSource = vm.SendReportModules;
            sendReportPreview.ItemsSource = vm.SendReportPreviewModules;
            shortcutSendReportPreview.ItemsSource = vm.ShortcutSendReportPreviewModules;
            shortcutSendReportReportified.ItemsSource = vm.ShortcutSendReportReportifiedModules;
            test.ItemsSource = vm.TestModules;
        }

        private Point _dragStart;
        // Modify OnProcDrag to record source index
        private int _dragSourceIndex = -1;
        public void OnProcDrag(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            if (sender is ListBox lb && lb.SelectedItem is string item)
            {
                if ((e.GetPosition(lb) - _dragStart).Length < 6) return;
                _dragItem = item; _dragSource = lb; _dragSourceIndex = lb.SelectedIndex;
                Debug.WriteLine($"[AutoDrag] start item='{item}' src={_dragSource?.Name} idx={_dragSourceIndex}");
                CreateGhost(item, e.GetPosition(this));
                DragDrop.DoDragDrop(lb, new DataObject("musm-proc", item), DragDropEffects.Move | DragDropEffects.Copy);
                RemoveGhost();
            }
        }
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e) { base.OnPreviewMouseLeftButtonDown(e); _dragStart = e.GetPosition(this); }

        // Update OnProcDrop implementation for proper library behavior and indicator clearing
        public void OnProcDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("musm-proc")) { Debug.WriteLine("[AutoDrop] no data"); ClearDropIndicator(); RemoveGhost(); return; }
            if (sender is not ListBox target) { Debug.WriteLine("[AutoDrop] target not list"); ClearDropIndicator(); RemoveGhost(); return; }
            string item = (string)e.Data.GetData("musm-proc")!;
            var targetList = GetListForListBox(target);
            if (targetList == null) { Debug.WriteLine("[AutoDrop] target list null"); ClearDropIndicator(); RemoveGhost(); return; }

            bool fromLibrary = _dragSource?.Name == "lstLibrary";
            bool sameList = _dragSource == target;
            Debug.WriteLine($"[AutoDrop] item='{item}' fromLib={fromLibrary} sameList={sameList} target={target.Name}");

            if (target.Name == "lstLibrary")
            {
                // Always keep library copy; do not remove from source (copy semantics)
                if (!targetList.Contains(item)) targetList.Add(item);
                ClearDropIndicator(); RemoveGhost(); return;
            }

            int insertIndex = GetItemInsertIndex(target, e.GetPosition(target));
            if (insertIndex < 0 || insertIndex > targetList.Count) insertIndex = targetList.Count;

            if (sameList && !fromLibrary)
            {
                // Move inside same list (reorder)
                if (_dragSourceIndex >= 0 && _dragSourceIndex < targetList.Count)
                {
                    if (insertIndex > _dragSourceIndex) insertIndex--; // account for removal
                    targetList.RemoveAt(_dragSourceIndex);
                }
                targetList.Insert(insertIndex, item);
            }
            else if (fromLibrary)
            {
                // Copy from library (allow duplicates)
                targetList.Insert(insertIndex, item);
            }
            else
            {
                // Move from other ordered list
                var srcList = GetListForListBox(_dragSource!);
                if (srcList != null && _dragSourceIndex >= 0 && _dragSourceIndex < srcList.Count)
                {
                    // remove the specific source instance at index (even if duplicates exist)
                    srcList.RemoveAt(_dragSourceIndex);
                }
                targetList.Insert(insertIndex, item);
            }
            ClearDropIndicator(); RemoveGhost();
        }

        private void CreateGhost(string text, Point pos)
        {
            RemoveGhost(); _dragGhost = new Border { Background = new SolidColorBrush(Color.FromArgb(180, 60, 120, 255)), BorderBrush = Brushes.DodgerBlue, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(3), Padding = new Thickness(6,2,6,2), Child = new TextBlock { Text = text, Foreground = Brushes.White }, IsHitTestVisible = false };
            AddChildToOverlay(_dragGhost, pos);
        }
        private void AddChildToOverlay(FrameworkElement fe, Point pos)
        { if (Content is not Grid root) return; if (root.Children.OfType<Canvas>().FirstOrDefault(c => c.Name == "GhostCanvas") is not Canvas canvas) { canvas = new Canvas { Name = "GhostCanvas", IsHitTestVisible = false }; root.Children.Add(canvas); } canvas.Children.Add(fe); Canvas.SetLeft(fe, pos.X + 8); Canvas.SetTop(fe, pos.Y + 8); }
        private void RemoveGhost() { if (Content is not Grid root) return; if (_dragGhost == null) return; var canvas = root.Children.OfType<Canvas>().FirstOrDefault(c => c.Name == "GhostCanvas"); canvas?.Children.Remove(_dragGhost); _dragGhost = null; _dragItem = null; _dragSource = null; }

        protected override void OnMouseMove(MouseEventArgs e)
        { base.OnMouseMove(e); if (_dragGhost != null && Content is Grid root) { var pos = e.GetPosition(root); Canvas.SetLeft(_dragGhost, pos.X + 8); Canvas.SetTop(_dragGhost, pos.Y + 8); } }

        protected override void OnDragOver(DragEventArgs e)
        {
            base.OnDragOver(e); if (!e.Data.GetDataPresent("musm-proc")) { ClearDropIndicator(); return; } if (_dragGhost != null && Content is Grid root) { var pos = e.GetPosition(root); Canvas.SetLeft(_dragGhost, pos.X + 8); Canvas.SetTop(_dragGhost, pos.Y + 8); }
            if (e.OriginalSource is DependencyObject d)
            {
                var lb = FindAncestor<ListBox>(d); if (lb != null && (lb.Name == "lstNewStudy" || lb.Name == "lstAddStudy")) { EnsureDropIndicator(lb); var p = e.GetPosition(lb); int idx = GetItemInsertIndex(lb, p); PositionDropIndicator(lb, idx); }
                else ClearDropIndicator();
            }
        }
        protected override void OnDragLeave(DragEventArgs e) { base.OnDragLeave(e); }
        protected override void OnDrop(DragEventArgs e)
        { base.OnDrop(e); ClearDropIndicator(); RemoveGhost(); }

        private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
        { while (current != null) { if (current is T t) return t; current = VisualTreeHelper.GetParent(current); } return null; }

        private ObservableCollection<string>? GetListForListBox(ListBox lb)
        {
            // Harden against null DataContext or unexpected types (bug fix FR-234)
            try
            {
                if (lb == null) return null;
                if (DataContext is not SettingsViewModel vm)
                {
                    Debug.WriteLine("[AutoGetList] DataContext not SettingsViewModel; fallback to ItemsSource");
                    return lb.ItemsSource as ObservableCollection<string>;
                }
                var list = lb.Name switch
                {
                    "lstLibrary" => vm.AvailableModules,
                    "lstNewStudy" => vm.NewStudyModules,
                    "lstAddStudy" => vm.AddStudyModules,
                    "lstShortcutOpenNew" => vm.ShortcutOpenNewModules,
                    "lstShortcutOpenAdd" => vm.ShortcutOpenAddModules,
                    "lstShortcutOpenAfterOpen" => vm.ShortcutOpenAfterOpenModules,
                    "lstSendReport" => vm.SendReportModules,
                    "lstSendReportPreview" => vm.SendReportPreviewModules,
                    "lstShortcutSendReportPreview" => vm.ShortcutSendReportPreviewModules,
                    "lstShortcutSendReportReportified" => vm.ShortcutSendReportReportifiedModules,
                    "lstTest" => vm.TestModules,
                    _ => null
                };
                if (list == null)
                {
                    Debug.WriteLine($"[AutoGetList] Unknown list name '{lb.Name}', using ItemsSource fallback");
                    list = lb.ItemsSource as ObservableCollection<string>;
                }
                return list;
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine("[AutoGetList] error: " + ex.Message);
                return lb?.ItemsSource as ObservableCollection<string>;
            }
        }

        private ObservableCollection<string>? GetListForItem(string item)
        {
            if (DataContext is not SettingsViewModel vm) return null;
            if (vm.NewStudyModules.Contains(item)) return vm.NewStudyModules;
            if (vm.AddStudyModules.Contains(item)) return vm.AddStudyModules;
            if (vm.AvailableModules.Contains(item)) return vm.AvailableModules;
            return null;
        }

        private int GetItemInsertIndex(ListBox lb, Point pos) { for (int i = 0; i < lb.Items.Count; i++) { if (lb.ItemContainerGenerator.ContainerFromIndex(i) is FrameworkElement fe) { var r = fe.TransformToAncestor(lb).TransformBounds(new Rect(0,0,fe.ActualWidth,fe.ActualHeight)); if (pos.Y < r.Top + r.Height/2) return i; } } return lb.Items.Count; }

        private void EnsureDropIndicator(ListBox host)
        {
            if (_dropIndicator != null) return; if (Content is not Grid root) return; if (root.Children.OfType<Canvas>().FirstOrDefault(c => c.Name == "GhostCanvas") is not Canvas canvas) { canvas = new Canvas { Name = "GhostCanvas", IsHitTestVisible = false }; root.Children.Add(canvas); }
            _dropIndicator = new Border { Height = 2, Background = Brushes.OrangeRed, IsHitTestVisible = false, Opacity = 0.85 }; canvas.Children.Add(_dropIndicator);
        }
        private void PositionDropIndicator(ListBox lb, int index)
        {
            if (_dropIndicator == null) return; if (Content is not Grid root) return; var canvas = root.Children.OfType<Canvas>().FirstOrDefault(c => c.Name == "GhostCanvas"); if (canvas == null) return; double y = 0; if (index >= lb.Items.Count) { if (lb.Items.Count > 0) { if (lb.ItemContainerGenerator.ContainerFromIndex(lb.Items.Count -1) is FrameworkElement last) { var b = last.TransformToAncestor(lb).TransformBounds(new Rect(0,0,last.ActualWidth,last.ActualHeight)); y = b.Bottom + 1; } } } else { if (lb.ItemContainerGenerator.ContainerFromIndex(index) is FrameworkElement item) { var b = item.TransformToAncestor(lb).TransformBounds(new Rect(0,0,item.ActualWidth,item.ActualHeight)); y = b.Top - 1; } }
            var lbPos = lb.TransformToAncestor(root).Transform(new Point(0,0)); Canvas.SetLeft(_dropIndicator, lbPos.X + 4); Canvas.SetTop(_dropIndicator, lbPos.Y + y); _dropIndicator.Width = lb.ActualWidth - 8;
        }
        private void ClearDropIndicator()
        {
            if (_dropIndicator == null) return;
            if (Content is not Grid root) return;
            var canvas = root.Children.OfType<Canvas>().FirstOrDefault(c => c.Name == "GhostCanvas");
            canvas?.Children.Remove(_dropIndicator); _dropIndicator = null;
            Debug.WriteLine("[AutoDrag] drop indicator cleared");
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonUp(e);
            ClearDropIndicator();
            RemoveGhost();
        }

        protected override void OnSourceInitialized(System.EventArgs e)
        {
            base.OnSourceInitialized(e);
            TryEnableDarkTitleBar();
        }

        private void TryEnableDarkTitleBar()
        {
            try
            {
                var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                if (hwnd == System.IntPtr.Zero) return;
                int useImmersiveDarkMode = 1;
                const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
                const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
                if (DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useImmersiveDarkMode, sizeof(int)) != 0)
                {
                    DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref useImmersiveDarkMode, sizeof(int));
                }
            }
            catch { }
        }

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(System.IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        // Add handler for remove button click
        public void OnRemoveModuleClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is not Button btn) return;
                var listBoxItem = FindAncestor<ListBoxItem>(btn);
                if (listBoxItem?.DataContext is not string module) return;
                var parentListBox = FindAncestor<ListBox>(btn);
                if (parentListBox == null) { Debug.WriteLine("[AutoRemove] parent listbox not found"); return; }
                var list = GetListForListBox(parentListBox) ?? parentListBox.ItemsSource as ObservableCollection<string>;
                if (list == null) { Debug.WriteLine("[AutoRemove] backing list null"); return; }
                int idx = parentListBox.ItemContainerGenerator.IndexFromContainer(listBoxItem);
                if (idx >= 0 && idx < list.Count)
                {
                    list.RemoveAt(idx);
                    Debug.WriteLine($"[AutoRemove] removed '{module}' idx={idx} from {parentListBox.Name}");
                }
                else
                {
                    // fallback: remove first matching instance
                    if (list.Remove(module)) Debug.WriteLine($"[AutoRemove] removed '{module}' by value from {parentListBox.Name}");
                }
            }
            catch (System.Exception ex) { Debug.WriteLine("[AutoRemove] error " + ex.Message); }
        }

        // Clear indicator when leaving list area
        public void OnListDragLeave(object sender, DragEventArgs e)
        {
            // When pointer leaves a list's visual tree, clear indicator.
            ClearDropIndicator();
        }

        // ---- Integrated Spy Tab Handlers (minimal reuse) ----
        private Views.AutomationWindow? _spyDelegate; // lazy delegate instance to reuse existing logic
        private AutomationWindow EnsureSpyDelegate()
        {
            if (_spyDelegate == null) { _spyDelegate = new AutomationWindow(); _spyDelegate.Hide(); }
            return _spyDelegate;
        }
        private void Spy_OnPick(object sender, RoutedEventArgs e) => EnsureSpyDelegate().GetType().GetMethod("OnPick", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.Invoke(_spyDelegate, new object?[]{sender,e});
        private void Spy_OnKnownSelectionChanged(object sender, SelectionChangedEventArgs e) => EnsureSpyDelegate().GetType().GetMethod("OnKnownSelectionChanged", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.Invoke(_spyDelegate, new object?[]{sender,e});
        private void Spy_OnReload(object sender, RoutedEventArgs e) => EnsureSpyDelegate().GetType().GetMethod("OnReload", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.Invoke(_spyDelegate, new object?[]{sender,e});
        private void Spy_OnAncestrySelected(object sender, RoutedPropertyChangedEventArgs<object> e) => EnsureSpyDelegate().GetType().GetMethod("OnAncestrySelected", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.Invoke(_spyDelegate, new object?[]{sender,e});
        private void Spy_OnValidateChain(object sender, RoutedEventArgs e) => EnsureSpyDelegate().GetType().GetMethod("OnValidateChain", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.Invoke(_spyDelegate, new object?[]{sender,e});
        private void Spy_OnInvoke(object sender, RoutedEventArgs e) => EnsureSpyDelegate().GetType().GetMethod("OnInvoke", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.Invoke(_spyDelegate, new object?[]{sender,e});
        private void Spy_OnGetText(object sender, RoutedEventArgs e) => EnsureSpyDelegate().GetType().GetMethod("OnGetText", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.Invoke(_spyDelegate, new object?[]{sender,e});
        private void Spy_OnGetName(object sender, RoutedEventArgs e) => EnsureSpyDelegate().GetType().GetMethod("OnGetName", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.Invoke(_spyDelegate, new object?[]{sender,e});
        private void Spy_OnGetSelectedRow(object sender, RoutedEventArgs e) => EnsureSpyDelegate().GetType().GetMethod("OnGetSelectedRow", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.Invoke(_spyDelegate, new object?[]{sender,e});
        private void Spy_OnSaveEdited(object sender, RoutedEventArgs e) => EnsureSpyDelegate().GetType().GetMethod("OnSaveEdited", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.Invoke(_spyDelegate, new object?[]{sender,e});
        private void Spy_OnAddProcRow(object sender, RoutedEventArgs e) => EnsureSpyDelegate().GetType().GetMethod("OnAddProcRow", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.Invoke(_spyDelegate, new object?[]{sender,e});
        private void Spy_OnSaveProcedure(object sender, RoutedEventArgs e) => EnsureSpyDelegate().GetType().GetMethod("OnSaveProcedure", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.Invoke(_spyDelegate, new object?[]{sender,e});
        private void Spy_OnRunProcedure(object sender, RoutedEventArgs e) => EnsureSpyDelegate().GetType().GetMethod("OnRunProcedure", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.Invoke(_spyDelegate, new object?[]{sender,e});

        // New: open Spy window directly from Settings Automation tab
        public void OnOpenSpy(object sender, RoutedEventArgs e)
        {
            AutomationWindow.ShowInstance();
        }

        // Keyboard hotkey capture: capture modifiers + key and write as string into bound TextBox
        public void OnHotkeyTextBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox tb) return;
            e.Handled = true; // prevent beep / text input

            // Determine modifiers and key
            var mods = new System.Collections.Generic.List<string>();
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) mods.Add("Ctrl");
            if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)) mods.Add("Alt");
            if (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin)) mods.Add("Win");
            // NEW: Support Shift modifier capture
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) mods.Add("Shift");

            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LWin || key == Key.RWin ||
                key == Key.LeftShift || key == Key.RightShift)
            {
                // Only modifier pressed; keep showing mods only
                tb.Text = string.Join("+", mods);
                return;
            }
            var keyStr = key.ToString();
            // Normalize OEM keys if needed
            if (key >= Key.A && key <= Key.Z) keyStr = keyStr.ToUpperInvariant();
            var combo = mods.Count > 0 ? string.Join("+", mods) + "+" + keyStr : keyStr;
            tb.Text = combo;
        }

        /// <summary>
        /// Refresh phrase snapshot and completion cache when settings window closes.
        /// This ensures the editor's phrase highlighting and autocomplete data is up to date
        /// after any phrase modifications in the Settings tabs.
        /// </summary>
        private async void OnWindowClosing(object? sender, CancelEventArgs e)
        {
            try
            {
                if (_tenantContext == null || Application.Current is not App app)
                {
                    Debug.WriteLine("[SettingsWindow] OnWindowClosing: no tenant context or app, skipping refresh");
                    return;
                }

                var accountId = _tenantContext.AccountId;
                if (accountId <= 0)
                {
                    Debug.WriteLine("[SettingsWindow] OnWindowClosing: no valid account, skipping refresh");
                    return;
                }

                Debug.WriteLine("[SettingsWindow] OnWindowClosing: refreshing phrase data for account {0}", accountId);

                // Get services
                var phraseService = app.Services.GetService<IPhraseService>();
                var phraseCache = app.Services.GetService<IPhraseCache>();

                if (phraseService == null || phraseCache == null)
                {
                    Debug.WriteLine("[SettingsWindow] OnWindowClosing: required services not available");
                    return;
                }

                // Refresh both account-specific and global phrases in the service's in-memory snapshot
                await phraseService.RefreshPhrasesAsync(accountId).ConfigureAwait(false);
                await phraseService.RefreshGlobalPhrasesAsync().ConfigureAwait(false);

                // Clear completion cache so it rebuilds with fresh combined phrases
                phraseCache.Clear(accountId);
                phraseCache.Clear(-1); // Clear global cache as well

                Debug.WriteLine("[SettingsWindow] OnWindowClosing: phrase data refresh completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[SettingsWindow] OnWindowClosing: error refreshing phrase data - {0}", ex.Message);
            }
        }
    }
}

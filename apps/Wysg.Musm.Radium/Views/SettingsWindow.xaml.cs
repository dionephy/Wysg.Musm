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

    public partial class SettingsWindow : Window, INotifyPropertyChanged
    {
        private Border? _dragGhost; private string? _dragItem; private ListBox? _dragSource; private Border? _dropIndicator;
        private readonly bool _databaseOnly;
        private readonly ITenantContext? _tenantContext;

        // Expose AccountId for binding
        public long AccountId => _tenantContext?.AccountId ?? 0;

        public event PropertyChangedEventHandler? PropertyChanged;

        public SettingsWindow(bool databaseOnly = false)
        {
            _databaseOnly = databaseOnly;
            InitializeComponent();
            if (Application.Current is App app)
            {
                var vm = app.Services.GetRequiredService<SettingsViewModel>();
                DataContext = vm;
                _tenantContext = app.Services.GetService<ITenantContext>();
                
                // Subscribe to account changes to update visibility
                if (_tenantContext != null)
                {
                    _tenantContext.AccountIdChanged += OnAccountIdChanged;
                }
            }
            else DataContext = new SettingsViewModel();
            InitializeAutomationLists();
            if (_databaseOnly) ApplyDatabaseOnlyMode();
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
                
                // Subscribe to account changes
                if (_tenantContext != null)
                {
                    _tenantContext.AccountIdChanged += OnAccountIdChanged;
                }
            }
        }

        private void OnAccountIdChanged(long oldId, long newId)
        {
            // Notify that AccountId property has changed
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AccountId)));
            Debug.WriteLine($"[SettingsWindow] AccountId changed from {oldId} to {newId}");
        }

        private void ApplyDatabaseOnlyMode()
        {
            try
            {
                if (FindName("tabsRoot") is TabControl tabs)
                {
                    if (FindName("tabDatabase") is TabItem db) tabs.SelectedItem = db;
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
            vm.LoadAutomation();
        }

        private Point _dragStart;
        // Modify OnProcDrag to record source index
        private int _dragSourceIndex = -1;
        private void OnProcDrag(object sender, MouseEventArgs e)
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
        private void OnProcDrop(object sender, DragEventArgs e)
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
        private void OnRemoveModuleClick(object sender, RoutedEventArgs e)
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
        private void OnListDragLeave(object sender, DragEventArgs e)
        {
            // When pointer leaves a list's visual tree, clear indicator.
            ClearDropIndicator();
        }

        private void OnPhrasesTabLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is SettingsViewModel svm)
            {
                if (svm.Phrases != null)
                {
                    Debug.WriteLine("[SettingsWindow] Setting phrases DataContext from SettingsViewModel.Phrases");
                    phrasesRoot.DataContext = svm.Phrases;
                }
                else if (Application.Current is App app)
                {
                    try
                    {
                        Debug.WriteLine("[SettingsWindow] SettingsViewModel.Phrases is null, getting PhrasesViewModel from DI");
                        var phrasesVm = app.Services.GetService<PhrasesViewModel>();
                        if (phrasesVm != null) 
                        {
                            phrasesRoot.DataContext = phrasesVm;
                            Debug.WriteLine("[SettingsWindow] Successfully set phrases DataContext from DI");
                        }
                        else
                        {
                            Debug.WriteLine("[SettingsWindow] Failed to get PhrasesViewModel from DI");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[SettingsWindow] Error getting PhrasesViewModel from DI: {ex.Message}");
                    }
                }
            }
            else
            {
                Debug.WriteLine($"[SettingsWindow] DataContext is not SettingsViewModel, it is: {DataContext?.GetType().Name ?? "null"}");
            }
        }

        private void OnGlobalPhrasesTabLoaded(object sender, RoutedEventArgs e)
        {
            if (Application.Current is App app)
            {
                try
                {
                    var vm = app.Services.GetService<GlobalPhrasesViewModel>();
                    if (vm != null)
                    {
                        globalPhrasesRoot.DataContext = vm;
                        Debug.WriteLine("[SettingsWindow] Successfully set GlobalPhrasesViewModel");
                    }
                    else
                    {
                        Debug.WriteLine("[SettingsWindow] Failed to get GlobalPhrasesViewModel from DI");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SettingsWindow] Error loading GlobalPhrasesViewModel: {ex.Message}");
                }
            }
        }

        private void OnHotkeysTabLoaded(object sender, RoutedEventArgs e)
        {
            if (Application.Current is App app)
            {
                try
                {
                    var vm = app.Services.GetService<HotkeysViewModel>();
                    if (vm != null)
                    {
                        hotkeysRoot.DataContext = vm;
                        Debug.WriteLine("[SettingsWindow] Successfully set HotkeysViewModel");
                    }
                    else
                    {
                        Debug.WriteLine("[SettingsWindow] Failed to get HotkeysViewModel from DI");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SettingsWindow] Error loading HotkeysViewModel: {ex.Message}");
                }
            }
        }

        private void OnSnippetsTabLoaded(object sender, RoutedEventArgs e)
        {
            if (Application.Current is App app)
            {
                try
                {
                    var vm = app.Services.GetService<SnippetsViewModel>();
                    if (vm != null)
                    {
                        snippetsRoot.DataContext = vm;
                        Debug.WriteLine("[SettingsWindow] Successfully set SnippetsViewModel");
                    }
                    else
                    {
                        Debug.WriteLine("[SettingsWindow] Failed to get SnippetsViewModel from DI");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SettingsWindow] Error loading SnippetsViewModel: {ex.Message}");
                }
            }
        }

        // ---- Integrated Spy Tab Handlers (minimal reuse) ----
        private Views.SpyWindow? _spyDelegate; // lazy delegate instance to reuse existing logic
        private SpyWindow EnsureSpyDelegate()
        {
            if (_spyDelegate == null) { _spyDelegate = new SpyWindow(); _spyDelegate.Hide(); }
            return _spyDelegate;
        }
        private void Spy_OnPick(object sender, RoutedEventArgs e) => EnsureSpyDelegate().GetType().GetMethod("OnPick", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.Invoke(_spyDelegate, new object?[]{sender,e});
        private void Spy_OnKnownSelectionChanged(object sender, SelectionChangedEventArgs e) => EnsureSpyDelegate().GetType().GetMethod("OnKnownSelectionChanged", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.Invoke(_spyDelegate, new object?[]{sender,e});
        private void Spy_OnMapSelected(object sender, RoutedEventArgs e) => EnsureSpyDelegate().GetType().GetMethod("OnMapSelected", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.Invoke(_spyDelegate, new object?[]{sender,e});
        private void Spy_OnResolveSelected(object sender, RoutedEventArgs e) => EnsureSpyDelegate().GetType().GetMethod("OnResolveSelected", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.Invoke(_spyDelegate, new object?[]{sender,e});
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
        private void OnOpenSpy(object sender, RoutedEventArgs e)
        {
            var win = new SpyWindow { Owner = this };
            win.Show();
        }
    }
}

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Wysg.Musm.Radium.ViewModels;
using Wysg.Musm.Radium.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Wysg.Musm.Radium.Views
{
    /// <summary>
    /// Partial class for Automation tab functionality in AutomationWindow
    /// </summary>
    public partial class AutomationWindow
    {
        private SettingsViewModel? _automationViewModel;
        private Point _automationDragStart;
        private bool _isAutomationDragging;
        private ObservableCollection<string> _customModules = new();
        private ObservableCollection<string> _builtinModules = new(); // Filtered built-in modules (excluding custom)
        
        // Drag-drop visual feedback fields
        private Border? _automationDragGhost;
        private string? _automationDragItem;
        private ListBox? _automationDragSource;
        private int _automationDragSourceIndex = -1;
        private Border? _automationDropIndicator;

        private void InitializeAutomationTab()
        {
            // Get SettingsViewModel from DI container
            if (Application.Current is App app)
            {
                _automationViewModel = app.Services.GetService<SettingsViewModel>();
                
                // Bind modalities textbox
                if (FindName("txtModalitiesNoHeaderUpdate") is TextBox txtModalities && _automationViewModel != null)
                {
                    txtModalities.SetBinding(TextBox.TextProperty, new System.Windows.Data.Binding("ModalitiesNoHeaderUpdate")
                    {
                        Source = _automationViewModel,
                        Mode = System.Windows.Data.BindingMode.TwoWay,
                        UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged
                    });
                }
                
                // Initialize ListBox bindings
                if (_automationViewModel != null)
                {
                    // Load custom modules first
                    LoadCustomModules();
                    
                    // Filter built-in modules (exclude custom modules)
                    RefreshBuiltinModules();
                    
                    if (FindName("lstLibrary") is ListBox lib)
                    {
                        lib.ItemsSource = _builtinModules; // Use filtered list
                        lib.PreviewMouseLeftButtonDown += OnAutomationListMouseDown;
                    }
                    if (FindName("lstNewStudy") is ListBox ns)
                    {
                        ns.ItemsSource = _automationViewModel.NewStudyModules;
                        ns.PreviewMouseLeftButtonDown += OnAutomationListMouseDown;
                    }
                    if (FindName("lstAddStudy") is ListBox add)
                    {
                        add.ItemsSource = _automationViewModel.AddStudyModules;
                        add.PreviewMouseLeftButtonDown += OnAutomationListMouseDown;
                    }
                    if (FindName("lstShortcutOpenNew") is ListBox s1)
                    {
                        s1.ItemsSource = _automationViewModel.ShortcutOpenNewModules;
                        s1.PreviewMouseLeftButtonDown += OnAutomationListMouseDown;
                    }
                    if (FindName("lstShortcutOpenAdd") is ListBox s2)
                    {
                        s2.ItemsSource = _automationViewModel.ShortcutOpenAddModules;
                        s2.PreviewMouseLeftButtonDown += OnAutomationListMouseDown;
                    }
                    if (FindName("lstShortcutOpenAfterOpen") is ListBox s3)
                    {
                        s3.ItemsSource = _automationViewModel.ShortcutOpenAfterOpenModules;
                        s3.PreviewMouseLeftButtonDown += OnAutomationListMouseDown;
                    }
                    if (FindName("lstSendReport") is ListBox sr)
                    {
                        sr.ItemsSource = _automationViewModel.SendReportModules;
                        sr.PreviewMouseLeftButtonDown += OnAutomationListMouseDown;
                    }
                    if (FindName("lstSendReportPreview") is ListBox srp)
                    {
                        srp.ItemsSource = _automationViewModel.SendReportPreviewModules;
                        srp.PreviewMouseLeftButtonDown += OnAutomationListMouseDown;
                    }
                    if (FindName("lstShortcutSendReportPreview") is ListBox ssrp)
                    {
                        ssrp.ItemsSource = _automationViewModel.ShortcutSendReportPreviewModules;
                        ssrp.PreviewMouseLeftButtonDown += OnAutomationListMouseDown;
                    }
                    if (FindName("lstShortcutSendReportReportified") is ListBox ssrr)
                    {
                        ssrr.ItemsSource = _automationViewModel.ShortcutSendReportReportifiedModules;
                        ssrr.PreviewMouseLeftButtonDown += OnAutomationListMouseDown;
                    }
                    if (FindName("lstTest") is ListBox test)
                    {
                        test.ItemsSource = _automationViewModel.TestModules;
                        test.PreviewMouseLeftButtonDown += OnAutomationListMouseDown;
                    }
                    
                    // Initialize Custom Modules
                    if (FindName("lstCustomModules") is ListBox customList)
                    {
                        customList.ItemsSource = _customModules;
                        customList.PreviewMouseLeftButtonDown += OnAutomationListMouseDown;
                    }
                }
            }
        }

        private void LoadCustomModules()
        {
            try
            {
                var store = CustomModuleStore.Load();
                _customModules.Clear();
                foreach (var module in store.Modules.OrderBy(m => m.Name))
                {
                    _customModules.Add(module.Name);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AutomationWindow] Error loading custom modules: {ex.Message}");
            }
        }
        
        private void RefreshBuiltinModules()
        {
            if (_automationViewModel == null) return;
            
            _builtinModules.Clear();
            
            // Get custom module names for filtering
            var customNames = new HashSet<string>(_customModules, StringComparer.OrdinalIgnoreCase);
            
            // Add only modules that are NOT in custom modules
            foreach (var module in _automationViewModel.AvailableModules)
            {
                if (!customNames.Contains(module))
                {
                    _builtinModules.Add(module);
                }
            }
        }

        private void OnCreateModule(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new CreateModuleWindow
                {
                    Owner = this
                };
                
                if (dialog.ShowDialog() == true && dialog.Result != null)
                {
                    var store = CustomModuleStore.Load();
                    
                    try
                    {
                        store.AddModule(dialog.Result);
                        CustomModuleStore.Save(store);
                        
                        // Refresh the custom modules list
                        LoadCustomModules();
                        
                        // Refresh built-in modules (remove newly created custom module)
                        RefreshBuiltinModules();
                        
                        // Also add to SettingsViewModel.AvailableModules if not present
                        if (_automationViewModel != null && 
                            !_automationViewModel.AvailableModules.Contains(dialog.Result.Name))
                        {
                            _automationViewModel.AvailableModules.Add(dialog.Result.Name);
                        }
                        
                        MessageBox.Show($"Custom module '{dialog.Result.Name}' created successfully.", 
                            "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (InvalidOperationException ex)
                    {
                        MessageBox.Show(ex.Message, "Error", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AutomationWindow] Error creating module: {ex.Message}");
                MessageBox.Show($"Error creating module: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnDeleteCustomModule(object sender, RoutedEventArgs e)
        {
            try
            {
                if (FindName("lstCustomModules") is not ListBox list) return;
                if (list.SelectedItem is not string moduleName) return;
                
                var result = MessageBox.Show(
                    $"Are you sure you want to delete custom module '{moduleName}'?\n\n" +
                    $"This will remove it from the available modules list.\n" +
                    $"Note: It will NOT be removed from existing automation sequences automatically.",
                    "Delete Custom Module",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                
                if (result != MessageBoxResult.Yes) return;
                
                var store = CustomModuleStore.Load();
                if (store.RemoveModule(moduleName))
                {
                    CustomModuleStore.Save(store);
                    
                    // Refresh UI
                    LoadCustomModules();
                    RefreshBuiltinModules();
                    
                    // Also remove from SettingsViewModel.AvailableModules
                    if (_automationViewModel != null && _automationViewModel.AvailableModules.Contains(moduleName))
                    {
                        _automationViewModel.AvailableModules.Remove(moduleName);
                    }
                    
                    MessageBox.Show($"Custom module '{moduleName}' deleted successfully.", 
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Failed to delete module '{moduleName}'.", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AutomationWindow] Error deleting module: {ex.Message}");
                MessageBox.Show($"Error deleting module: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnCustomModulesContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            try
            {
                if (sender is not ListBox list) return;
                
                // Check if the mouse is over an actual item
                var mousePosition = Mouse.GetPosition(list);
                var element = list.InputHitTest(mousePosition) as DependencyObject;
                
                // Walk up the visual tree to find a ListBoxItem
                var listBoxItem = FindVisualParent<ListBoxItem>(element);
                
                if (listBoxItem == null || list.SelectedItem == null)
                {
                    // Not over an item - cancel the context menu
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AutomationWindow] Error in ContextMenuOpening: {ex.Message}");
            }
        }

        private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent)
                    return parent;
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }

        private void OnAutomationListMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox lb)
            {
                _automationDragStart = e.GetPosition(lb);
            }
        }

        private void OnAutomationProcDrag(object sender, MouseEventArgs e)
        {
            // Only initiate drag if left button is pressed and we're not already dragging
            if (e.LeftButton != MouseButtonState.Pressed) return;
            if (_isAutomationDragging) return; // Prevent multiple simultaneous drags
            if (sender is not ListBox lb || lb.SelectedItem is not string item) return;
            
            // Check if mouse has moved far enough to start drag (minimum 6 pixels)
            Point currentPosition = e.GetPosition(lb);
            Vector diff = currentPosition - _automationDragStart;
            if (diff.Length < 6) return;
            
            // Set flag and store drag source info
            _isAutomationDragging = true;
            _automationDragItem = item;
            _automationDragSource = lb;
            _automationDragSourceIndex = lb.SelectedIndex;
            
            System.Diagnostics.Debug.WriteLine($"[AutoDrag] start item='{item}' src={_automationDragSource?.Name} idx={_automationDragSourceIndex}");
            
            // Create ghost tooltip
            CreateAutomationGhost(item, e.GetPosition(this));
            
            try
            {
                // Initiate drag operation
                DragDrop.DoDragDrop(lb, new DataObject("musm-proc", item), DragDropEffects.Move | DragDropEffects.Copy);
            }
            finally
            {
                // Always reset state when drag completes
                ClearAutomationDragState();
            }
        }

        private void OnAutomationProcDragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("musm-proc"))
            {
                ClearAutomationDropIndicator();
                return;
            }

            if (sender is not ListBox target)
            {
                ClearAutomationDropIndicator();
                return;
            }

            // Update ghost position
            if (_automationDragGhost != null && Content is Grid root)
            {
                var pos = e.GetPosition(root);
                Canvas.SetLeft(_automationDragGhost, pos.X + 8);
                Canvas.SetTop(_automationDragGhost, pos.Y + 8);
            }

            // Show drop indicator only for ordered lists (not library panes or Delete pane)
            bool isLibraryOrDelete = target.Name == "lstLibrary" || 
                                     target.Name == "lstCustomModules" || 
                                     target.Name == "lstDelete";
            
            if (!isLibraryOrDelete)
            {
                EnsureAutomationDropIndicator(target);
                var p = e.GetPosition(target);
                int idx = GetAutomationItemInsertIndex(target, p);
                PositionAutomationDropIndicator(target, idx);
            }
            else
            {
                ClearAutomationDropIndicator();
            }

            e.Handled = true;
        }

        private void OnAutomationProcDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("musm-proc"))
            {
                System.Diagnostics.Debug.WriteLine("[AutoDrop] no data");
                ClearAutomationDragState();
                return;
            }

            if (sender is not ListBox target)
            {
                System.Diagnostics.Debug.WriteLine("[AutoDrop] target not list");
                ClearAutomationDragState();
                return;
            }

            string item = (string)e.Data.GetData("musm-proc")!;
            
            bool fromLibrary = _automationDragSource?.Name == "lstLibrary" || _automationDragSource?.Name == "lstCustomModules";
            bool toDelete = target.Name == "lstDelete";
            bool toLibrary = target.Name == "lstLibrary" || target.Name == "lstCustomModules";
            bool sameList = _automationDragSource == target;
            
            System.Diagnostics.Debug.WriteLine($"[AutoDrop] item='{item}' fromLib={fromLibrary} toDelete={toDelete} toLib={toLibrary} sameList={sameList} target={target.Name}");

            // DEDICATED DELETE PANE: Remove from source and don't add to Delete pane
            if (toDelete)
            {
                if (!fromLibrary && !sameList && _automationDragSource != null)
                {
                    // Remove from source ordered list
                    var srcList = GetAutomationListForListBox(_automationDragSource);
                    if (srcList != null && _automationDragSourceIndex >= 0 && _automationDragSourceIndex < srcList.Count)
                    {
                        srcList.RemoveAt(_automationDragSourceIndex);
                        System.Diagnostics.Debug.WriteLine($"[AutoDrop] deleted '{item}' from {_automationDragSource.Name}");
                    }
                }
                // Don't add to delete pane - it's just a drop target, not a container
                ClearAutomationDragState();
                return;
            }

            // Library panes are copy-only (never add dropped items)
            if (toLibrary)
            {
                // Library panes don't accept drops (they are sources only)
                ClearAutomationDragState();
                return;
            }

            var targetList = GetAutomationListForListBox(target);
            
            if (targetList == null)
            {
                System.Diagnostics.Debug.WriteLine("[AutoDrop] target list null");
                ClearAutomationDragState();
                return;
            }

            // Calculate insert index based on drop position
            int insertIndex = GetAutomationItemInsertIndex(target, e.GetPosition(target));
            if (insertIndex < 0 || insertIndex > targetList.Count)
            {
                insertIndex = targetList.Count;
            }

            if (sameList && !fromLibrary)
            {
                // Move inside same list (reorder)
                if (_automationDragSourceIndex >= 0 && _automationDragSourceIndex < targetList.Count)
                {
                    if (insertIndex > _automationDragSourceIndex)
                    {
                        insertIndex--; // account for removal
                    }
                    targetList.RemoveAt(_automationDragSourceIndex);
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
                var srcList = GetAutomationListForListBox(_automationDragSource!);
                if (srcList != null && _automationDragSourceIndex >= 0 && _automationDragSourceIndex < srcList.Count)
                {
                    // remove the specific source instance at index (even if duplicates exist)
                    srcList.RemoveAt(_automationDragSourceIndex);
                }
                targetList.Insert(insertIndex, item);
            }

            ClearAutomationDragState();
        }

        private void OnAutomationListDragLeave(object sender, DragEventArgs e)
        {
            // Clear drop indicator when leaving list area
            ClearAutomationDropIndicator();
        }

        // Helper methods for drag-drop visual feedback
        private void CreateAutomationGhost(string text, Point pos)
        {
            RemoveAutomationGhost();
            
            _automationDragGhost = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(180, 60, 120, 255)),
                BorderBrush = Brushes.DodgerBlue,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(6, 2, 6, 2),
                Child = new TextBlock
                {
                    Text = text,
                    Foreground = Brushes.White
                },
                IsHitTestVisible = false
            };
            
            AddChildToAutomationOverlay(_automationDragGhost, pos);
        }

        private void AddChildToAutomationOverlay(FrameworkElement fe, Point pos)
        {
            if (Content is not Grid root) return;
            
            var canvas = root.Children.OfType<Canvas>().FirstOrDefault(c => c.Name == "AutomationGhostCanvas");
            if (canvas == null)
            {
                canvas = new Canvas
                {
                    Name = "AutomationGhostCanvas",
                    IsHitTestVisible = false
                };
                root.Children.Add(canvas);
            }
            
            canvas.Children.Add(fe);
            Canvas.SetLeft(fe, pos.X + 8);
            Canvas.SetTop(fe, pos.Y + 8);
        }

        private void RemoveAutomationGhost()
        {
            if (Content is not Grid root) return;
            if (_automationDragGhost == null) return;
            
            var canvas = root.Children.OfType<Canvas>().FirstOrDefault(c => c.Name == "AutomationGhostCanvas");
            canvas?.Children.Remove(_automationDragGhost);
            
            _automationDragGhost = null;
        }

        private void EnsureAutomationDropIndicator(ListBox host)
        {
            if (_automationDropIndicator != null) return;
            if (Content is not Grid root) return;
            
            var canvas = root.Children.OfType<Canvas>().FirstOrDefault(c => c.Name == "AutomationGhostCanvas");
            if (canvas == null)
            {
                canvas = new Canvas
                {
                    Name = "AutomationGhostCanvas",
                    IsHitTestVisible = false
                };
                root.Children.Add(canvas);
            }
            
            _automationDropIndicator = new Border
            {
                Height = 2,
                Background = Brushes.OrangeRed,
                IsHitTestVisible = false,
                Opacity = 0.85
            };
            
            canvas.Children.Add(_automationDropIndicator);
        }

        private void PositionAutomationDropIndicator(ListBox lb, int index)
        {
            if (_automationDropIndicator == null) return;
            if (Content is not Grid root) return;
            
            var canvas = root.Children.OfType<Canvas>().FirstOrDefault(c => c.Name == "AutomationGhostCanvas");
            if (canvas == null) return;
            
            double y = 0;
            
            if (index >= lb.Items.Count)
            {
                // Drop at end
                if (lb.Items.Count > 0)
                {
                    if (lb.ItemContainerGenerator.ContainerFromIndex(lb.Items.Count - 1) is FrameworkElement last)
                    {
                        var b = last.TransformToAncestor(lb).TransformBounds(new Rect(0, 0, last.ActualWidth, last.ActualHeight));
                        y = b.Bottom + 1;
                    }
                }
            }
            else
            {
                // Drop before item at index
                if (lb.ItemContainerGenerator.ContainerFromIndex(index) is FrameworkElement item)
                {
                    var b = item.TransformToAncestor(lb).TransformBounds(new Rect(0, 0, item.ActualWidth, item.ActualHeight));
                    y = b.Top - 1;
                }
            }
            
            var lbPos = lb.TransformToAncestor(root).Transform(new Point(0, 0));
            Canvas.SetLeft(_automationDropIndicator, lbPos.X + 4);
            Canvas.SetTop(_automationDropIndicator, lbPos.Y + y);
            _automationDropIndicator.Width = lb.ActualWidth - 8;
        }

        private void ClearAutomationDropIndicator()
        {
            if (_automationDropIndicator == null) return;
            if (Content is not Grid root) return;
            
            var canvas = root.Children.OfType<Canvas>().FirstOrDefault(c => c.Name == "AutomationGhostCanvas");
            canvas?.Children.Remove(_automationDropIndicator);
            
            _automationDropIndicator = null;
            System.Diagnostics.Debug.WriteLine("[AutoDrag] drop indicator cleared");
        }

        private void ClearAutomationDragState()
        {
            _isAutomationDragging = false;
            _automationDragItem = null;
            _automationDragSource = null;
            _automationDragSourceIndex = -1;
            
            RemoveAutomationGhost();
            ClearAutomationDropIndicator();
        }

        private int GetAutomationItemInsertIndex(ListBox lb, Point pos)
        {
            for (int i = 0; i < lb.Items.Count; i++)
            {
                if (lb.ItemContainerGenerator.ContainerFromIndex(i) is FrameworkElement fe)
                {
                    var r = fe.TransformToAncestor(lb).TransformBounds(new Rect(0, 0, fe.ActualWidth, fe.ActualHeight));
                    if (pos.Y < r.Top + r.Height / 2)
                    {
                        return i;
                    }
                }
            }
            
            return lb.Items.Count;
        }

        private System.Collections.ObjectModel.ObservableCollection<string>? GetAutomationListForListBox(ListBox lb)
        {
            if (_automationViewModel == null) return null;
            
            return lb.Name switch
            {
                "lstLibrary" => _builtinModules, // Use filtered built-in modules
                "lstCustomModules" => _customModules,
                "lstDelete" => null, // Delete pane has no backing list (it's just a drop target)
                "lstNewStudy" => _automationViewModel.NewStudyModules,
                "lstAddStudy" => _automationViewModel.AddStudyModules,
                "lstShortcutOpenNew" => _automationViewModel.ShortcutOpenNewModules,
                "lstShortcutOpenAdd" => _automationViewModel.ShortcutOpenAddModules,
                "lstShortcutOpenAfterOpen" => _automationViewModel.ShortcutOpenAfterOpenModules,
                "lstSendReport" => _automationViewModel.SendReportModules,
                "lstSendReportPreview" => _automationViewModel.SendReportPreviewModules,
                "lstShortcutSendReportPreview" => _automationViewModel.ShortcutSendReportPreviewModules,
                "lstShortcutSendReportReportified" => _automationViewModel.ShortcutSendReportReportifiedModules,
                "lstTest" => _automationViewModel.TestModules,
                _ => null
            };
        }

        private void OnSaveAutomation(object sender, RoutedEventArgs e)
        {
            if (_automationViewModel != null)
            {
                // Trigger the save command
                if (_automationViewModel.SaveAutomationCommand.CanExecute(null))
                {
                    _automationViewModel.SaveAutomationCommand.Execute(null);
                }
            }
        }

        private void OnCloseWindow(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wysg.Musm.Radium.ViewModels;
using Wysg.Musm.Radium.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Wysg.Musm.Radium.Views
{
    /// <summary>
    /// Partial class for Automation tab functionality in SpyWindow
    /// </summary>
    public partial class SpyWindow
    {
        private SettingsViewModel? _automationViewModel;
        private Point _automationDragStart;
        private bool _isAutomationDragging;
        private ObservableCollection<string> _customModules = new();

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
                    if (FindName("lstLibrary") is ListBox lib)
                    {
                        lib.ItemsSource = _automationViewModel.AvailableModules;
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
                    
                    // NEW: Initialize Custom Modules
                    if (FindName("lstCustomModules") is ListBox customList)
                    {
                        LoadCustomModules();
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
                System.Diagnostics.Debug.WriteLine($"[SpyWindow] Error loading custom modules: {ex.Message}");
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
                        
                        // Refresh the list
                        LoadCustomModules();
                        
                        // Also add to SettingsViewModel.AvailableModules
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
                System.Diagnostics.Debug.WriteLine($"[SpyWindow] Error creating module: {ex.Message}");
                MessageBox.Show($"Error creating module: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
            
            // Set flag to prevent re-entry
            _isAutomationDragging = true;
            
            try
            {
                // Initiate drag operation
                DragDrop.DoDragDrop(lb, new DataObject("musm-proc", item), DragDropEffects.Move | DragDropEffects.Copy);
            }
            finally
            {
                // Always reset flag when drag completes
                _isAutomationDragging = false;
            }
        }

        private void OnAutomationProcDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("musm-proc")) return;
            if (sender is not ListBox target) return;
            if (e.Data.GetData("musm-proc") is not string item) return;
            
            var targetList = GetAutomationListForListBox(target);
            if (targetList == null) return;
            
            // Simple append to end of list
            if (!targetList.Contains(item))
            {
                targetList.Add(item);
            }
        }

        private void OnAutomationListDragLeave(object sender, DragEventArgs e)
        {
            // Clear any drag indicators (not implemented in this simple version)
        }

        private System.Collections.ObjectModel.ObservableCollection<string>? GetAutomationListForListBox(ListBox lb)
        {
            if (_automationViewModel == null) return null;
            
            return lb.Name switch
            {
                "lstLibrary" => _automationViewModel.AvailableModules,
                "lstCustomModules" => _customModules,
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FlaUI.Core.AutomationElements;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.Views
{
    public partial class AutomationWindow
    {
        private static bool NeedsAsync(string? op)
        {
            return string.Equals(op, "GetTextOCR", StringComparison.OrdinalIgnoreCase)
                || string.Equals(op, "GetHTML", StringComparison.OrdinalIgnoreCase);
        }

        // Runtime element cache for storing elements from GetSelectedElement
        private readonly Dictionary<string, FlaUI.Core.AutomationElements.AutomationElement> _elementCache = new();

        private bool _handlingProcOpChange;
        private void OnProcOpChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_handlingProcOpChange) return;
            if (sender is System.Windows.Controls.ComboBox cb && cb.DataContext is ProcOpRow row)
            {
                try
                {
                    _handlingProcOpChange = true;
                    switch (row.Op)
                    {
                        case "GetText":
                        {
                            // GetText can target either a bookmark (Element) or a cached element reference (Var); only default to Element when unset
                            row.Arg1Enabled = true;
                            var getTextArgKind = ParseArgKind(row.Arg1.Type);
                            if (getTextArgKind == ArgKind.String || getTextArgKind == ArgKind.Number)
                            {
                                row.Arg1.Type = nameof(ArgKind.Element);
                            }
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = false; row.Arg2.Value = string.Empty;
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        }
                        case "GetTextOCR":
                        case "GetName":
                        case "Invoke":
                        case "MouseMoveToElement":
                        case "IsVisible":
                        case "SetFocus":
                            row.Arg1.Type = nameof(ArgKind.Element); row.Arg1Enabled = true;
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = false; row.Arg2.Value = string.Empty;
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "SetValue":
                            // SetValue: Arg1=Element (target control), Arg2=String or Var (value to set)
                            row.Arg1.Type = nameof(ArgKind.Element); row.Arg1Enabled = true;
                            row.Arg2Enabled = true; // Allow String or Var
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "SetValueWeb":
                            // SetValueWeb: Arg1=Element (target control), Arg2=String or Var (value to set)
                            row.Arg1.Type = nameof(ArgKind.Element); row.Arg1Enabled = true;
                            row.Arg2Enabled = true; // Allow String or Var
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "ClickElement":
                            // ClickElement accepts both Element (bookmark) and Var (from GetSelectedElement output)
                            // Don't reset Type if already set by user - only enable/disable args
                            row.Arg1Enabled = true;
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = false; row.Arg2.Value = string.Empty;
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "ClickElementAndStay":
                            // ClickElementAndStay: clicks element but leaves cursor at element (no restore)
                            // Accepts both Element (bookmark) and Var (from GetSelectedElement output)
                            row.Arg1Enabled = true;
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = false; row.Arg2.Value = string.Empty;
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "SetClipboard":
                            // FIX: SetClipboard accepts both String and Var types - don't force to String
                            // Only enable Arg1, keep user's Type selection
                            row.Arg1Enabled = true;
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = false; row.Arg2.Value = string.Empty;
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "TrimString":
                            // NEW: TrimString trims Arg2 away from Arg1 (both accept String or Var)
                            row.Arg1Enabled = true; // Source string (String or Var)
                            row.Arg2Enabled = true; // String to trim away (String or Var)
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "Delay":
                            // Delay: pauses execution for specified milliseconds
                            row.Arg1.Type = nameof(ArgKind.Number); row.Arg1Enabled = true; if (string.IsNullOrWhiteSpace(row.Arg1.Value)) row.Arg1.Value = "100";
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = false; row.Arg2.Value = string.Empty;
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "SimulateTab":
                        case "SimulatePaste":
                        case "SimulateSelectAll":
                        case "SimulateDelete":
                            // These keyboard simulations do not take arguments
                            row.Arg1.Type = nameof(ArgKind.String); row.Arg1Enabled = false; row.Arg1.Value = string.Empty;
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = false; row.Arg2.Value = string.Empty;
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "Split":
                            row.Arg1.Type = nameof(ArgKind.Var); row.Arg1Enabled = true;
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = true; if (string.IsNullOrWhiteSpace(row.Arg2.Value)) row.Arg2.Value = ",";
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = true; if (string.IsNullOrWhiteSpace(row.Arg3.Value)) row.Arg3.Value = "0";
                            break;
                        case "IsMatch":
                        case "IsAlmostMatch":
                            // IsMatch/IsAlmostMatch: Arg1=input (Var), Arg2=comparison value (String or Var)
                            // Don't force Arg2 type - let user choose between String literal and Var
                            row.Arg1Enabled = true; // Typically Var but could be String
                            row.Arg2Enabled = true; // Allow String or Var
                            row.Arg3.Type = nameof(ArgKind.String); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "GetLongerText":
                            // GetLongerText: Arg1=text1 (String or Var), Arg2=text2 (String or Var), returns longer text
                            row.Arg1Enabled = true; // Allow String or Var
                            row.Arg2Enabled = true; // Allow String or Var
                            row.Arg3.Type = nameof(ArgKind.String); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "And":
                            // And: Arg1=boolean var, Arg2=boolean var, returns "true" if both true
                            row.Arg1.Type = nameof(ArgKind.Var); row.Arg1Enabled = true;
                            row.Arg2.Type = nameof(ArgKind.Var); row.Arg2Enabled = true;
                            row.Arg3.Type = nameof(ArgKind.String); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "Not":
                            // Not: Arg1=boolean var, returns "true" if Arg1 is false, "false" if Arg1 is true
                            row.Arg1.Type = nameof(ArgKind.Var); row.Arg1Enabled = true;
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = false; row.Arg2.Value = string.Empty;
                            row.Arg3.Type = nameof(ArgKind.String); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "Echo":
                            // Echo: Pass-through operation, accepts String or Var, returns value unchanged
                            // Useful for capturing built-in properties into procedure variables
                            row.Arg1Enabled = true; // Allow String or Var
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = false; row.Arg2.Value = string.Empty;
                            row.Arg3.Type = nameof(ArgKind.String); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "IsBlank":
                            // IsBlank: Arg1=string to check (String or Var), returns "true" if blank/whitespace, "false" otherwise
                            row.Arg1Enabled = true; // Allow String or Var
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = false; row.Arg2.Value = string.Empty;
                            row.Arg3.Type = nameof(ArgKind.String); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "Replace":
                        case "Merge":
                            // Replace/Merge: Arg1=input (Var), Arg2=search/input2 (String or Var), Arg3=replacement/separator (String or Var)
                            // Don't force types - let user choose between String and Var
                            row.Arg1Enabled = true;
                            row.Arg2Enabled = true;
                            row.Arg3Enabled = true;
                            break;
                        case "GetHTML":
                            row.Arg1.Type = nameof(ArgKind.Var); row.Arg1Enabled = true;
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = false; row.Arg2.Value = string.Empty;
                            row.Arg3.Type = nameof(ArgKind.String); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "MouseClick":
                            row.Arg1.Type = nameof(ArgKind.Number); row.Arg1Enabled = true; if (string.IsNullOrWhiteSpace(row.Arg1.Value)) row.Arg1.Value = "0";
                            row.Arg2.Type = nameof(ArgKind.Number); row.Arg2Enabled = true; if (string.IsNullOrWhiteSpace(row.Arg2.Value)) row.Arg2.Value = "0";
                            row.Arg3.Type = nameof(ArgKind.String); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "Trim":
                             row.Arg1.Type = nameof(ArgKind.Var); row.Arg1Enabled = true;
                             row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = false; row.Arg2.Value = string.Empty;
                             row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                             break;
                        case "GetValueFromSelection":
                            row.Arg1.Type = nameof(ArgKind.Element); row.Arg1Enabled = true;
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = true; if (string.IsNullOrWhiteSpace(row.Arg2.Value)) row.Arg2.Value = "ID";
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "GetDateFromSelectionWait":
                            row.Arg1.Type = nameof(ArgKind.Element); row.Arg1Enabled = true;
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = true; if (string.IsNullOrWhiteSpace(row.Arg2.Value)) row.Arg2.Value = "ID";
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "GetSelectedElement":
                            row.Arg1.Type = nameof(ArgKind.Element); row.Arg1Enabled = true;
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = false; row.Arg2.Value = string.Empty;
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        default:
                            row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                    }
                }
                finally { _handlingProcOpChange = false; }
            }
        }

        private void OnAddProcRow(object sender, RoutedEventArgs e)
        {
            if (FindName("gridProcSteps") is System.Windows.Controls.DataGrid procGrid) returnToList(procGrid, list => list.Add(new ProcOpRow()));
        }
        
        private void OnDuplicateOperations(object sender, RoutedEventArgs e)
        {
            var cmbSource = (System.Windows.Controls.ComboBox?)FindName("cmbProcMethod");
            var procGrid = (System.Windows.Controls.DataGrid?)FindName("gridProcSteps");
            
            // Get current source procedure (now just a string)
            string? sourceTag = cmbSource?.SelectedItem as string;
            
            if (string.IsNullOrWhiteSpace(sourceTag))
            {
                txtStatus.Text = "Select a source procedure first";
                return;
            }
            
            if (procGrid == null)
            {
                txtStatus.Text = "No operations grid found";
                return;
            }
            
            // Commit any pending edits
            try { procGrid.CommitEdit(DataGridEditingUnit.Cell, true); procGrid.CommitEdit(DataGridEditingUnit.Row, true); } catch { }
            
            // Get current operations
            var sourceOps = procGrid.Items.OfType<ProcOpRow>().Where(s => !string.IsNullOrWhiteSpace(s.Op)).ToList();
            
            if (sourceOps.Count == 0)
            {
                txtStatus.Text = "No operations to duplicate (source procedure is empty)";
                return;
            }
            
            // Show selection dialog for target procedure
            var targetTag = ShowProcedureSelectionDialog(sourceTag);
            
            if (string.IsNullOrWhiteSpace(targetTag))
            {
                txtStatus.Text = "Duplication cancelled";
                return;
            }
            
            try
            {
                // Load target procedure to check if it's empty
                var targetOps = LoadProcedureForMethod(targetTag);
                
                if (targetOps.Count > 0)
                {
                    var result = System.Windows.MessageBox.Show(
                        $"Target procedure '{targetTag}' already has {targetOps.Count} operation(s).\n\nDo you want to replace them?",
                        "Confirm Replace",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Warning);
                    
                    if (result != System.Windows.MessageBoxResult.Yes)
                    {
                        txtStatus.Text = "Duplication cancelled";
                        return;
                    }
                }
                
                // Deep copy operations (create new instances to avoid reference issues)
                var duplicatedOps = sourceOps.Select(op => new ProcOpRow
                {
                    Op = op.Op,
                    Arg1 = new ProcArg { Type = op.Arg1.Type, Value = op.Arg1.Value },
                    Arg2 = new ProcArg { Type = op.Arg2.Type, Value = op.Arg2.Value },
                    Arg3 = new ProcArg { Type = op.Arg3.Type, Value = op.Arg3.Value },
                    Arg1Enabled = op.Arg1Enabled,
                    Arg2Enabled = op.Arg2Enabled,
                    Arg3Enabled = op.Arg3Enabled,
                    OutputVar = null, // Clear output variables for new procedure
                    OutputPreview = null
                }).ToList();
                
                // Save duplicated operations to target procedure
                SaveProcedureForMethod(targetTag, duplicatedOps);
                
                txtStatus.Text = $"Successfully duplicated {duplicatedOps.Count} operation(s) from '{sourceTag}' to '{targetTag}'";
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"Error duplicating operations: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[AutomationWindow] Duplicate error: {ex}");
            }
        }
        
        private string? ShowProcedureSelectionDialog(string sourceProcedure)
        {
            // Get all available procedures from ProcedureNames (already loaded from ui-procedures.json)
            var availableProcedures = ProcedureNames
                .Where(name => !string.Equals(name, sourceProcedure, StringComparison.OrdinalIgnoreCase))
                .OrderBy(name => name)
                .ToList();
            
            if (availableProcedures.Count == 0)
            {
                System.Windows.MessageBox.Show(
                    "No other procedures available.\n\nCreate a new procedure first using 'Add procedure' button.",
                    "No Target Procedures",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                return null;
            }
            
            // Create selection dialog
            var dialog = new System.Windows.Window
            {
                Title = "Select Target Procedure",
                Width = 500,
                Height = 400,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30)),
                ResizeMode = System.Windows.ResizeMode.NoResize
            };
            
            var grid = new System.Windows.Controls.Grid { Margin = new System.Windows.Thickness(20) };
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
            
            // Header
            var header = new System.Windows.Controls.TextBlock
            {
                Text = $"Select target procedure to copy operations from '{sourceProcedure}':",
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                TextWrapping = System.Windows.TextWrapping.Wrap,
                Margin = new System.Windows.Thickness(0, 0, 0, 15)
            };
            System.Windows.Controls.Grid.SetRow(header, 0);
            grid.Children.Add(header);
            
            // ListBox with procedure names (strings)
            var listBox = new System.Windows.Controls.ListBox
            {
                ItemsSource = availableProcedures,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 48)),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60)),
                Margin = new System.Windows.Thickness(0, 0, 0, 15)
            };
            System.Windows.Controls.Grid.SetRow(listBox, 1);
            grid.Children.Add(listBox);
            
            // Buttons
            var buttonPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };
            System.Windows.Controls.Grid.SetRow(buttonPanel, 2);
            
            var btnOk = new System.Windows.Controls.Button
            {
                Content = "OK",
                Width = 80,
                Margin = new System.Windows.Thickness(0, 0, 10, 0),
                Padding = new System.Windows.Thickness(10, 5, 10, 5),
                IsDefault = true
            };
            btnOk.Click += (s, e) =>
            {
                if (listBox.SelectedItem != null)
                {
                    dialog.DialogResult = true;
                    dialog.Close();
                }
            };
            buttonPanel.Children.Add(btnOk);
            
            var btnCancel = new System.Windows.Controls.Button
            {
                Content = "Cancel",
                Width = 80,
                Padding = new System.Windows.Thickness(10, 5, 10, 5),
                IsCancel = true
            };
            btnCancel.Click += (s, e) =>
            {
                dialog.DialogResult = false;
                dialog.Close();
            };
            buttonPanel.Children.Add(btnCancel);
            
            grid.Children.Add(buttonPanel);
            dialog.Content = grid;
            
            // Handle double-click
            listBox.MouseDoubleClick += (s, e) =>
            {
                if (listBox.SelectedItem != null)
                {
                    dialog.DialogResult = true;
                    dialog.Close();
                }
            };
            
            var result = dialog.ShowDialog();
            // Return the selected procedure name (string)
            if (result == true && listBox.SelectedItem is string selectedName)
            {
                return selectedName;
            }
            return null;
        }
        
        private void OnRemoveProcRow(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button b && b.Tag is ProcOpRow row && FindName("gridProcSteps") is System.Windows.Controls.DataGrid procGrid)
            {
                returnToList(procGrid, list => list.Remove(row));
            }
        }
        private async void OnSetProcRow(object sender, RoutedEventArgs e)
        {
            if (FindName("gridProcSteps") is not System.Windows.Controls.DataGrid procGrid) return;
            try { procGrid.CommitEdit(DataGridEditingUnit.Cell, true); procGrid.CommitEdit(DataGridEditingUnit.Row, true); } catch { }
            var list = procGrid.Items.OfType<ProcOpRow>().ToList();
            if (sender is System.Windows.Controls.Button b && b.Tag is ProcOpRow row)
            {
                var index = list.IndexOf(row); if (index < 0) return;
                var vars = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < index; i++) vars[$"var{i + 1}"] = list[i].OutputVar != null ? list[i].OutputPreview : null;
                var varName = $"var{index + 1}";
                var result = NeedsAsync(row.Op)
                    ? await ExecuteSingleAsync(row, vars)
                    : ExecuteSingle(row, vars);
                row.OutputVar = varName;
                row.OutputPreview = result.preview;
                if (!ProcedureVars.Contains(varName)) ProcedureVars.Add(varName);
                procGrid.ItemsSource = null; procGrid.ItemsSource = list;
            }
        }

        private void returnToList(System.Windows.Controls.DataGrid grid, Action<System.Collections.Generic.List<ProcOpRow>> mutator)
        {
            var list = grid.Items.OfType<ProcOpRow>().ToList();
            mutator(list);
            grid.ItemsSource = null; grid.ItemsSource = list;
            UpdateProcedureVarsFrom(list);
        }

        private static string UnescapeUserText(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            try { return Regex.Unescape(s); } catch { return s; }
        }

        private (string preview, string? value) ExecuteSingle(ProcOpRow row, Dictionary<string, string?> vars)
        {
            // Delegate to shared OperationExecutor service
            return OperationExecutor.ExecuteOperation(
                row.Op,
                resolveArg1Element: () => ResolveElement(row.Arg1, vars),
                resolveArg1String: () => ResolveString(row.Arg1, vars),
                resolveArg2String: () => ResolveString(row.Arg2, vars),
                resolveArg3String: () => ResolveString(row.Arg3, vars),
                elementCache: _elementCache
            );
        }

        private async Task<(string preview, string? value)> ExecuteSingleAsync(ProcOpRow row, Dictionary<string, string?> vars)
        {
            // Delegate to shared OperationExecutor service for async operations
            return await OperationExecutor.ExecuteOperationAsync(
                row.Op,
                resolveArg1Element: () => ResolveElement(row.Arg1, vars),
                resolveArg1String: () => ResolveString(row.Arg1, vars),
                resolveArg2String: () => ResolveString(row.Arg2, vars),
                resolveArg3String: () => ResolveString(row.Arg3, vars),
                elementCache: _elementCache
            );
        }

        private void UpdateProcedureVarsFrom(System.Collections.Generic.List<ProcOpRow> rows)
        {
            ProcedureVars.Clear();
            
            // Add potential varN for each row (whether executed or not)
            for (int i = 0; i < rows.Count; i++)
            {
                var varName = $"var{i + 1}";
                if (!ProcedureVars.Contains(varName))
                {
                    ProcedureVars.Add(varName);
                }
            }
            
            // Also add any explicitly set output vars
            foreach (var r in rows)
            {
                if (!string.IsNullOrWhiteSpace(r.OutputVar) && !ProcedureVars.Contains(r.OutputVar))
                {
                    ProcedureVars.Add(r.OutputVar);
                }
            }
            
            // Add built-in properties from CustomModuleProperties
            foreach (var prop in Wysg.Musm.Radium.Models.CustomModuleProperties.AllReadableProperties)
            {
                if (!ProcedureVars.Contains(prop))
                {
                    ProcedureVars.Add(prop);
                }
            }
        }

        private FlaUI.Core.AutomationElements.AutomationElement? ResolveElement(ProcArg arg, Dictionary<string, string?> vars)
        {
            var type = ParseArgKind(arg.Type); 
            
            // Handle Element type (bookmark-based resolution)
            if (type == ArgKind.Element)
            {
                var tag = arg.Value ?? string.Empty;
                
                // Simplified: All bookmarks resolved by name (no enum parsing)
                var bookmarkByTag = UiBookmarks.Resolve(tag);
                if (bookmarkByTag.element != null)
                {
                    return bookmarkByTag.element;
                }
                
                return null;
            }
            
            // Handle Var type (variable containing cached element reference)
            if (type == ArgKind.Var)
            {
                var varName = arg.Value ?? string.Empty;
                Debug.WriteLine($"[AutomationWindow][ResolveElement][Var] Resolving variable '{varName}'");
                
                // First resolve the variable value from vars dictionary
                if (!vars.TryGetValue(varName, out var varValue) || string.IsNullOrWhiteSpace(varValue))
                {
                    Debug.WriteLine($"[AutomationWindow][ResolveElement][Var] Variable '{varName}' not found in vars dictionary or is empty");
                    return null;
                }
                
                Debug.WriteLine($"[AutomationWindow][ResolveElement][Var] Variable '{varName}' resolved to: '{varValue}'");
                
                // Check if this variable contains a cached element reference
                if (_elementCache.TryGetValue(varValue, out var cachedElement))
                {
                    Debug.WriteLine($"[AutomationWindow][ResolveElement][Var] Found cached element for key '{varValue}'");
                    // Validate element is still alive
                    try
                    {
                        _ = cachedElement.Name; // Test if element is still accessible
                        Debug.WriteLine($"[AutomationWindow][ResolveElement][Var] Cached element is still alive");
                        return cachedElement;
                    }
                    catch
                    {
                        // Element is stale, remove from cache
                        Debug.WriteLine($"[AutomationWindow][ResolveElement][Var] Cached element is stale, removing from cache");
                        _elementCache.Remove(varValue);
                        return null;
                    }
                }
                else
                {
                    Debug.WriteLine($"[AutomationWindow][ResolveElement][Var] No cached element found for key '{varValue}'");
                }
            }
            
            return null;
        }

        private static string? ResolveString(ProcArg arg, Dictionary<string, string?> vars)
        {
            var type = ParseArgKind(arg.Type);
            
            if (type == ArgKind.Var)
            {
                var varName = arg.Value ?? string.Empty;
                
                // Check if this is a built-in property name first
                if (Wysg.Musm.Radium.Models.CustomModuleProperties.IsBuiltInProperty(varName))
                {
                    Debug.WriteLine($"[AutomationWindow][ResolveString] Resolving built-in property: '{varName}'");
                    return GetBuiltInPropertyValue(varName);
                }
                
                // For ArgKind.Var, look up value in vars dictionary
                return (arg.Value != null && vars.TryGetValue(arg.Value, out var v)) ? v : null;
            }
            
            return type switch
            {
                ArgKind.String => arg.Value,
                ArgKind.Number => arg.Value,
                _ => null
            };
        }
        
        /// <summary>
        /// Get a built-in property value from MainViewModel.
        /// </summary>
        private static string? GetBuiltInPropertyValue(string propertyName)
        {
            try
            {
                var mainWindow = System.Windows.Application.Current?.MainWindow;
                if (mainWindow?.DataContext is Wysg.Musm.Radium.ViewModels.MainViewModel mainVM)
                {
                    var result = mainVM.GetPropertyValue(propertyName);
                    Debug.WriteLine($"[AutomationWindow][GetBuiltInPropertyValue] '{propertyName}' = '{result}'");
                    return result;
                }
                else
                {
                    Debug.WriteLine($"[AutomationWindow][GetBuiltInPropertyValue] MainViewModel not found");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AutomationWindow][GetBuiltInPropertyValue] Error: {ex.Message}");
            }
            return null;
        }

        private static ArgKind ParseArgKind(string? s)
        {
            if (Enum.TryParse<ArgKind>(s, true, out var k)) return k;
            return s?.Equals("Var", StringComparison.OrdinalIgnoreCase) == true ? ArgKind.Var : ArgKind.String;
        }

        private void OnOpComboPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.ComboBox cb && !cb.IsDropDownOpen)
                {
                    e.Handled = true;
                    var cell = FindParent<System.Windows.Controls.DataGridCell>(cb);
                    var grid = FindParent<System.Windows.Controls.DataGrid>(cell) ?? (System.Windows.Controls.DataGrid?)FindName("gridProcSteps");
                    try { grid?.BeginEdit(); } catch { }
                    cb.Focus(); cb.IsDropDownOpen = true;
                }
            }
            catch { }
        }

        private void OnOpComboPreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.ComboBox cb)
                {
                    if (e.Key == Key.F4 || (e.Key == Key.Down && (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt) || e.Key == Key.Space)
                    {
                        var cell = FindParent<System.Windows.Controls.DataGridCell>(cb);
                        var grid = FindParent<System.Windows.Controls.DataGrid>(cell) ?? (System.Windows.Controls.DataGrid?)FindName("gridProcSteps");
                        try { grid?.BeginEdit(); } catch { }
                        cb.Focus(); cb.IsDropDownOpen = !cb.IsDropDownOpen; e.Handled = true;
                    }
                }
            }
            catch { }
        }

        private static string GetProcPath()
        {
            try
            {
                if (UiBookmarks.GetStorePathOverride != null)
                {
                    var bookmarkPath = UiBookmarks.GetStorePathOverride.Invoke();
                    if (!string.IsNullOrWhiteSpace(bookmarkPath))
                    {
                        var baseDir = System.IO.Path.GetDirectoryName(bookmarkPath);
                        if (!string.IsNullOrEmpty(baseDir))
                        {
                            System.IO.Directory.CreateDirectory(baseDir);
                            return System.IO.Path.Combine(baseDir, "ui-procedures.json");
                        }
                    }
                }

                if (System.Windows.Application.Current is Wysg.Musm.Radium.App app)
                {
                    try
                    {
                        var tenant = (Wysg.Musm.Radium.Services.ITenantContext?)app.Services.GetService(typeof(Wysg.Musm.Radium.Services.ITenantContext));
                        var pacsKey = string.IsNullOrWhiteSpace(tenant?.CurrentPacsKey) ? "default_pacs" : tenant!.CurrentPacsKey;
                        var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Wysg.Musm", "Radium", "Pacs", SanitizeFileName(pacsKey));
                        System.IO.Directory.CreateDirectory(dir);
                        return System.IO.Path.Combine(dir, "ui-procedures.json");
                    }
                    catch { }
                }
            }
            catch { }

            var legacyDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Wysg.Musm", "Radium");
            System.IO.Directory.CreateDirectory(legacyDir);
            return System.IO.Path.Combine(legacyDir, "ui-procedures.json");
        }
        private static ProcStore LoadProcStore()
        {
            try
            {
                var p = GetProcPath();
                if (!System.IO.File.Exists(p)) return new ProcStore();
                return System.Text.Json.JsonSerializer.Deserialize<ProcStore>(System.IO.File.ReadAllText(p), new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web) { WriteIndented = true }) ?? new ProcStore();
            }
            catch { return new ProcStore(); }
        }
        private static void SaveProcStore(ProcStore s)
        {
            try
            {
                var p = GetProcPath();
                System.IO.File.WriteAllText(p, System.Text.Json.JsonSerializer.Serialize(s, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web) { WriteIndented = true }));
            }
            catch { }
        }
        private static void SaveProcedureForMethod(string methodTag, System.Collections.Generic.List<ProcOpRow> steps)
        { var s = LoadProcStore(); s.Methods[methodTag] = steps; SaveProcStore(s); }
        private static System.Collections.Generic.List<ProcOpRow> LoadProcedureForMethod(string methodTag)
        { var s = LoadProcStore(); return s.Methods.TryGetValue(methodTag, out var steps) ? steps : new System.Collections.Generic.List<ProcOpRow>(); }

        private void OnProcMethodChanged(object? sender, SelectionChangedEventArgs e)
        {
            var cmb = (System.Windows.Controls.ComboBox?)FindName("cmbProcMethod");
            
            // Get selected procedure name (now just a string)
            string? tag = cmb?.SelectedItem as string;
            
            if (FindName("gridProcSteps") is not System.Windows.Controls.DataGrid procGrid || string.IsNullOrWhiteSpace(tag)) return;
            var steps = LoadProcedureForMethod(tag).ToList();
            procGrid.ItemsSource = steps;
            UpdateProcedureVarsFrom(steps);
        }
        
        private void OnSaveProcedure(object sender, RoutedEventArgs e)
        {
            var cmb = (System.Windows.Controls.ComboBox?)FindName("cmbProcMethod");
            var procGrid = (System.Windows.Controls.DataGrid?)FindName("gridProcSteps");
            
            // Get selected procedure name (now just a string)
            string? tag = cmb?.SelectedItem as string;
            
            if (string.IsNullOrWhiteSpace(tag)) { txtStatus.Text = "Select custom procedure"; return; }
            if (procGrid == null) { txtStatus.Text = "No steps"; return; }
            try { procGrid.CommitEdit(DataGridEditingUnit.Cell, true); procGrid.CommitEdit(DataGridEditingUnit.Row, true); } catch { }
            var steps = procGrid.Items.OfType<ProcOpRow>().Where(s => !string.IsNullOrWhiteSpace(s.Op)).ToList();
            SaveProcedureForMethod(tag, steps);
            txtStatus.Text = $"Saved procedure for {tag}";
        }
        
        private async void OnRunProcedure(object sender, RoutedEventArgs e)
        {
            var cmb = (System.Windows.Controls.ComboBox?)FindName("cmbProcMethod");
            var procGrid = (System.Windows.Controls.DataGrid?)FindName("gridProcSteps");
            
            // Get selected procedure name (now just a string)
            string? tag = cmb?.SelectedItem as string;
            
            if (string.IsNullOrWhiteSpace(tag)) { txtStatus.Text = "Select custom procedure"; return; }
            if (procGrid == null) { txtStatus.Text = "No steps"; return; }
            try { procGrid.CommitEdit(DataGridEditingUnit.Cell, true); procGrid.CommitEdit(DataGridEditingUnit.Row, true); } catch { }
            var steps = procGrid.Items.OfType<ProcOpRow>().Where(s => !string.IsNullOrWhiteSpace(s.Op)).ToList();
            var (result, annotated) = await RunProcedureAsync(steps);
            procGrid.ItemsSource = null; procGrid.ItemsSource = annotated; UpdateProcedureVarsFrom(annotated);
            txtStatus.Text = result ?? "(null)";
        }
        private async Task<(string? result, System.Collections.Generic.List<ProcOpRow> annotated)> RunProcedureAsync(System.Collections.Generic.List<ProcOpRow> steps)
        {
            // Clear element cache before running procedure
            _elementCache.Clear();
            
            var vars = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            string? last = null; var annotated = new System.Collections.Generic.List<ProcOpRow>();
            for (int i = 0; i < steps.Count; i++)
            {
                var row = steps[i]; string varName = $"var{i + 1}"; row.OutputVar = varName;
                var res = NeedsAsync(row.Op)
                    ? await ExecuteSingleAsync(row, vars)
                    : ExecuteSingle(row, vars);
                vars[varName] = res.value; if (res.value != null) last = res.value; row.OutputPreview = res.preview; annotated.Add(row);
            }
            return (last, annotated);
        }
    }
}

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Wysg.Musm.Radium.Models;
using Wysg.Musm.Radium.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Wysg.Musm.Radium.Views
{
    public partial class CreateModuleWindow : Window
    {
        public CustomModule? Result { get; private set; }
        public string? CreatedLabelName { get; private set; }
        private bool _isInitializing = true;
        
        public CreateModuleWindow()
        {
            InitializeComponent();
            LoadProperties();
            LoadProcedures();
            LoadLabels();
            
            // Set default selection
            cboModuleType.SelectedIndex = 0;
            
            // Disable auto-naming during initialization
            _isInitializing = false;
        }
        
        private void LoadProperties()
        {
            foreach (var prop in CustomModuleProperties.AllProperties)
            {
                cboProperty.Items.Add(prop);
            }
            
            if (cboProperty.Items.Count > 0)
                cboProperty.SelectedIndex = 0;
        }
        
        private void LoadProcedures()
        {
            try
            {
                // Load custom procedures using the same pattern as AutomationWindow
                var procPath = GetProcPath();
                if (System.IO.File.Exists(procPath))
                {
                    var json = System.IO.File.ReadAllText(procPath);
                    var store = System.Text.Json.JsonSerializer.Deserialize<ProcStore>(json,
                        new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
                    
                    if (store?.Methods != null)
                    {
                        var procedureNames = store.Methods.Keys.OrderBy(k => k).ToList();
                        foreach (var name in procedureNames)
                        {
                            cboProcedure.Items.Add(name);
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"[CreateModule] Loaded {procedureNames.Count} custom procedures");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[CreateModule] No custom procedures file found");
                }
                
                if (cboProcedure.Items.Count > 0)
                    cboProcedure.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CreateModule] Error loading procedures: {ex.Message}");
            }
        }
        
        private void LoadLabels()
        {
            try
            {
                cboGotoLabel.Items.Clear();
                var store = CustomModuleStore.Load();
                var labels = store.Labels
                    .Select(CustomModuleStore.ToLabelDisplay)
                    .Where(label => !string.IsNullOrWhiteSpace(label))
                    .OrderBy(label => label, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                
                foreach (var label in labels)
                {
                    cboGotoLabel.Items.Add(label);
                }
                
                if (cboGotoLabel.Items.Count > 0)
                {
                    cboGotoLabel.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CreateModule] Error loading labels: {ex.Message}");
            }
        }
        
        // Helper to get proc path (same as AutomationWindow pattern)
        private static string GetProcPath()
        {
            try
            {
                if (Application.Current is App app)
                {
                    try
                    {
                        var tenant = app.Services.GetService(typeof(Services.ITenantContext)) as Services.ITenantContext;
                        var pacsKey = string.IsNullOrWhiteSpace(tenant?.CurrentPacsKey) ? "default_pacs" : tenant!.CurrentPacsKey;
                        var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                            "Wysg.Musm", "Radium", "Pacs", SanitizeFileName(pacsKey));
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
        
        private static string SanitizeFileName(string name)
        {
            var invalid = System.IO.Path.GetInvalidFileNameChars();
            return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }
        
        // Simple ProcStore class for deserialization
        private class ProcStore
        {
            public System.Collections.Generic.Dictionary<string, object> Methods { get; set; } = new();
        }
        
        private void OnModuleTypeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboModuleType.SelectedValue != null)
            {
                var type = cboModuleType.SelectedValue.ToString();
                var requiresProcedure = type != "Label" && type != "Goto";
                pnlProperty.Visibility = type == "Set" ? Visibility.Visible : Visibility.Collapsed;
                pnlProcedure.Visibility = requiresProcedure ? Visibility.Visible : Visibility.Collapsed;
                pnlLabel.Visibility = type == "Label" ? Visibility.Visible : Visibility.Collapsed;
                pnlGoto.Visibility = type == "Goto" ? Visibility.Visible : Visibility.Collapsed;
                
                // Auto-generate module name
                if (!_isInitializing)
                {
                    UpdateModuleName();
                }
            }
        }
        
        private void OnSave(object sender, RoutedEventArgs e)
        {
            // Validate inputs - module name is auto-generated
            var moduleName = txtGeneratedName.Text?.Trim();
            
            // Check if name was properly generated
            if (string.IsNullOrWhiteSpace(moduleName) || 
                moduleName.StartsWith("(") || 
                moduleName.Contains("[Property]"))
            {
                MessageBox.Show("Please select all required options to generate a module name.", "Validation", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (cboModuleType.SelectedValue == null)
            {
                MessageBox.Show("Please select a module type.", "Validation", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            var typeStr = cboModuleType.SelectedValue.ToString();
            if (typeStr == "Label")
            {
                var normalized = CustomModuleStore.NormalizeLabelName(txtLabelName.Text ?? string.Empty);
                if (string.IsNullOrWhiteSpace(normalized))
                {
                    MessageBox.Show("Please enter a label name.", "Validation", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                CreatedLabelName = normalized;
                Result = null;
                DialogResult = true;
                Close();
                return;
            }
            
            var moduleType = typeStr switch
            {
                "Run" => CustomModuleType.Run,
                "Set" => CustomModuleType.Set,
                "Abort if" => CustomModuleType.AbortIf,
                "If" => CustomModuleType.If,
                "If not" => CustomModuleType.IfNot,
                "Goto" => CustomModuleType.Goto,
                "If message is Yes" => CustomModuleType.IfMessageYes,
                _ => CustomModuleType.Run
            };
            
            string? procedureName = string.Empty;
            if (moduleType != CustomModuleType.Goto)
            {
                if (cboProcedure.SelectedItem is not string selectedProcedure || 
                    string.IsNullOrWhiteSpace(selectedProcedure))
                {
                    MessageBox.Show("Please select a custom procedure.", "Validation", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                procedureName = selectedProcedure;
            }
            
            string? propertyName = null;
            if (moduleType == CustomModuleType.Set)
            {
                propertyName = cboProperty.SelectedItem as string;
                if (string.IsNullOrWhiteSpace(propertyName))
                {
                    MessageBox.Show("Please select a property.", "Validation", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            
            string? targetLabel = null;
            if (moduleType == CustomModuleType.Goto)
            {
                if (cboGotoLabel.SelectedItem is not string selectedLabel || 
                    !CustomModuleStore.TryParseLabelDisplay(selectedLabel, out var parsedLabel))
                {
                    MessageBox.Show("Please select a target label for the goto module.", "Validation", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                targetLabel = parsedLabel;
            }
            
            // Create the custom module with auto-generated name
            Result = new CustomModule
            {
                Name = moduleName,
                Type = moduleType,
                ProcedureName = procedureName ?? string.Empty,
                PropertyName = propertyName,
                TargetLabelName = targetLabel
            };
            
            DialogResult = true;
            Close();
        }
        
        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        
        private void OnPropertyChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitializing)
            {
                UpdateModuleName();
            }
        }
        
        private void OnProcedureChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitializing)
            {
                UpdateModuleName();
            }
        }
        
        private void OnLabelNameChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isInitializing)
            {
                UpdateModuleName();
            }
        }
        
        private void OnGotoLabelChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitializing)
            {
                UpdateModuleName();
            }
        }
        
        private void UpdateModuleName()
        {
            try
            {
                if (cboModuleType.SelectedValue == null)
                {
                    txtGeneratedName.Text = "(Select options above to generate name)";
                    return;
                }
                
                var type = cboModuleType.SelectedValue.ToString();
                var procedure = cboProcedure.SelectedItem as string;
                
                string moduleName;
                
                if (type == "Set")
                {
                    var property = cboProperty.SelectedItem as string;
                    if (string.IsNullOrWhiteSpace(property))
                    {
                        moduleName = "Set [Property] to " + (procedure ?? "[Procedure]");
                    }
                    else if (string.IsNullOrWhiteSpace(procedure))
                    {
                        moduleName = $"Set {property} to [Procedure]";
                    }
                    else
                    {
                        moduleName = $"Set {property} to {procedure}";
                    }
                }
                else if (type == "Abort if")
                {
                    moduleName = string.IsNullOrWhiteSpace(procedure) ? "Abort if [Procedure]" : $"Abort if {procedure}";
                }
                else if (type == "If")
                {
                    moduleName = string.IsNullOrWhiteSpace(procedure) ? "If [Procedure]" : $"If {procedure}";
                }
                else if (type == "If not")
                {
                    moduleName = string.IsNullOrWhiteSpace(procedure) ? "If not [Procedure]" : $"If not {procedure}";
                }
                else if (type == "Label")
                {
                    var normalized = CustomModuleStore.NormalizeLabelName(txtLabelName.Text ?? string.Empty);
                    moduleName = string.IsNullOrEmpty(normalized) ? "Label [Name]" : CustomModuleStore.ToLabelDisplay(normalized);
                }
                else if (type == "Goto")
                {
                    var selectedLabel = cboGotoLabel.SelectedItem as string;
                    moduleName = string.IsNullOrWhiteSpace(selectedLabel) ? "Goto [Label]" : $"Goto {selectedLabel}";
                }
                else // Run
                {
                    moduleName = string.IsNullOrWhiteSpace(procedure) ? "Run [Procedure]" : $"Run {procedure}";
                }
                
                // Generate name for "If message is Yes" modules
                if (type == "If message is Yes")
                {
                    moduleName = string.IsNullOrWhiteSpace(procedure)
                        ? "If message [Procedure] is Yes"
                        : $"If message {procedure} is Yes";
                }
                
                txtGeneratedName.Text = moduleName;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CreateModule] Error updating module name: {ex.Message}");
                txtGeneratedName.Text = "(Error generating name)";
            }
        }
    }
}

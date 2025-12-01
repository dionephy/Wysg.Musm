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
        private bool _isInitializing = true;
        
        public CreateModuleWindow()
        {
            InitializeComponent();
            LoadProperties();
            LoadProcedures();
            
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
                pnlProperty.Visibility = type == "Set" ? Visibility.Visible : Visibility.Collapsed;
                
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
            var moduleType = typeStr switch
            {
                "Run" => CustomModuleType.Run,
                "Set" => CustomModuleType.Set,
                "Abort if" => CustomModuleType.AbortIf,
                "If" => CustomModuleType.If,
                "If not" => CustomModuleType.IfNot,
                _ => CustomModuleType.Run
            };
            
            if (cboProcedure.SelectedItem is not string procedureName || 
                string.IsNullOrWhiteSpace(procedureName))
            {
                MessageBox.Show("Please select a custom procedure.", "Validation", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
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
            
            // Create the custom module with auto-generated name
            Result = new CustomModule
            {
                Name = moduleName,
                Type = moduleType,
                ProcedureName = procedureName,
                PropertyName = propertyName
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
                
                if (string.IsNullOrWhiteSpace(procedure))
                {
                    txtGeneratedName.Text = "(Select a procedure to generate name)";
                    return;
                }
                
                string moduleName;
                
                if (type == "Set")
                {
                    var property = cboProperty.SelectedItem as string;
                    if (string.IsNullOrWhiteSpace(property))
                    {
                        moduleName = $"Set [Property] to {procedure}";
                    }
                    else
                    {
                        moduleName = $"Set {property} to {procedure}";
                    }
                }
                else if (type == "Abort if")
                {
                    moduleName = $"Abort if {procedure}";
                }
                else if (type == "If")
                {
                    moduleName = $"If {procedure}";
                }
                else if (type == "If not")
                {
                    moduleName = $"If not {procedure}";
                }
                else // Run
                {
                    moduleName = $"Run {procedure}";
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

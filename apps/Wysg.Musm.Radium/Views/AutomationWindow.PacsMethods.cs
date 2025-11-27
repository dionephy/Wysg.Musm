using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.Views
{
    public partial class AutomationWindow
    {
        /// <summary>
        /// Observable collection of procedure names for UI binding
        /// </summary>
        public ObservableCollection<string> ProcedureNames { get; } = new();

        /// <summary>
        /// Initialize and load procedures from ui-procedures.json
        /// </summary>
        private void InitializePacsMethods()
        {
            try
            {
                // Set the procedure path override to use PACS-specific location
                string pacsKey = "default_pacs";
                if (Application.Current is App app)
                {
                    var tenant = app.Services.GetService(typeof(ITenantContext)) as ITenantContext;
                    if (tenant != null && !string.IsNullOrWhiteSpace(tenant.CurrentPacsKey))
                    {
                        pacsKey = tenant.CurrentPacsKey;
                    }
                }

                var appData = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Wysg.Musm", "Radium", "Pacs", SanitizeFileName(pacsKey));
                System.IO.Directory.CreateDirectory(appData);
                var procPath = System.IO.Path.Combine(appData, "ui-procedures.json");

                ProcedureExecutor.SetProcPathOverride(() => procPath);
                
                LoadPacsMethods();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AutomationWindow] Error initializing procedures: {ex.Message}");
                // Continue with empty list - user can add procedures manually
            }
        }

        /// <summary>
        /// Load procedure names from ui-procedures.json
        /// </summary>
        private void LoadPacsMethods()
        {
            try
            {
                ProcedureNames.Clear();
                var names = ProcedureExecutor.GetAllProcedureNames();
                foreach (var name in names.OrderBy(n => n))
                {
                    ProcedureNames.Add(name);
                }

                Debug.WriteLine($"[AutomationWindow] Loaded {ProcedureNames.Count} procedures from ui-procedures.json");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AutomationWindow] Error loading procedures: {ex.Message}");
            }
        }

        /// <summary>
        /// Add a new procedure
        /// </summary>
        private void OnAddPacsMethod(object sender, RoutedEventArgs e)
        {
            var dialog = CreateProcedureNameDialog("Add Custom Procedure", null);
            if (dialog.ShowDialog() != true) return;

            var procedureName = (string)dialog.Tag;

            try
            {
                // Check if procedure already exists
                if (ProcedureExecutor.ProcedureExists(procedureName))
                {
                    MessageBox.Show($"Procedure '{procedureName}' already exists", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Create empty procedure (operations will be added via grid)
                // This is done automatically when user saves operations in the grid
                LoadPacsMethods();

                // Select the new procedure
                if (FindName("cmbProcMethod") is ComboBox cmb)
                {
                    cmb.SelectedItem = procedureName;
                }

                txtStatus.Text = $"Added custom procedure '{procedureName}'";
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"Error adding custom procedure: {ex.Message}";
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Edit the selected procedure (rename)
        /// </summary>
        private void OnEditPacsMethod(object sender, RoutedEventArgs e)
        {
            var cmb = (ComboBox?)FindName("cmbProcMethod");
            if (cmb?.SelectedItem is not string selectedProcedure)
            {
                txtStatus.Text = "No custom procedure selected";
                return;
            }

            var dialog = CreateProcedureNameDialog("Rename Custom Procedure", selectedProcedure);
            if (dialog.ShowDialog() != true) return;

            var newName = (string)dialog.Tag;

            try
            {
                // Check if same name
                if (string.Equals(selectedProcedure, newName, StringComparison.OrdinalIgnoreCase))
                {
                    txtStatus.Text = "Procedure name unchanged";
                    return;
                }

                // Rename procedure
                if (!ProcedureExecutor.RenameProcedure(selectedProcedure, newName))
                {
                    MessageBox.Show($"Failed to rename procedure. '{newName}' may already exist.", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                LoadPacsMethods();

                // Re-select the renamed procedure
                cmb.SelectedItem = newName;

                txtStatus.Text = $"Renamed procedure to '{newName}'";
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"Error renaming procedure: {ex.Message}";
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Delete the selected procedure
        /// </summary>
        private void OnDeletePacsMethod(object sender, RoutedEventArgs e)
        {
            var cmb = (ComboBox?)FindName("cmbProcMethod");
            if (cmb?.SelectedItem is not string selectedProcedure)
            {
                txtStatus.Text = "No custom procedure selected";
                return;
            }

            var result = MessageBox.Show(
                $"Delete custom procedure '{selectedProcedure}'?\n\nThis will delete all its operations.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                if (!ProcedureExecutor.DeleteProcedure(selectedProcedure))
                {
                    MessageBox.Show($"Failed to delete procedure '{selectedProcedure}'", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                LoadPacsMethods();
                txtStatus.Text = $"Deleted custom procedure '{selectedProcedure}'";
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"Error deleting procedure: {ex.Message}";
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Create a dialog for entering/editing procedure name
        /// </summary>
        private Window CreateProcedureNameDialog(string title, string? existingName)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 500,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30)),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(208, 208, 208)),
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.SingleBorderWindow
            };

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Name label
            var lblName = new TextBlock
            {
                Text = "Procedure Name:",
                Margin = new Thickness(0, 0, 0, 5)
            };
            Grid.SetRow(lblName, 0);
            grid.Children.Add(lblName);

            // Name textbox
            var txtName = new TextBox
            {
                Text = existingName ?? string.Empty,
                Margin = new Thickness(0, 0, 0, 15),
                Padding = new Thickness(6, 4, 6, 4),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 48)),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(208, 208, 208)),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60)),
                BorderThickness = new Thickness(1),
                CaretBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White)
            };
            Grid.SetRow(txtName, 1);
            grid.Children.Add(txtName);

            // Button panel
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetRow(buttonPanel, 2);

            var btnOk = new Button
            {
                Content = "OK",
                Width = 80,
                Margin = new Thickness(0, 0, 10, 0),
                Padding = new Thickness(10, 5, 10, 5),
                IsDefault = true
            };
            btnOk.Click += (s, e) =>
            {
                var name = txtName.Text?.Trim();

                if (string.IsNullOrWhiteSpace(name))
                {
                    MessageBox.Show("Procedure name cannot be empty", "Validation", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validate name format (alphanumeric + underscores + spaces)
                if (!System.Text.RegularExpressions.Regex.IsMatch(name, @"^[a-zA-Z][a-zA-Z0-9_ ]*$"))
                {
                    MessageBox.Show("Procedure name must start with a letter and contain only letters, numbers, underscores, and spaces", 
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                dialog.Tag = name;
                dialog.DialogResult = true;
                dialog.Close();
            };
            buttonPanel.Children.Add(btnOk);

            var btnCancel = new Button
            {
                Content = "Cancel",
                Width = 80,
                Padding = new Thickness(10, 5, 10, 5),
                IsCancel = true
            };
            btnCancel.Click += (s, e) => { dialog.DialogResult = false; dialog.Close(); };
            buttonPanel.Children.Add(btnCancel);

            grid.Children.Add(buttonPanel);
            dialog.Content = grid;

            // Focus name textbox when dialog opens
            dialog.Loaded += (s, e) =>
            {
                txtName.Focus();
                txtName.SelectAll();
            };

            return dialog;
        }
    }
}

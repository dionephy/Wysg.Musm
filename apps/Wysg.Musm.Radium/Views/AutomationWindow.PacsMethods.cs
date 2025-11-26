using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Wysg.Musm.Radium.Models;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.Views
{
    public partial class SpyWindow
    {
        private PacsMethodManager? _pacsMethodManager;

        /// <summary>
        /// Observable collection of PACS methods for UI binding
        /// </summary>
        public ObservableCollection<PacsMethod> PacsMethods { get; } = new();

        /// <summary>
        /// Initialize PACS method manager and load methods for current PACS
        /// </summary>
        private void InitializePacsMethods()
        {
            try
            {
                // Get current PACS key from tenant context
                string pacsKey = "default_pacs";
                if (Application.Current is App app)
                {
                    var tenant = app.Services.GetService(typeof(ITenantContext)) as ITenantContext;
                    if (tenant != null && !string.IsNullOrWhiteSpace(tenant.CurrentPacsKey))
                    {
                        pacsKey = tenant.CurrentPacsKey;
                    }
                }

                _pacsMethodManager = new PacsMethodManager(pacsKey);
                LoadPacsMethods();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SpyWindow] Error initializing PACS methods: {ex.Message}");
                // Continue with empty list - user can add methods manually
            }
        }

        /// <summary>
        /// Load PACS methods from manager into observable collection
        /// </summary>
        private void LoadPacsMethods()
        {
            if (_pacsMethodManager == null) return;

            try
            {
                PacsMethods.Clear();
                var methods = _pacsMethodManager.GetAllMethods();
                foreach (var method in methods.OrderBy(m => m.Name))
                {
                    PacsMethods.Add(method);
                }

                Debug.WriteLine($"[SpyWindow] Loaded {PacsMethods.Count} PACS methods");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SpyWindow] Error loading PACS methods: {ex.Message}");
            }
        }

        /// <summary>
        /// Add a new PACS method
        /// </summary>
        private void OnAddPacsMethod(object sender, RoutedEventArgs e)
        {
            if (_pacsMethodManager == null)
            {
                txtStatus.Text = "Custom procedure manager not initialized";
                return;
            }

            var dialog = CreatePacsMethodDialog("Add Custom Procedure", null);
            if (dialog.ShowDialog() != true) return;

            var (name, tag) = ((string, string))dialog.Tag;

            try
            {
                var method = new PacsMethod
                {
                    Name = name,
                    Tag = tag,
                    IsBuiltIn = false
                };

                _pacsMethodManager.AddMethod(method);
                LoadPacsMethods();

                // Select the new method
                if (FindName("cmbProcMethod") is ComboBox cmb)
                {
                    cmb.SelectedItem = PacsMethods.FirstOrDefault(m => string.Equals(m.Tag, tag, StringComparison.OrdinalIgnoreCase));
                }

                txtStatus.Text = $"Added custom procedure '{name}'";
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"Error adding custom procedure: {ex.Message}";
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Edit the selected PACS method
        /// </summary>
        private void OnEditPacsMethod(object sender, RoutedEventArgs e)
        {
            if (_pacsMethodManager == null)
            {
                txtStatus.Text = "Custom procedure manager not initialized";
                return;
            }

            var cmb = (ComboBox?)FindName("cmbProcMethod");
            if (cmb?.SelectedItem is not PacsMethod selectedMethod)
            {
                txtStatus.Text = "No custom procedure selected";
                return;
            }

            if (selectedMethod.IsBuiltIn)
            {
                txtStatus.Text = "Cannot edit built-in custom procedures";
                return;
            }

            var dialog = CreatePacsMethodDialog("Edit Custom Procedure", selectedMethod);
            if (dialog.ShowDialog() != true) return;

            var (name, tag) = ((string, string))dialog.Tag;

            try
            {
                var method = new PacsMethod
                {
                    Name = name,
                    Tag = tag,
                    IsBuiltIn = false
                };

                _pacsMethodManager.UpdateMethod(selectedMethod.Tag, method);
                LoadPacsMethods();

                // Re-select the edited method
                cmb.SelectedItem = PacsMethods.FirstOrDefault(m => string.Equals(m.Tag, tag, StringComparison.OrdinalIgnoreCase));

                txtStatus.Text = $"Updated custom procedure '{name}'";
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"Error updating custom procedure: {ex.Message}";
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Delete the selected PACS method
        /// </summary>
        private void OnDeletePacsMethod(object sender, RoutedEventArgs e)
        {
            if (_pacsMethodManager == null)
            {
                txtStatus.Text = "Custom procedure manager not initialized";
                return;
            }

            var cmb = (ComboBox?)FindName("cmbProcMethod");
            if (cmb?.SelectedItem is not PacsMethod selectedMethod)
            {
                txtStatus.Text = "No custom procedure selected";
                return;
            }

            if (selectedMethod.IsBuiltIn)
            {
                txtStatus.Text = "Cannot delete built-in custom procedures";
                return;
            }

            var result = MessageBox.Show(
                $"Delete custom procedure '{selectedMethod.Name}'?\n\nThis will also delete its associated procedure steps.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                _pacsMethodManager.DeleteMethod(selectedMethod.Tag);
                LoadPacsMethods();

                txtStatus.Text = $"Deleted custom procedure '{selectedMethod.Name}'";
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"Error deleting custom procedure: {ex.Message}";
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Create a dialog for adding/editing PACS methods
        /// </summary>
        private Window CreatePacsMethodDialog(string title, PacsMethod? existingMethod)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 500,
                Height = 250,
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
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Name label
            var lblName = new TextBlock
            {
                Text = "Display Name:",
                Margin = new Thickness(0, 0, 0, 5)
            };
            Grid.SetRow(lblName, 0);
            grid.Children.Add(lblName);

            // Name textbox
            var txtName = new TextBox
            {
                Text = existingMethod?.Name ?? string.Empty,
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

            // Tag label
            var lblTag = new TextBlock
            {
                Text = "Method Tag (used in code):",
                Margin = new Thickness(0, 0, 0, 5)
            };
            Grid.SetRow(lblTag, 2);
            grid.Children.Add(lblTag);

            // Tag textbox
            var txtTag = new TextBox
            {
                Text = existingMethod?.Tag ?? string.Empty,
                Margin = new Thickness(0, 0, 0, 15),
                Padding = new Thickness(6, 4, 6, 4),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 48)),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(208, 208, 208)),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60)),
                BorderThickness = new Thickness(1),
                CaretBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White)
            };
            Grid.SetRow(txtTag, 3);
            grid.Children.Add(txtTag);

            // Button panel
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetRow(buttonPanel, 4);

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
                var tag = txtTag.Text?.Trim();

                if (string.IsNullOrWhiteSpace(name))
                {
                    MessageBox.Show("Display name cannot be empty", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(tag))
                {
                    MessageBox.Show("Method tag cannot be empty", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validate tag format (alphanumeric + underscores)
                if (!System.Text.RegularExpressions.Regex.IsMatch(tag, @"^[a-zA-Z][a-zA-Z0-9_]*$"))
                {
                    MessageBox.Show("Method tag must start with a letter and contain only letters, numbers, and underscores", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                dialog.Tag = (name, tag);
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

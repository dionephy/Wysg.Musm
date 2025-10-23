using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace Wysg.Musm.LlmDataBuilder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string DataFileName = "data.json";
        private const string PromptFileName = "prompt.txt";
        
        private string _workingDirectory;

        public MainWindow()
        {
            InitializeComponent();
            
            // Set working directory to the application's directory
            _workingDirectory = AppDomain.CurrentDomain.BaseDirectory;
            
            // Load existing data
            LoadPromptFile();
            UpdateRecordCount();
            UpdateStatus("Ready");
        }

        private void LoadPromptFile()
        {
            try
            {
                string promptPath = System.IO.Path.Combine(_workingDirectory, PromptFileName);
                if (File.Exists(promptPath))
                {
                    txtPrompt.Text = File.ReadAllText(promptPath);
                    UpdateStatus($"Loaded prompt from {PromptFileName}");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error loading prompt: {ex.Message}", isError: true);
            }
        }

        private void UpdateRecordCount()
        {
            try
            {
                string dataPath = System.IO.Path.Combine(_workingDirectory, DataFileName);
                if (File.Exists(dataPath))
                {
                    string json = File.ReadAllText(dataPath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var records = JsonSerializer.Deserialize<List<LlmDataRecord>>(json);
                        txtRecordCount.Text = $"Records: {records?.Count ?? 0}";
                        return;
                    }
                }
                txtRecordCount.Text = "Records: 0";
            }
            catch
            {
                txtRecordCount.Text = "Records: 0";
            }
        }

        private void UpdateStatus(string message, bool isError = false)
        {
            txtStatus.Text = message;
            txtStatus.Foreground = isError ? 
                new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red) : 
                new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
        }

        private void BtnGetProtoResult_Click(object sender, RoutedEventArgs e)
        {
            // Placeholder for future LLM server integration
            UpdateStatus("Get Proto Result - Not implemented yet", isError: false);
            txtProtoOutput.Text = "[This feature will connect to LLM server in the future]";
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(txtInput.Text))
                {
                    UpdateStatus("Error: Input cannot be empty", isError: true);
                    MessageBox.Show("Please enter an input value.", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtOutput.Text))
                {
                    UpdateStatus("Error: Output cannot be empty", isError: true);
                    MessageBox.Show("Please enter an output value.", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Parse applied prompt numbers
                List<int> appliedPromptNumbers = new List<int>();
                if (!string.IsNullOrWhiteSpace(txtAppliedPromptNumbers.Text))
                {
                    try
                    {
                        appliedPromptNumbers = txtAppliedPromptNumbers.Text
                            .Split(',')
                            .Select(s => int.Parse(s.Trim()))
                            .ToList();
                    }
                    catch
                    {
                        UpdateStatus("Error: Invalid prompt numbers format", isError: true);
                        MessageBox.Show("Please enter valid comma-separated numbers (e.g., 1,2,3).", 
                            "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // Create new record
                var newRecord = new LlmDataRecord
                {
                    Input = txtInput.Text,
                    Output = txtOutput.Text,
                    ProtoOutput = txtProtoOutput.Text,
                    AppliedPromptNumbers = appliedPromptNumbers
                };

                // Load existing records or create new list
                List<LlmDataRecord> records = new List<LlmDataRecord>();
                string dataPath = System.IO.Path.Combine(_workingDirectory, DataFileName);
                
                if (File.Exists(dataPath))
                {
                    string existingJson = File.ReadAllText(dataPath);
                    if (!string.IsNullOrWhiteSpace(existingJson))
                    {
                        records = JsonSerializer.Deserialize<List<LlmDataRecord>>(existingJson) 
                            ?? new List<LlmDataRecord>();
                    }
                }

                // Append new record
                records.Add(newRecord);

                // Save to JSON with formatting
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                string jsonOutput = JsonSerializer.Serialize(records, options);
                File.WriteAllText(dataPath, jsonOutput);

                // Save prompt.txt
                string promptPath = System.IO.Path.Combine(_workingDirectory, PromptFileName);
                File.WriteAllText(promptPath, txtPrompt.Text);

                // Clear data entry fields (but not prompt)
                ClearDataFields();

                // Update UI
                UpdateRecordCount();
                UpdateStatus($"Successfully saved! Total records: {records.Count}");
                
                MessageBox.Show($"Data saved successfully!\n\nFiles saved to:\n{_workingDirectory}", 
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error saving: {ex.Message}", isError: true);
                MessageBox.Show($"An error occurred while saving:\n\n{ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Clear all data entry fields (excluding prompt)?", 
                "Confirm Clear", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                ClearDataFields();
                UpdateStatus("Data fields cleared");
            }
        }

        private void ClearDataFields()
        {
            txtInput.Clear();
            txtOutput.Clear();
            txtProtoOutput.Clear();
            txtAppliedPromptNumbers.Clear();
            // Note: txtPrompt is intentionally NOT cleared
        }
    }

    /// <summary>
    /// Represents a single LLM training data record
    /// </summary>
    public class LlmDataRecord
    {
        public string Input { get; set; } = string.Empty;
        public string Output { get; set; } = string.Empty;
        public string ProtoOutput { get; set; } = string.Empty;
        public List<int> AppliedPromptNumbers { get; set; } = new List<int>();
    }
}
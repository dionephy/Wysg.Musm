using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using Wysg.Musm.LlmDataBuilder.Services;

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
        private ProofreadApiService _apiService;
        private ApiConfiguration _apiConfig;

        public MainWindow()
        {
            InitializeComponent();
            
            // Set working directory to the application's directory
            _workingDirectory = AppDomain.CurrentDomain.BaseDirectory;
            
            // Load API configuration
            _apiConfig = ApiConfiguration.Load(_workingDirectory);
            _apiService = new ProofreadApiService(_apiConfig.ApiUrl, _apiConfig.AuthToken);
            
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
                else
                {
                    // Set default prompt if file doesn't exist
                    txtPrompt.Text = _apiConfig.DefaultPrompt;
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
                new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 135, 113)) : 
                new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(204, 204, 204));
        }

        private void ChkAlwaysOnTop_Checked(object sender, RoutedEventArgs e)
        {
            this.Topmost = true;
            UpdateStatus("Window is now always on top");
        }

        private void ChkAlwaysOnTop_Unchecked(object sender, RoutedEventArgs e)
        {
            this.Topmost = false;
            UpdateStatus("Window is no longer always on top");
        }

        private async void BtnGetProtoResult_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(txtInput.Text))
                {
                    UpdateStatus("Error: Input cannot be empty", isError: true);
                    MessageBox.Show("Please enter an input value before getting proto result.", 
                        "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtPrompt.Text))
                {
                    UpdateStatus("Error: Prompt cannot be empty", isError: true);
                    MessageBox.Show("Please enter a prompt (e.g., 'Proofread').", 
                        "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Disable button during API call
                btnGetProtoResult.IsEnabled = false;
                UpdateStatus("Calling API...");

                // Call the API
                var response = await _apiService.GetProofreadResultAsync(
                    txtPrompt.Text,
                    txtInput.Text
                );

                if (response != null && response.Status == "completed")
                {
                    // Update Proto Output with the proofread text
                    txtProtoOutput.Text = response.ProofreadText;

                    // Build a detailed status message
                    string statusMessage = $"API Success! Model: {response.ModelName}, Latency: {response.LatencyMs}ms";
                    if (response.Issues.Count > 0)
                    {
                        statusMessage += $", Issues found: {response.Issues.Count}";
                    }

                    UpdateStatus(statusMessage);

                    // Show issues if any
                    if (response.Issues.Count > 0)
                    {
                        var issuesSummary = new StringBuilder();
                        issuesSummary.AppendLine($"Found {response.Issues.Count} issue(s):\n");
                        foreach (var issue in response.Issues)
                        {
                            issuesSummary.AppendLine($"- {issue.Category} ({issue.Severity})");
                            issuesSummary.AppendLine($"  Suggestion: {issue.Suggestion}");
                            issuesSummary.AppendLine($"  Confidence: {issue.Confidence:P0}\n");
                        }

                        MessageBox.Show(issuesSummary.ToString(), 
                            "Proofreading Issues Found", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Information);
                    }
                }
                else
                {
                    UpdateStatus($"API returned status: {response?.Status ?? "unknown"}", isError: true);
                    txtProtoOutput.Text = $"[API Error: {response?.FailureReason ?? "Unknown error"}]";
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error: {ex.Message}", isError: true);
                MessageBox.Show($"Failed to get proto result:\n\n{ex.Message}\n\nPlease check:\n" +
                    $"1. API server is running at {_apiConfig.ApiUrl}\n" +
                    $"2. Network connectivity\n" +
                    $"3. API configuration in api_config.json", 
                    "API Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
            finally
            {
                // Re-enable button
                btnGetProtoResult.IsEnabled = true;
            }
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
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
            AddMessage("Application started. Working directory: " + _workingDirectory);
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
                AddMessage($"ERROR loading prompt: {ex.Message}");
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
                        // Use JsonSerializerOptions to match saved format (camelCase)
                        var options = new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            PropertyNameCaseInsensitive = true
                        };
                        
                        var records = JsonSerializer.Deserialize<List<LlmDataRecord>>(json, options);
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

        private void AddMessage(string message, bool isError = false)
        {
 var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var prefix = isError ? "ERROR" : "INFO";
          var newMessage = $"[{timestamp}] {prefix}: {message}";
            
 if (string.IsNullOrEmpty(txtMessages.Text))
            {
    txtMessages.Text = newMessage;
          }
   else
    {
                txtMessages.Text += Environment.NewLine + newMessage;
        }
            
            // Auto-scroll to bottom
        txtMessages.ScrollToEnd();
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
        AddMessage("Validation failed: Please enter an input value before getting proto result.", isError: true);
       return;
      }

        if (string.IsNullOrWhiteSpace(txtPrompt.Text))
      {
        UpdateStatus("Error: Prompt cannot be empty", isError: true);
         AddMessage("Validation failed: Please enter a prompt (e.g., 'Proofread').", isError: true);
         return;
         }

      // Disable button during API call
     btnGetProtoResult.IsEnabled = false;
      UpdateStatus("Calling API...");
          AddMessage($"Calling API at {_apiConfig.ApiUrl}...");

    // Call the API
    var response = await _apiService.GetProofreadResultAsync(
           txtPrompt.Text,
         txtInput.Text,
        _apiConfig.Language,
         _apiConfig.Strictness
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
              AddMessage($"API call successful. Model: {response.ModelName}, Latency: {response.LatencyMs}ms");

     // Log issues if any
             if (response.Issues.Count > 0)
             {
AddMessage($"Found {response.Issues.Count} issue(s):");
            foreach (var issue in response.Issues)
     {
        AddMessage($"  - {issue.Category} ({issue.Severity}): {issue.Suggestion} (Confidence: {issue.Confidence:P0})");
               }
          }
  }
      else
     {
             UpdateStatus($"API returned status: {response?.Status ?? "unknown"}", isError: true);
      txtProtoOutput.Text = $"[API Error: {response?.FailureReason ?? "Unknown error"}]";
          AddMessage($"API call failed: {response?.FailureReason ?? "Unknown error"}", isError: true);
    }
    }
   catch (Exception ex)
 {
                UpdateStatus($"Error: {ex.Message}", isError: true);
           AddMessage($"API call failed: {ex.Message}", isError: true);
                AddMessage($"Please check: 1) API server is running at {_apiConfig.ApiUrl}, 2) Network connectivity, 3) API configuration in api_config.json", isError: true);
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
           // Trim all inputs first to prevent saving whitespace-only content
         string inputText = txtInput.Text?.Trim() ?? string.Empty;
            string outputText = txtOutput.Text?.Trim() ?? string.Empty;
           string protoOutputText = txtProtoOutput.Text?.Trim() ?? string.Empty;
           
        // Validate input (must have actual content, not just whitespace)
    if (string.IsNullOrWhiteSpace(inputText))
         {
         UpdateStatus("Error: Input cannot be empty", isError: true);
     AddMessage("Validation failed: Please enter an input value with actual content.", isError: true);
             txtInput.Focus();
   return;
   }

    // Validate output (must have actual content, not just whitespace)
    if (string.IsNullOrWhiteSpace(outputText))
             {
       UpdateStatus("Error: Output cannot be empty", isError: true);
      AddMessage("Validation failed: Please enter an output value with actual content.", isError: true);
              txtOutput.Focus();
       return;
          }

  // Parse applied prompt numbers - now accepts decimals like 1.1, 1.2, 1.3
        List<string> appliedPromptNumbers = new List<string>();
                if (!string.IsNullOrWhiteSpace(txtAppliedPromptNumbers.Text))
            {
         try
            {
   appliedPromptNumbers = txtAppliedPromptNumbers.Text
        .Split(',')
          .Select(s => s.Trim())
             .Where(s => !string.IsNullOrWhiteSpace(s))
          .ToList();
        
                // Validate that each entry is a valid number (integer or decimal)
            foreach (var num in appliedPromptNumbers)
               {
if (!decimal.TryParse(num, out _))
  {
       throw new FormatException($"Invalid number format: {num}");
       }
   }
          }
    catch
              {
               UpdateStatus("Error: Invalid prompt numbers format", isError: true);
     AddMessage("Validation failed: Please enter valid comma-separated numbers. Examples: 1,2,3 or 1.1,1.2,2.1", isError: true);
        return;
     }
         }

    // Create new record with trimmed values
      var newRecord = new LlmDataRecord
  {
         Input = inputText,
   Output = outputText,
   ProtoOutput = protoOutputText,
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
     // Use JsonSerializerOptions to properly deserialize camelCase JSON
     var deserializeOptions = new JsonSerializerOptions
            {
   PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
       PropertyNameCaseInsensitive = true
       };
             
     records = JsonSerializer.Deserialize<List<LlmDataRecord>>(existingJson, deserializeOptions) 
      ?? new List<LlmDataRecord>();
         }
         }

           // Append new record
        records.Add(newRecord);

       // Save to JSON with formatting
var serializeOptions = new JsonSerializerOptions
     {
         WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
 };
      string jsonOutput = JsonSerializer.Serialize(records, serializeOptions);
   File.WriteAllText(dataPath, jsonOutput);

         // Save prompt.txt
  string promptPath = System.IO.Path.Combine(_workingDirectory, PromptFileName);
        File.WriteAllText(promptPath, txtPrompt.Text);

            // Clear data entry fields (but not prompt)
     ClearDataFields();

        // Update UI
     UpdateRecordCount();
           UpdateStatus($"Successfully saved! Total records: {records.Count}");
    AddMessage($"Data saved successfully! Total records: {records.Count}. Files saved to: {_workingDirectory}");
    }
            catch (Exception ex)
        {
       UpdateStatus($"Error saving: {ex.Message}", isError: true);
 AddMessage($"Save failed: {ex.Message}", isError: true);
       }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
  {
        ClearDataFields();
            UpdateStatus("Data fields cleared");
            AddMessage("Data entry fields cleared (prompt preserved)");
        }

        private void ClearDataFields()
      {
    txtInput.Clear();
            txtOutput.Clear();
            txtProtoOutput.Clear();
    txtAppliedPromptNumbers.Clear();
   // Note: txtPrompt is intentionally NOT cleared
 }

    private void BtnBrowseData_Click(object sender, RoutedEventArgs e)
        {
 try
      {
    var browserWindow = new DataBrowserWindow(_workingDirectory);
      browserWindow.Owner = this;
                browserWindow.ShowDialog();
        
          // Refresh record count in case data was modified in the browser
    UpdateRecordCount();
       UpdateStatus("Data browser closed");
            AddMessage("Data browser window closed");
            }
    catch (Exception ex)
            {
         UpdateStatus($"Error opening data browser: {ex.Message}", isError: true);
         AddMessage($"Failed to open data browser: {ex.Message}", isError: true);
   }
        }

        private void BtnCleanup_Click(object sender, RoutedEventArgs e)
        {
            AddMessage("Starting cleanup of blank records...");
            CleanupBlankRecords();
            UpdateRecordCount();
  }

        private void CleanupBlankRecords()
        {
         try
            {
   string dataPath = System.IO.Path.Combine(_workingDirectory, DataFileName);
       if (!File.Exists(dataPath))
      {
       UpdateStatus("No data file to clean up");
      AddMessage("Cleanup skipped: No data.json file found.");
            return;
       }

    string existingJson = File.ReadAllText(dataPath);
           if (string.IsNullOrWhiteSpace(existingJson))
   {
           UpdateStatus("Data file is empty");
            AddMessage("Cleanup skipped: Data file is empty.");
return;
}

              // Use JsonSerializerOptions to match saved format (camelCase)
           var deserializeOptions = new JsonSerializerOptions
       {
   PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
       PropertyNameCaseInsensitive = true
   };

                var records = JsonSerializer.Deserialize<List<LlmDataRecord>>(existingJson, deserializeOptions) 
            ?? new List<LlmDataRecord>();

             // Filter out blank records (where Input or Output is empty/whitespace)
 int originalCount = records.Count;
                records = records.Where(r => 
            !string.IsNullOrWhiteSpace(r.Input) && 
      !string.IsNullOrWhiteSpace(r.Output)
           ).ToList();

 int removedCount = originalCount - records.Count;

             if (removedCount > 0)
          {
 // Create backup file first
         string backupPath = System.IO.Path.Combine(_workingDirectory, 
      $"data.backup.{DateTime.Now:yyyyMMddHHmmss}.json");
    File.Copy(dataPath, backupPath);

        // Save cleaned data
           var serializeOptions = new JsonSerializerOptions
 {
        WriteIndented = true,
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
  string jsonOutput = JsonSerializer.Serialize(records, serializeOptions);
File.WriteAllText(dataPath, jsonOutput);

 UpdateStatus($"Cleaned up {removedCount} blank record(s). Backup saved.");
        AddMessage($"Cleanup complete: Removed {removedCount} blank record(s). Remaining records: {records.Count}");
          AddMessage($"Backup saved: {System.IO.Path.GetFileName(backupPath)}");
  }
      else
        {
       UpdateStatus("No blank records found");
             AddMessage("Cleanup complete: No blank records found.");
     }
            }
    catch (Exception ex)
      {
              UpdateStatus($"Cleanup error: {ex.Message}", isError: true);
         AddMessage($"Cleanup failed: {ex.Message}", isError: true);
            }
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
   public List<string> AppliedPromptNumbers { get; set; } = new List<string>();
    }
}
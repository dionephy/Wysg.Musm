using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;

namespace Wysg.Musm.LlmDataBuilder
{
    /// <summary>
    /// Interaction logic for DataBrowserWindow.xaml
    /// </summary>
    public partial class DataBrowserWindow : Window
    {
        private const string DataFileName = "data.json";
        private string _workingDirectory;
        private ObservableCollection<LlmDataRecordViewModel> _records;

        public DataBrowserWindow(string workingDirectory)
        {
            InitializeComponent();
            _workingDirectory = workingDirectory;
            _records = new ObservableCollection<LlmDataRecordViewModel>();
            dgRecords.ItemsSource = _records;
            
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                _records.Clear();
                
                string dataPath = Path.Combine(_workingDirectory, DataFileName);
                if (File.Exists(dataPath))
                {
                    string json = File.ReadAllText(dataPath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        // Use JsonNamingPolicy.CamelCase to match the saved format
                        var options = new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            PropertyNameCaseInsensitive = true
                        };
                        
                        var records = JsonSerializer.Deserialize<List<LlmDataRecord>>(json, options);
                        if (records != null)
                        {
                            for (int i = 0; i < records.Count; i++)
                            {
                                _records.Add(new LlmDataRecordViewModel(records[i], i + 1));
                            }
                        }
                    }
                }

                txtRecordCount.Text = $"Records: {_records.Count}";
                UpdateStatus($"Loaded {_records.Count} record(s)");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error loading data: {ex.Message}", isError: true);
                MessageBox.Show($"Failed to load data:\n\n{ex.Message}", 
                    "Load Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }

        private void SaveData()
        {
            try
            {
                var records = _records.Select(vm => vm.ToRecord()).ToList();
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                string jsonOutput = JsonSerializer.Serialize(records, options);
                string dataPath = Path.Combine(_workingDirectory, DataFileName);
                File.WriteAllText(dataPath, jsonOutput);
                
                UpdateStatus("Data saved successfully");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error saving data: {ex.Message}", isError: true);
                MessageBox.Show($"Failed to save data:\n\n{ex.Message}", 
                    "Save Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
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

        private void DgRecords_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (dgRecords.SelectedItem is LlmDataRecordViewModel selectedRecord)
            {
                txtDetailInput.Text = selectedRecord.Input;
                txtDetailOutput.Text = selectedRecord.Output;
                txtDetailProtoOutput.Text = selectedRecord.ProtoOutput;
                txtDetailPromptNumbers.Text = selectedRecord.AppliedPromptNumbersDisplay;
                
                UpdateStatus($"Viewing record #{selectedRecord.Index}");
            }
            else
            {
                txtDetailInput.Clear();
                txtDetailOutput.Clear();
                txtDetailProtoOutput.Clear();
                txtDetailPromptNumbers.Clear();
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            if (dgRecords.SelectedItem is LlmDataRecordViewModel selectedRecord)
            {
                try
                {
                    var saveFileDialog = new SaveFileDialog
                    {
                        Filter = "JSON files (*.json)|*.json|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                        DefaultExt = "json",
                        FileName = $"record_{selectedRecord.Index}.json"
                    };

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        var options = new JsonSerializerOptions
                        {
                            WriteIndented = true,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        };
                        
                        string jsonOutput = JsonSerializer.Serialize(selectedRecord.ToRecord(), options);
                        File.WriteAllText(saveFileDialog.FileName, jsonOutput);
                        
                        UpdateStatus($"Record #{selectedRecord.Index} exported successfully");
                        MessageBox.Show($"Record exported to:\n{saveFileDialog.FileName}", 
                            "Export Success", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatus($"Error exporting record: {ex.Message}", isError: true);
                    MessageBox.Show($"Failed to export record:\n\n{ex.Message}", 
                        "Export Error", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a record to export.", 
                    "No Selection", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning);
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgRecords.SelectedItem is LlmDataRecordViewModel selectedRecord)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete record #{selectedRecord.Index}?\n\n" +
                    $"Input: {selectedRecord.Input.Substring(0, Math.Min(50, selectedRecord.Input.Length))}...",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _records.Remove(selectedRecord);
                        
                        // Re-index remaining records
                        for (int i = 0; i < _records.Count; i++)
                        {
                            _records[i].Index = i + 1;
                        }
                        
                        SaveData();
                        txtRecordCount.Text = $"Records: {_records.Count}";
                        UpdateStatus($"Record deleted. Total records: {_records.Count}");
                        
                        // Clear detail panel
                        txtDetailInput.Clear();
                        txtDetailOutput.Clear();
                        txtDetailProtoOutput.Clear();
                        txtDetailPromptNumbers.Clear();
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus($"Error deleting record: {ex.Message}", isError: true);
                        MessageBox.Show($"Failed to delete record:\n\n{ex.Message}", 
                            "Delete Error", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a record to delete.", 
                    "No Selection", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    /// <summary>
    /// View model for displaying LlmDataRecord in the DataGrid
    /// </summary>
    public class LlmDataRecordViewModel
    {
        public int Index { get; set; }
        public string Input { get; set; } = string.Empty;
        public string Output { get; set; } = string.Empty;
        public string ProtoOutput { get; set; } = string.Empty;
        public List<string> AppliedPromptNumbers { get; set; } = new List<string>();
        
        public string AppliedPromptNumbersDisplay => 
            AppliedPromptNumbers.Count > 0 
                ? string.Join(", ", AppliedPromptNumbers) 
                : "-";

        public LlmDataRecordViewModel(LlmDataRecord record, int index)
        {
            Index = index;
            Input = record.Input;
            Output = record.Output;
            ProtoOutput = record.ProtoOutput;
            AppliedPromptNumbers = record.AppliedPromptNumbers ?? new List<string>();
        }

        public LlmDataRecord ToRecord()
        {
            return new LlmDataRecord
            {
                Input = Input,
                Output = Output,
                ProtoOutput = ProtoOutput,
                AppliedPromptNumbers = AppliedPromptNumbers
            };
        }
    }
}

# Custom Modules Feature - Implementation Guide (2025-11-25)

## Overview
This feature allows users to create custom automation modules that combine module types (Run, Set, Abort If) with Custom Procedures to create reusable automation components.

## Architecture

### Components Required

1. **Model Layer** (`CustomModule.cs`) ? CREATED
   - `CustomModule` class with Name, Type, ProcedureName, PropertyName
   - `CustomModuleType` enum (Run, Set, AbortIf)
   - `CustomModuleStore` for persistence
   - `CustomModuleProperties` constants for property mappings

2. **UI Layer** (TO CREATE)
   - `CreateModuleWindow.xaml` - Dialog for creating custom modules
   - `CreateModuleWindow.xaml.cs` - Code-behind for dialog logic
   - Updates to `SpyWindow.xaml` - Add Custom Modules pane
   - Updates to `SpyWindow.Automation.cs` - Wire up Create Module button

3. **ViewModel Layer** (TO UPDATE)
   - `SettingsViewModel.cs` - Add CustomModules collection
   - `SpyWindow.Automation.cs` - Initialize custom modules list

4. **Execution Layer** (TO UPDATE)
   - `MainViewModel.Commands.Automation.cs` - Add custom module execution logic
   - Handle Run, Set, and Abort If types
   - Map properties to MainViewModel fields

## Implementation Steps

### Step 1: Create Module Dialog Window ? Model Created

Create `apps/Wysg.Musm.Radium/Views/CreateModuleWindow.xaml`:

```xaml
<Window x:Class="Wysg.Musm.Radium.Views.CreateModuleWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Create Custom Module" 
        Width="500" Height="400"
        Background="#1E1E1E" Foreground="#D0D0D0"
        WindowStartupLocation="CenterOwner">
    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <!-- Module Name -->
        <TextBlock Grid.Row="0" Text="Module Name:" Margin="0,0,0,5"/>
        <TextBox x:Name="txtModuleName" Grid.Row="1" Margin="0,0,0,15" 
                 Background="#2D2D30" Foreground="#D0D0D0" 
                 BorderBrush="#3C3C3C" Padding="6,4"/>
        
        <!-- Module Type -->
        <TextBlock Grid.Row="2" Text="Module Type:" Margin="0,0,0,5"/>
        <ComboBox x:Name="cboModuleType" Grid.Row="3" Margin="0,0,0,15"
                  Background="#2D2D30" Foreground="#D0D0D0" 
                  BorderBrush="#3C3C3C" Padding="6,4"
                  SelectionChanged="OnModuleTypeChanged">
            <ComboBoxItem Content="Run"/>
            <ComboBoxItem Content="Set"/>
            <ComboBoxItem Content="Abort if"/>
        </ComboBox>
        
        <!-- Property Selection (for Set type) -->
        <StackPanel x:Name="pnlProperty" Grid.Row="4" Visibility="Collapsed" Margin="0,0,0,15">
            <TextBlock Text="Property:" Margin="0,0,0,5"/>
            <ComboBox x:Name="cboProperty" 
                      Background="#2D2D30" Foreground="#D0D0D0" 
                      BorderBrush="#3C3C3C" Padding="6,4" Margin="0,0,0,10"/>
            <TextBlock Text="to" Margin="0,0,0,5"/>
        </StackPanel>
        
        <!-- Procedure Selection -->
        <StackPanel x:Name="pnlProcedure" Grid.Row="5" Margin="0,0,0,15">
            <TextBlock Text="Custom Procedure:" Margin="0,0,0,5"/>
            <ComboBox x:Name="cboProcedure" 
                      Background="#2D2D30" Foreground="#D0D0D0" 
                      BorderBrush="#3C3C3C" Padding="6,4"/>
        </StackPanel>
        
        <!-- Buttons -->
        <StackPanel Grid.Row="6" Orientation="Horizontal" HorizontalAlignment="Right">
            <Button Content="Save" Width="80" Margin="0,0,10,0" Click="OnSave" IsDefault="True"/>
            <Button Content="Cancel" Width="80" Click="OnCancel" IsCancel="True"/>
        </StackPanel>
    </Grid>
</Window>
```

Create `apps/Wysg.Musm.Radium/Views/CreateModuleWindow.xaml.cs`:

```csharp
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Wysg.Musm.Radium.Models;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.Views
{
    public partial class CreateModuleWindow : Window
    {
        public CustomModule? Result { get; private set; }
        
        public CreateModuleWindow()
        {
            InitializeComponent();
            LoadProperties();
            LoadProcedures();
        }
        
        private void LoadProperties()
        {
            foreach (var prop in CustomModuleProperties.AllProperties)
            {
                cboProperty.Items.Add(prop);
            }
        }
        
        private void LoadProcedures()
        {
            try
            {
                // Load PACS methods from PacsMethodManager
                var methods = PacsMethodManager.GetAllMethods();
                foreach (var method in methods.OrderBy(m => m.Name))
                {
                    cboProcedure.Items.Add(method.Name);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CreateModule] Error loading procedures: {ex.Message}");
            }
        }
        
        private void OnModuleTypeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboModuleType.SelectedItem is ComboBoxItem item)
            {
                var type = item.Content.ToString();
                pnlProperty.Visibility = type == "Set" ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        
        private void OnSave(object sender, RoutedEventArgs e)
        {
            // Validate inputs
            var moduleName = txtModuleName.Text?.Trim();
            if (string.IsNullOrWhiteSpace(moduleName))
            {
                MessageBox.Show("Please enter a module name.", "Validation", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (cboModuleType.SelectedItem is not ComboBoxItem typeItem)
            {
                MessageBox.Show("Please select a module type.", "Validation", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            var typeStr = typeItem.Content.ToString();
            var moduleType = typeStr switch
            {
                "Run" => CustomModuleType.Run,
                "Set" => CustomModuleType.Set,
                "Abort if" => CustomModuleType.AbortIf,
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
            
            // Create the custom module
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
    }
}
```

### Step 2: Add Custom Modules Pane to SpyWindow

Update `apps/Wysg.Musm.Radium/Views/SpyWindow.xaml` Automation tab:

Add after Available Modules pane:

```xaml
<!-- Custom Modules (parallel to Available Modules) -->
<Border Grid.Row="0" Grid.Column="3" Grid.RowSpan="6" 
        BorderBrush="#3C3C3C" BorderThickness="1" CornerRadius="4" 
        Padding="6" Background="#252526" Margin="8,0,0,0">
    <DockPanel>
        <StackPanel DockPanel.Dock="Top">
            <TextBlock Text="Custom Modules" FontWeight="SemiBold" 
                       Margin="0,0,0,4" Foreground="#D0D0D0"/>
            <Button x:Name="btnCreateModule" Content="Create Module" 
                    Margin="0,0,0,8" Click="OnCreateModule" 
                    Style="{StaticResource SpyWindowButtonStyle}"/>
        </StackPanel>
        <ListBox x:Name="lstCustomModules" Background="#1E1E1E" 
                 Foreground="#D0D0D0" BorderBrush="#3C3C3C" 
                 PreviewMouseMove="OnAutomationProcDrag" 
                 DragLeave="OnAutomationListDragLeave" 
                 Tag="CustomModules"/>
    </DockPanel>
</Border>
```

Update Grid.ColumnDefinitions:
```xaml
<Grid.ColumnDefinitions>
    <ColumnDefinition Width="*"/>
    <ColumnDefinition Width="*"/>
    <ColumnDefinition Width="Auto"/>
    <ColumnDefinition Width="Auto"/> <!-- NEW for Custom Modules -->
</Grid.ColumnDefinitions>
```

### Step 3: Wire Up Custom Modules Logic

Update `apps/Wysg.Musm.Radium/Views/SpyWindow.Automation.cs`:

```csharp
private ObservableCollection<string> _customModules = new();

private void InitializeAutomationTab()
{
    // ... existing code ...
    
    // NEW: Initialize Custom Modules
    if (FindName("lstCustomModules") is ListBox customList)
    {
        LoadCustomModules();
        customList.ItemsSource = _customModules;
        customList.PreviewMouseLeftButtonDown += OnAutomationListMouseDown;
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
```

### Step 4: Update SettingsViewModel

Update `apps/Wysg.Musm.Radium/ViewModels/SettingsViewModel.cs`:

In constructor, after initializing AvailableModules:

```csharp
// Load custom modules into available modules
LoadCustomModules();

private void LoadCustomModules()
{
    try
    {
        var store = CustomModuleStore.Load();
        foreach (var module in store.Modules)
        {
            if (!AvailableModules.Contains(module.Name))
            {
                AvailableModules.Add(module.Name);
            }
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"[SettingsVM] Error loading custom modules: {ex.Message}");
    }
}
```

### Step 5: Implement Module Execution Logic

Update `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Commands.Automation.cs`:

Add to `RunModulesSequentially`:

```csharp
// Check if this is a custom module
var customStore = CustomModuleStore.Load();
var customModule = customStore.GetModule(m);

if (customModule != null)
{
    await RunCustomModuleAsync(customModule);
    continue;
}

// ... existing module handling ...

private async Task RunCustomModuleAsync(CustomModule module)
{
    try
    {
        Debug.WriteLine($"[CustomModule] Executing '{module.Name}' type={module.Type} proc={module.ProcedureName}");
        
        // Run the procedure and get result
        var result = await _pacs.RunProcedureAsync(module.ProcedureName);
        
        switch (module.Type)
        {
            case CustomModuleType.Run:
                // Just run the procedure, result ignored
                SetStatus($"Custom module '{module.Name}' executed");
                break;
                
            case CustomModuleType.AbortIf:
                // Abort if result is true/non-empty
                var shouldAbort = !string.IsNullOrWhiteSpace(result) && 
                                  !string.Equals(result, "false", StringComparison.OrdinalIgnoreCase);
                if (shouldAbort)
                {
                    SetStatus($"Custom module '{module.Name}' aborted sequence", true);
                    throw new OperationCanceledException($"Aborted by {module.Name}");
                }
                break;
                
            case CustomModuleType.Set:
                // Set property value
                if (string.IsNullOrWhiteSpace(module.PropertyName))
                {
                    Debug.WriteLine($"[CustomModule] Set module '{module.Name}' has no property name");
                    break;
                }
                
                SetPropertyValue(module.PropertyName, result);
                SetStatus($"Custom module '{module.Name}' set {module.PropertyName} = {result}");
                break;
        }
    }
    catch (OperationCanceledException)
    {
        throw; // Propagate abort
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"[CustomModule] Error executing '{module.Name}': {ex.Message}");
        SetStatus($"Custom module '{module.Name}' failed: {ex.Message}", true);
        throw;
    }
}

private void SetPropertyValue(string propertyName, string value)
{
    switch (propertyName)
    {
        case CustomModuleProperties.CurrentPatientName:
            PatientName = value;
            break;
        case CustomModuleProperties.CurrentPatientNumber:
            PatientNumber = value;
            break;
        case CustomModuleProperties.CurrentPatientAge:
            PatientAge = value;
            break;
        case CustomModuleProperties.CurrentPatientSex:
            PatientSex = value;
            break;
        case CustomModuleProperties.CurrentStudyStudyname:
            StudyName = value;
            break;
        case CustomModuleProperties.CurrentStudyDatetime:
            if (DateTime.TryParse(value, out var dt))
                StudyDateTime = dt;
            break;
        case CustomModuleProperties.CurrentStudyRemark:
            StudyRemark = value;
            break;
        case CustomModuleProperties.CurrentPatientRemark:
            PatientRemark = value;
            break;
            
        // Previous study properties - store in temporary fields
        case CustomModuleProperties.PreviousStudyStudyname:
            TempPreviousStudyStudyname = value;
            break;
        case CustomModuleProperties.PreviousStudyDatetime:
            if (DateTime.TryParse(value, out var pdt))
                TempPreviousStudyDatetime = pdt;
            break;
        case CustomModuleProperties.PreviousStudyReportDatetime:
            if (DateTime.TryParse(value, out var prdt))
                TempPreviousStudyReportDatetime = prdt;
            break;
        case CustomModuleProperties.PreviousStudyReportReporter:
            TempPreviousStudyReportReporter = value;
            break;
        case CustomModuleProperties.PreviousStudyReportHeaderAndFindings:
            TempPreviousStudyReportHeaderAndFindings = value;
            break;
        case CustomModuleProperties.PreviousStudyReportConclusion:
            TempPreviousStudyReportConclusion = value;
            break;
            
        default:
            Debug.WriteLine($"[CustomModule] Unknown property: {propertyName}");
            break;
    }
}
```

Add temporary properties to MainViewModel:

```csharp
// Temporary storage for previous study properties
public string? TempPreviousStudyStudyname { get; set; }
public DateTime? TempPreviousStudyDatetime { get; set; }
public DateTime? TempPreviousStudyReportDatetime { get; set; }
public string? TempPreviousStudyReportReporter { get; set; }
public string? TempPreviousStudyReportHeaderAndFindings { get; set; }
public string? TempPreviousStudyReportConclusion { get; set; }
```

## Usage Examples

### Example 1: Run Custom Procedure
```
Module Name: "Run Get Patient Name"
Type: Run
Procedure: "Get current patient name"
```
Result: Simply executes the procedure, no value stored.

### Example 2: Set Current Patient Name
```
Module Name: "Set Current Patient Name to Get current patient name"
Type: Set
Property: "Current Patient Name"
Procedure: "Get current patient name"
```
Result: Executes procedure, stores result in MainViewModel.PatientName

### Example 3: Abort If Patient Mismatch
```
Module Name: "Abort if Patient Number Not Match"
Type: Abort if
Procedure: "Check patient number match"
```
Result: If procedure returns true/non-empty, aborts the automation sequence

### Example 4: Store Previous Study Data
```
Module Name: "Get Previous Study Studyname"
Type: Set
Property: "Previous Study Studyname"
Procedure: "Get selected previous study name"
```
Result: Stores in TempPreviousStudyStudyname for later use

## Storage Format

Custom modules are stored in `%AppData%\Wysg.Musm\Radium\custom-modules.json`:

```json
{
  "Modules": [
    {
      "Name": "Set Current Patient Name to Get current patient name",
      "Type": 1,
      "ProcedureName": "Get current patient name",
      "PropertyName": "Current Patient Name"
    },
    {
      "Name": "Abort if Patient Number Not Match",
      "Type": 2,
      "ProcedureName": "Check patient number match",
      "PropertyName": null
    }
  ]
}
```

## Testing Checklist

- [ ] Create Module dialog opens correctly
- [ ] Module types populate correctly (Run, Set, Abort if)
- [ ] Property list shows all 14 properties
- [ ] Procedure list shows all Custom Procedures
- [ ] Property panel shows/hides based on module type
- [ ] Save validates all required fields
- [ ] Created module appears in Custom Modules list
- [ ] Custom module can be dragged to automation panes
- [ ] Run type executes procedure correctly
- [ ] Set type stores value in correct MainViewModel property
- [ ] Abort If type aborts sequence on true result
- [ ] Previous Study properties store in temporary fields
- [ ] Modules persist across application restarts

## Integration Points

1. **PacsService**: Must have `RunProcedureAsync(string procedureName)` method
2. **PacsMethodManager**: Used to load available procedures
3. **MainViewModel**: Properties must be accessible for Set operations
4. **SettingsViewModel**: AvailableModules must include custom modules
5. **Automation Tab**: Must support drag-drop for custom modules

## Future Enhancements

- Edit existing custom modules
- Delete custom modules
- Export/import custom module sets
- Module parameters/variables
- Conditional logic within modules
- Module templates library

---

**Status**: DESIGN COMPLETE  
**Implementation**: REQUIRES CODING  
**Estimated Effort**: 4-6 hours  
**Files to Create**: 2 (CreateModuleWindow.xaml, CreateModuleWindow.xaml.cs)  
**Files to Modify**: 4 (SpyWindow.xaml, SpyWindow.Automation.cs, SettingsViewModel.cs, MainViewModel.Commands.Automation.cs)  

---

*Document Created: 2025-11-25*  
*Author: GitHub Copilot*  
*Type: Implementation Guide*

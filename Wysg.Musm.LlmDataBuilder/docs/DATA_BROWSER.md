# Data Browser Feature

## Overview

The Data Browser is a new window that allows you to view, manage, and export training data records stored in `data.json`. It provides a comprehensive interface for browsing through all saved records with advanced features like deletion, export, and detailed viewing.

## Accessing the Data Browser

From the main LLM Data Builder window, click the **"Browse Data"** button located at the bottom of the window (blue border, leftmost button in the action button group).

## Features

### 1. Data Grid View

The main view displays all records in a tabular format with the following columns:

| Column | Description |
|--------|-------------|
| **#** | Sequential record number (index) |
| **Input** | The input/prompt text |
| **Output** | The expected output/answer |
| **Proto Output** | The LLM-generated prototype output |
| **Prompts** | Applied prompt template numbers (comma-separated) |

**Features:**
- **Sortable columns**: Click column headers to sort
- **Resizable columns**: Drag column borders to resize
- **Alternating row colors**: Easier visual scanning (dark and slightly lighter rows)
- **Row selection**: Click any row to select it
- **Full-row highlighting**: Selected rows are highlighted in blue

### 2. Record Details Panel

An expandable panel at the bottom shows full details of the selected record:

- **Input**: Full text with scrolling support
- **Output**: Full text with scrolling support
- **Proto Output**: Full text with scrolling support
- **Applied Prompt Numbers**: Full list of prompt IDs

**To use:**
1. Select a record in the data grid
2. Expand the "Record Details" section (if collapsed)
3. View the full text of all fields with proper formatting

### 3. Action Buttons

#### Refresh Button (Blue Border)
- **Function**: Reloads data from `data.json`
- **Use case**: If the file was modified externally or you want to discard unsaved changes
- **Keyboard shortcut**: F5 (planned future enhancement)

#### Export Selected Button (Yellow Border)
- **Function**: Exports the selected record to a separate JSON file
- **Steps**:
  1. Select a record in the grid
  2. Click "Export Selected"
  3. Choose location and filename in the save dialog
  4. File is saved with proper JSON formatting
- **Default filename**: `record_[index].json`
- **Supported formats**: JSON (recommended), TXT, or any file type

#### Delete Selected Button (Red Border)
- **Function**: Permanently deletes the selected record from `data.json`
- **Steps**:
  1. Select a record in the grid
  2. Click "Delete Selected"
  3. Confirm deletion in the warning dialog
  4. Record is removed and file is saved immediately
- **Warning**: This action cannot be undone! The record is permanently deleted from `data.json`
- **Auto-reindexing**: Remaining records are automatically renumbered

#### Close Button (Gray Border)
- **Function**: Closes the Data Browser window
- **Effect**: Returns to the main window with updated record count

### 4. Status Bar

Located at the top of the window, displays:
- **Status messages**: Current operation status
- **Record count**: Total number of records loaded
- **Always on Top checkbox**: Keep window above other applications

### 5. Always on Top

Same functionality as the main window:
- Check the box to keep the Data Browser above all other windows
- Useful when referencing multiple sources
- Status updates when toggled

## Usage Scenarios

### Viewing All Records
1. Click "Browse Data" from the main window
2. Scroll through the data grid
3. Click any record to see full details in the detail panel

### Finding a Specific Record
1. Open the Data Browser
2. Use column sorting to organize records (click column headers)
3. Scroll through the sorted list
4. Select the record to view details

### Exporting a Record
1. Select the record you want to export
2. Click "Export Selected"
3. Choose destination folder and filename
4. Click Save

**Example use case**: Creating individual training examples for sharing or testing

### Deleting Incorrect Records
1. Identify the record to delete
2. Select it in the grid
3. Click "Delete Selected"
4. Confirm the deletion
5. The file is immediately saved with the record removed

**Example use case**: Removing duplicate or incorrect training data

### Verifying Recent Additions
1. After saving records in the main window
2. Click "Browse Data"
3. Scroll to the bottom to see newest records
4. Verify content is correct

## Data Grid Features

### Selection Behavior
- **Single selection mode**: Only one record can be selected at a time
- **Full-row selection**: Clicking anywhere on a row selects it
- **Keyboard navigation**: Use arrow keys to move between records (future enhancement)

### Visual Indicators
- **Alternating row colors**: Better readability
- **Blue highlight**: Currently selected record
- **Grid lines**: Clear separation between cells
- **Column headers**: Bold text with darker background

### Read-Only Protection
- Records cannot be edited directly in the grid
- Prevents accidental modifications
- Use main window to create new records
- Use Delete button to remove records

## Window Properties

- **Title**: "Data Browser"
- **Size**: 1400x800 pixels (larger than main window for better data viewing)
- **Position**: Center of screen
- **Theme**: Dark theme matching main application
- **Modal**: Yes (blocks main window while open)
- **Owner**: Main window (appears on top of main window)

## File Operations

### Loading Data
- Automatically loads on window open
- Reads from `data.json` in the application directory
- Handles missing or empty files gracefully
- Shows error message if JSON is corrupted

### Saving Data
- Automatically saves after deletion
- Maintains proper JSON formatting
- Uses camelCase property names
- Pretty-printed with indentation

### Export Format
Exported files contain a single record in JSON format:

```json
{
  "input": "Example input text",
  "output": "Example output text",
  "protoOutput": "Example proto output",
  "appliedPromptNumbers": [1, 2, 3]
}
```

## Error Handling

### Load Errors
- **Cause**: Corrupted JSON, file access issues
- **Effect**: Error message displayed, empty grid shown
- **Solution**: Check file permissions, validate JSON syntax

### Export Errors
- **Cause**: No write permission, disk full
- **Effect**: Error message with details
- **Solution**: Check destination folder permissions

### Delete Errors
- **Cause**: File locked by another process
- **Effect**: Error message, record remains
- **Solution**: Close other applications accessing the file

## Tips and Best Practices

### Before Deleting
- ? Review the record in the detail panel
- ? Export a backup if unsure
- ? Confirm you have the correct record selected
- ? Don't delete without reading the confirmation dialog

### Managing Large Datasets
- Use column sorting to organize records
- Export subsets of data for analysis
- Use the detail panel for full text review
- Consider splitting very large datasets into separate files

### Data Integrity
- Keep backups of `data.json` before major deletions
- Export important records before experimenting
- Use the Refresh button if you make external changes
- Verify changes in the main window after closing the browser

### Workflow Integration
1. Use main window for data entry
2. Periodically review data in browser
3. Delete errors or duplicates
4. Export specific records for sharing
5. Continue adding more data

## Keyboard Support (Future Enhancements)

Planned keyboard shortcuts:
- **F5**: Refresh data
- **Ctrl+E**: Export selected
- **Delete**: Delete selected (with confirmation)
- **Esc**: Close window
- **Arrow keys**: Navigate grid
- **Enter**: Expand/collapse details

## Visual Design

### Color Scheme
Matches the main application's dark theme:
- **Background**: `#1E1E1E`
- **Surface**: `#252526`
- **Highlights**: `#2D2D30`
- **Borders**: `#3E3E42`
- **Text**: `#CCCCCC`
- **Selected row**: `#007ACC` (blue)
- **Error text**: `#F48771` (red)

### Button Colors
- **Refresh**: Blue border (accent)
- **Export**: Yellow border (warning/caution)
- **Delete**: Red border (danger)
- **Close**: Gray border (neutral)

## Technical Details

### Data Binding
- Uses `ObservableCollection<LlmDataRecordViewModel>` for reactive updates
- Automatic UI updates when records are added/removed
- Two-way binding for selection state

### View Model
`LlmDataRecordViewModel` provides:
- Sequential indexing (1-based)
- Formatted display of prompt numbers
- Conversion to/from `LlmDataRecord` domain model

### Performance
- Efficient for datasets up to 10,000 records
- Lazy rendering of rows (WPF virtualization)
- Fast sorting using built-in DataGrid features

## Troubleshooting

### Issue: Window won't open
**Solution**: Check for exceptions in the main window, ensure `data.json` path is valid

### Issue: Grid is empty
**Solution**: Verify `data.json` exists and contains valid data, use Refresh button

### Issue: Can't select records
**Solution**: Ensure grid has focus, check if data loaded successfully

### Issue: Export fails
**Solution**: Check write permissions for destination folder, ensure disk space available

### Issue: Deleted records reappear
**Solution**: Ensure file saved successfully, check for file locking issues, try Refresh

## Integration with Main Window

- **Shared working directory**: Both windows use the same `data.json` location
- **Automatic refresh**: Main window record count updates when browser closes
- **Modal dialog**: Browser blocks main window while open
- **Consistent theme**: Same dark theme styling

## Future Enhancements

Planned features:
- **Search/filter functionality**: Find records by content
- **Bulk operations**: Select and delete/export multiple records
- **Inline editing**: Edit records directly in the grid
- **Column customization**: Show/hide columns, reorder
- **Export all**: Export entire dataset or filtered subset
- **Import**: Merge records from external JSON files
- **Statistics**: Show data quality metrics
- **Duplicate detection**: Identify similar records

## Support

For issues or feature requests related to the Data Browser:
1. Check error messages carefully
2. Verify `data.json` is valid JSON
3. Ensure file permissions are correct
4. Review this documentation
5. Report bugs with specific error messages

---

**The Data Browser provides a powerful way to manage your LLM training data with a user-friendly interface and robust error handling.**

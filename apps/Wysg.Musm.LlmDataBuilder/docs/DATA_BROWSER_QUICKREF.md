# Data Browser Feature - Quick Reference

## What is the Data Browser?

The Data Browser is a new window that allows you to view, manage, and export all your saved training data records from `data.json`. Think of it as a spreadsheet view of your data with powerful management features.

## How to Access

Click the **"Browse Data"** button (blue border, leftmost button) at the bottom of the main window.

## Key Features

### 1. View All Records
- **DataGrid**: Sortable table showing all records
- **Columns**: #, Input, Output, Proto Output, Prompts
- **Details Panel**: Expandable view for full record content

### 2. Export Records
- Select a record
- Click "Export Selected"
- Save to JSON file
- Share or archive individual records

### 3. Delete Records
- Select a record
- Click "Delete Selected"
- Confirm deletion
- File is updated immediately
- ?? Cannot undo!

### 4. Refresh Data
- Click "Refresh" to reload from file
- Useful after external edits

## Quick Tips

? **DO:**
- Review records in detail panel before deleting
- Export backups of important records
- Use sorting to find specific records
- Check record count in status bar

? **DON'T:**
- Delete without reading confirmation
- Forget to export backups
- Rely on undo (there isn't one!)

## Keyboard Support (Coming Soon)

Future keyboard shortcuts planned:
- **F5**: Refresh
- **Ctrl+E**: Export selected
- **Delete**: Delete selected
- **Esc**: Close window

## Color Coding

- **Blue border**: Browse Data, Refresh (navigation/info)
- **Yellow border**: Export (caution - action with external effect)
- **Red border**: Delete (danger - permanent action)
- **Gray border**: Close (neutral)

## Use Cases

### 1. Quality Control
Browse all records to verify correctness before training.

### 2. Data Sharing
Export specific high-quality examples for sharing with team.

### 3. Data Cleanup
Identify and remove duplicate or incorrect records.

### 4. Record Review
View full content of any record without editing.

## Technical Details

- **Window Size**: 1400¡¿800 pixels
- **Selection Mode**: Single row
- **File Format**: JSON with camelCase properties
- **Auto-indexing**: Records renumbered after deletion

## Common Questions

**Q: Can I edit records in the browser?**  
A: No, use the main window to create new records. Browser is read-only (except delete).

**Q: Can I select multiple records?**  
A: Not yet, single selection only (future enhancement).

**Q: What happens if I delete by mistake?**  
A: Deletion is permanent. Always export a backup first!

**Q: Can I import records?**  
A: Not yet, but planned for future version.

## See Also

- **[DATA_BROWSER.md](DATA_BROWSER.md)**: Complete guide
- **[README.md](README.md)**: Main documentation
- **[CHANGELOG.md](CHANGELOG.md)**: Version history

---

**Remember**: The Data Browser is a powerful tool for managing your training data. Always review records carefully before deleting!

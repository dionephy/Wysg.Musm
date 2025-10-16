# Manual Updates Required for SpyWindow.xaml

## Context
The following changes need to be made manually to `apps\Wysg.Musm.Radium\Views\SpyWindow.xaml` because the file is very large and automated editing cannot reliably locate all the necessary ComboBox definitions.

## Changes Required

### 1. Add New Map-to Bookmarks (PP3)
**Location**: Find the `<ComboBox x:Name="cmbKnown"` element in the XAML

**Add these ComboBoxItems**:
```xaml
<ComboBoxItem Tag="Screen_MainCurrentStudyTab">Screen_main current study tab</ComboBoxItem>
<ComboBoxItem Tag="Screen_SubPreviousStudyTab">Screen_sub previous study tab</ComboBoxItem>
```

**Purpose**: Allows users to map screen area bookmarks for dual-monitor PACS workflows

### 2. Add New PACS Methods (PP4)
**Location**: Find the ComboBox for PACS Methods (typically `<ComboBox x:Name="cmbProcMethod"` or similar)

**Add these ComboBoxItems**:
```xaml
<ComboBoxItem Tag="SetCurrentStudyInMainScreen">Set current study in main screen</ComboBoxItem>
<ComboBoxItem Tag="SetPreviousStudyInSubScreen">Set previous study in sub screen</ComboBoxItem>
```

**Purpose**: Enables automated screen switching procedures for comparison workflows

### 3. Add ClickElement Operation (PP5)
**Location**: Find the ComboBox for Operations (where operations like "GetText", "Invoke", "MouseClick" are listed)

**Add this ComboBoxItem**:
```xaml
<ComboBoxItem Tag="ClickElement">ClickElement</ComboBoxItem>
```

**Purpose**: Allows clicking at the center of a resolved UI element, bridging bookmark resolution and mouse automation

## Verification After Manual Update

After making these changes:

1. **Build the solution** - Ensure no XAML parsing errors
2. **Run the application**
3. **Open SpyWindow** (Tools ¡æ Spy or similar menu)
4. **Verify Map-to dropdown** contains:
   - "Screen_main current study tab"
   - "Screen_sub previous study tab"
5. **Verify Custom Procedures ¡æ PACS Method** combo contains:
   - "Set current study in main screen"
   - "Set previous study in sub screen"
6. **Verify Custom Procedures ¡æ Operation** combo contains:
   - "ClickElement"

## Technical Notes

- All the backend code (Services, ViewModels, etc.) has been implemented
- Only the XAML UI declarations are missing
- The ComboBox Tag values must match exactly as shown (case-sensitive)
- The display text can be adjusted for clarity but Tags must remain as specified

## Related Issues

- **PP6 (msctls_statusbar32 reliability)**: Already addressed by FR-920..FR-925 bookmark robustness improvements. The "not supported" error is detected and manual walker is used as fallback. The issue occurs because the PACS statusbar control intermittently doesn't support standard UIA queries, but the manual walker succeeds reliably after re-pick.

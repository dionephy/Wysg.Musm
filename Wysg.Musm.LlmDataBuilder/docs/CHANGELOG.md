# Changelog

## Version 1.1.0 - Dark Theme & Always on Top

### New Features

#### 1. Dark Theme
- **Complete UI Overhaul**: Application now features a modern dark theme
- **Color Palette**:
  - Background: `#1E1E1E` (Dark gray)
  - Surface: `#252526` (Controls background)
  - Text: `#CCCCCC` (Light gray)
  - Borders: `#3E3E42` (Subtle borders)
  - Accent: `#007ACC` (Blue)
  - Success: `#73C991` (Green)
  - Warning: `#CCA700` (Yellow)
  - Error: `#F48771` (Red)
- **Benefits**:
  - Reduced eye strain during extended use
  - Better visibility in low-light environments
  - Modern, professional appearance
  - Consistent with popular development tools

#### 2. Always on Top Feature
- **Location**: Checkbox in the status bar (top-right corner)
- **Functionality**: Keep the application window above all other windows
- **Use Cases**:
  - Reference other applications while entering data
  - Multi-tasking across multiple monitors
  - Keeping the window visible during workflow
- **Feedback**: Status bar message when toggled on/off

### UI Improvements

#### Button Styling
- Dark themed buttons with colored borders
- Hover effects: Background changes to `#3E3E42`
- Press effects: Background changes to accent color `#007ACC`
- Visual hierarchy maintained through border colors:
  - **Save**: Green border (`#73C991`)
  - **Clear**: Red border (`#F48771`)
  - **Get Proto Result**: Yellow border (`#CCA700`)

#### Text Box Styling
- Dark background (`#252526`)
- Light text (`#CCCCCC`)
- Subtle borders (`#3E3E42`)
- Light-colored caret for visibility

#### Status Bar
- Dark surface background (`#252526`)
- Light text for readability
- Error text in red (`#F48771`)
- Always on Top checkbox integrated seamlessly

### Documentation Updates

#### Updated Files
1. **README.md**
   - Added dark theme section
   - Documented Always on Top feature
   - Updated UI Improvements section
   - Added color scheme details

2. **UI_REFERENCE.md**
   - Complete dark theme color palette
   - Always on Top checkbox documentation
   - Updated visual states table
   - Added keyboard navigation for checkbox
   - Updated button states and interactions

3. **QUICKSTART.md**
   - Mentioned dark theme in launch section
   - Added Always on Top to key features
   - Updated button guide with visual cues
   - Added window controls section

4. **CHANGELOG.md** (New)
   - Comprehensive list of changes
   - Version history tracking

### Technical Changes

#### MainWindow.xaml
- Added comprehensive dark theme resource dictionary
- Defined color brushes for consistency
- Updated all control styles for dark theme
- Added custom button template with hover/press states
- Added Always on Top checkbox to status bar
- Reorganized status bar with Grid layout

#### MainWindow.xaml.cs
- Added `ChkAlwaysOnTop_Checked` event handler
- Added `ChkAlwaysOnTop_Unchecked` event handler
- Updated status text colors to match dark theme
- Maintained all existing functionality

### Compatibility
- **.NET Version**: 9.0 (unchanged)
- **C# Version**: 13.0 (unchanged)
- **Framework**: WPF (unchanged)
- **Breaking Changes**: None - all existing functionality preserved

### Future Enhancements
- Theme toggle (light/dark mode selection)
- Remember Always on Top preference
- Additional keyboard shortcuts
- Theme customization options

---

## Version 1.0.0 - Initial Release

### Features
- Data entry interface for LLM training data
- Prompt management
- JSON data persistence
- Input validation
- Record counting
- Clear functionality

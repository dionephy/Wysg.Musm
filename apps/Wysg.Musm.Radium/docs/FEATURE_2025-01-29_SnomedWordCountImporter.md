# FEATURE: SNOMED-CT Concept Import by Word Count (2025-01-29)

## Overview
Added a new "Import SNOMED-CT Concepts by Word Count" feature accessible from the Global Phrases settings tab. This tool allows users to systematically browse and import SNOMED-CT concept synonyms filtered by word count, with the ability to add them as active or inactive global phrases. **Searches ALL SNOMED domains** without semantic tag filtering.

## Implementation Summary

### New Files Created
1. **ViewModels/SnomedWordCountImporterViewModel.cs**
   - ViewModel for the word count importer window
   - Manages search state, pagination, and candidate queue
   - Handles Add (active) and Ignore (inactive) operations
   - Filters SNOMED concepts by target word count (1-10 words)
   - **Searches across ALL domains** using "all" semantic tag
   - Tracks statistics: Added, Ignored, Total Processed

2. **Views/SnomedWordCountImporterWindow.xaml**
   - UI for the import tool
   - Configuration panel (Word count target only - no domain selector)
   - Statistics panel (Added/Ignored/Total counters)
   - Current candidate display
   - Action buttons (Add/Ignore)
   - Status bar and close button

3. **Views/SnomedWordCountImporterWindow.xaml.cs**
   - Code-behind for the window
   - Window initialization and close handling

### Modified Files
1. **Views/SettingsTabs/GlobalPhrasesSettingsTab.xaml**
   - Added "Import by Word Count" button next to "Browse SNOMED CT"
   - Button launches the new importer window

2. **Views/SettingsTabs/GlobalPhrasesSettingsTab.xaml.cs**
   - Added `OnOpenWordCountImporterClick` event handler
   - Dependency injection for required services
   - Refreshes global phrases after window closes

## Feature Details

### User Workflow
1. Navigate to Settings ¡æ Global Phrases tab
2. Click "Import by Word Count" button
3. Configure search:
   - Set target word count (default: 1 for single-word terms)
   - **No domain selection** - searches all SNOMED concepts
4. Click "Start Import"
5. For each candidate term:
   - View the term and concept info (includes semantic tag)
   - Choose "Add (Active)" to add as active global phrase
   - Choose "Ignore (Inactive)" to add as inactive global phrase
   - Automatically loads next candidate after action
6. Track progress via statistics panel
7. Close when finished

### Technical Implementation

#### Filtering Logic
- Fetches concepts from Snowstorm API using **"all" semantic tag**
- Searches across ALL SNOMED domains (no filtering)
- Filters synonyms (not FSN or PT) by exact word count match
- Skips terms that already exist as global phrases
- Uses pagination to handle large result sets efficiently

#### Data Management
- **Add**: Creates active global phrase + SNOMED mapping
- **Ignore**: Creates inactive global phrase + SNOMED mapping
- Both operations cache the SNOMED concept and create phrase-to-concept mapping
- Notes field includes: "Imported via Word Count Importer (N-word)"

#### Candidate Queue
- Pre-loads matching candidates in queue for fast browsing
- Fetches next page when queue is empty
- Continues until no more concepts available

### UI Design
- **Configuration Panel**: Word count input only (1-10 range)
- **Statistics Panel**: Large colored counters for Added (green), Ignored (orange), Total (purple)
- **Current Candidate**: Displays term (large blue text) and concept info (includes semantic tag)
- **Action Buttons**: Large green "Add" and orange "Ignore" buttons for easy selection
- **Status Bar**: Shows current operation and progress messages

### Performance Considerations
- Fetches 50 concepts per page batch
- Filters locally to minimize API calls
- Uses efficient phrase existence checks
- Supports token-based pagination for Snowstorm API

## Benefits Over Domain-Specific Search
- **Comprehensive Coverage**: Finds terms across all SNOMED hierarchies
- **Discovery**: Users encounter diverse medical terminology
- **Flexibility**: Semantic tag shown in concept info allows informed decisions
- **Simplicity**: No need to choose domain upfront
- **Efficiency**: Single workflow for all term types

## Testing Recommendations
1. Test with different word counts (1, 2, 3, etc.)
2. Verify diverse semantic tags appear in results
3. Verify skipping of existing phrases
4. Verify both Add and Ignore create correct phrase states
5. Test pagination when many concepts available
6. Verify statistics counters update correctly
7. Verify refresh of global phrases list after close

## Benefits
- **Efficient Import**: Quickly build global phrase library from SNOMED-CT
- **Focused Selection**: Filter by word count for specific use cases
- **Flexibility**: Add as active OR ignore as inactive
- **Progress Tracking**: Clear statistics and status messages
- **Integration**: Seamless integration with existing SNOMED mapping infrastructure
- **Comprehensive**: Searches all SNOMED domains for maximum coverage

## Future Enhancements
- Add multiple word count selection (e.g., 1-3 words)
- Add post-fetch semantic tag filtering within results
- Add undo/redo functionality
- Export/import of added phrases
- Bulk processing mode (auto-add/ignore patterns)

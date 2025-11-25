# Radium Documentation

**Last Updated**: 2025-11-25  
**Organization**: Relative Time Periods

---

## ?? Quick Navigation

| Time Period | Age | Location | Description |
|-------------|-----|----------|-------------|
| **Week 0** (Current) | 0-6 days | [00-current/](00-current/) | Active development |
| **Weeks 1-4** (Recent) | 7-34 days | [01-recent/](01-recent/) | Recent completed work |
| **This Quarter** | 35-90 days | [02-this-quarter/](02-this-quarter/) | Current quarter work |
| **Last Quarter** | 91-180 days | [03-last-quarter/](03-last-quarter/) | Previous quarter |
| **Archive** | 180+ days | [04-archive/](04-archive/) | Historical docs |

---

## ?? By Category (Timeless)

| Category | Location | Description |
|----------|----------|-------------|
| **Features** | [10-features/](10-features/) | Feature specifications by domain |
| **Architecture** | [11-architecture/](11-architecture/) | System architecture & design |
| **Guides** | [12-guides/](12-guides/) | User & developer guides |
| **Templates** | [99-templates/](99-templates/) | Document templates |

---

## ?? Current Sprint (Week 0)

**Last 7 days** - Active development and recent completions

### Editor & Completion (5 items)
- ? **[BUGFIX] Completion Window Blank Items** - Fixed double-filtering causing invisible items
- ? **[BUGFIX] Phrase Filtering** - Sort all phrases first, then take top 15
- ? **[ENHANCEMENT] Hotkeys/Snippets Edit** - In-place editing without delete-and-readd
- ? **[ENHANCEMENT] Alt+Left Copy to Caret** - Paste at cursor instead of end
- ? **[USABILITY] Shift Modifier Support** - Added Shift key to global hotkey capture

### Navigation & UI (2 items)
- ? **[ENHANCEMENT] Alt+Arrow Navigation** - Reorganized directions for intuitive workflow
- ? **[ENHANCEMENT] Always on Top** - Window stays on top feature

### Critical Fixes (3 items)
- ? **[CRITICAL FIX] Key Hook Conflicts** - Fixed keyboard hook consuming keys
- ? **[CRITICAL FIX] Editor Autofocus Caching** - Cached bookmark resolution
- ? **[CRITICAL FIX] Child Element Focus** - Prevent focus on child elements

### Configuration (2 items)
- ? **[BUGFIX] Google Login Connection String** - Fixed ConnectionStrings:Azure access
- ? **[BUGFIX] Encrypted Settings Corruption** - Null initialization fix

---

## ?? Recent (Weeks 1-4)

### Week 1 (7-13 days ago)
**Web Automation & Element Picker**
- ? **[ENHANCEMENT] Web Browser Element Picker** - "Pick Web" button for browser elements
- ? **[BUGFIX] Web Bookmark Robustness** - Fixed dynamic tab title issues
- ? **[ENHANCEMENT] SetValue Web Operation** - SetValue for web elements
- ? **[ENHANCEMENT] Conditional Send Report** - Conditional procedure logic

**Editor Improvements**
- ? **[BUGFIX] Selection Replacement** - Standard text deletion on selection
- ? **[BUGFIX] Comparison Field First Load** - Load before tab selection
- ? **[REMOVAL] Chief Complaint/Patient History Proofread** - Simplified UI (2 fewer rows)
- ? **[ENHANCEMENT] Copy Study Remark Toggle** - Quick copy to Chief Complaint

### Week 2 (14-20 days ago)
**UI Enhancements**
- ? **[ENHANCEMENT] Dark Theme Tab Control** - Dark theme for JSON panel tabs
- ? **[FIX] JSON TextBox Scrollbar** - Added vertical scrollbar
- ? **[ENHANCEMENT] Remove JSON Toggle** - JSON panel always visible
- ? **[ENHANCEMENT] Move Edit Buttons** - Buttons to CurrentReportEditorPanel toolbar
- ? **[BUGFIX] Alt Key System Menu** - Bypass Alt key menu activation

### Week 3 (21-27 days ago)
**Previous Study Fixes**
- ? **[CRITICAL FIX] Split Ranges Loading Order** - Fixed concatenated content issue
- ? **[CRITICAL FIX] Proofread Fields Update** - Update on report change
- ? **[CRITICAL FIX] Proofread JSON Persistence** - Write edits to JSON
- ? **[CRITICAL FIX] Split Ranges Persistence** - Persist across sessions
- ? **[FIX] Save Button** - Update JSON before save
- ? **[FIX] Disable Auto-Save** - Manual save only on tab switch

### Week 4 (28-34 days ago)
**SNOMED Integration**
- ? **[FIX] Phrase SNOMED Link Window** - Fixed search and FSN display

---

## ??? This Quarter (Weeks 5-12)

### Month 1 (35-64 days ago)

**Performance Optimizations**
- ? **[PERFORMANCE] Phrase Tabs** - 90%+ faster (2000+ phrases)
- ? **[PERFORMANCE] UI Bookmarks Fast-Fail** - 90%+ faster (30s �� 3s)
- ? **[PERFORMANCE] AddPreviousStudy Retry** - 65-85% faster
- ? **[PERFORMANCE] AddPreviousStudy Early Exit** - 93% faster

**Feature Enhancements**
- ? **[FEATURE] Findings Diff Visualization** - Collapsible diff viewer
- ? **[FIX] Word-Level Diff Granularity** - Word-by-word differences
- ? **[ENHANCEMENT] Collapsible JSON Panels** - Default collapsed state
- ? **[ENHANCEMENT] Previous Report Selector** - ComboBox with all reports
- ? **[ENHANCEMENT] Alphabetical Sorting** - Global phrases A-Z

**Reportify System**
- ? **[ENHANCEMENT] Arrow/Bullet Continuation** - Hierarchical formatting
- ? **[FIX] Unicode Dash Normalization** - Prevent character loss
- ? **[MAINTENANCE] Debug Log Cleanup** - Improved responsiveness

**Other**
- ? **[FIX] Previous Report JSON Loading** - Load all fields from database
- ? **[ENHANCEMENT] New Study Empty JSON** - Clean state for new study
- ? **[ENHANCEMENT] Spy Window UI** - GetTextWait, better labels
- ? **[ENHANCEMENT] PACS Module Timing Logs** - Performance monitoring

### Month 2 (65-90 days ago)

**Completion & Editor**
- ? **[FIX] GetCurrent* Operations** - Return actual editor text
- ? **[FIX] Completion Filter Trigger Only** - Filter by trigger, not description
- ? **[FIX] Number Digits in Triggers** - Keep popup open for "no2", "f3"

**Automation & Dialogs**
- ? **[ENHANCEMENT] Abort Confirmation Dialogs** - Confirm instead of abort
- ? **[ENHANCEMENT] Header Proofread Visualization** - Show proofread when toggle ON

---

## ??? Last Quarter (91-180 days ago)

### 2025-Q1 (January - March)
See [03-last-quarter/2025-Q1/](03-last-quarter/2025-Q1/) for complete list

**Highlights:**
- SNOMED CT Integration Complete
- Foreign Text Sync
- Phrase-SNOMED Mapping
- Multi-PACS Tenancy

---

## ?? By Feature Category

### ?? Editor & Text Processing
[10-features/ui/editor/](10-features/ui/editor/)
- Editor autofocus and focus management
- Text selection and replacement
- Font and styling (D2Coding font)
- Proofread visualization
- Diff viewer (word-level granularity)

### ?? Navigation & Shortcuts
[10-features/ui/navigation/](10-features/ui/navigation/)
- Alt+Arrow navigation (reorganized workflow)
- Copy to caret position (Alt+Left)
- Keyboard shortcut customization
- Navigation map and quick reference

### ?? PACS Automation
[10-features/automation/](10-features/automation/)
- Web browser element picker
- UI element bookmarks and validation
- Conditional procedures
- SetValue/Click/Delay operations
- Admin privilege support for UI Spy

### ?? Phrase System
[10-features/phrases/](10-features/phrases/)
- Phrase completion window
- Phrase highlighting and colorization
- Global phrase management
- Snippet expansion (Mode 1, Mode 2)
- SNOMED phrase mapping

### ?? SNOMED CT Integration
[10-features/snomed/](10-features/snomed/)
- SNOMED browser
- Phrase-SNOMED mapping
- Semantic tag coloring
- FSN (Fully Specified Name) display
- Snowstorm API integration

### ?? Previous Studies
[10-features/previous-studies/](10-features/previous-studies/)
- Previous study loading and display
- Report selector (ComboBox)
- Split range management
- JSON persistence
- Comparison field management

### ?? Reportify System
[10-features/reportify/](10-features/reportify/)
- Report formatting and numbering
- Arrow/bullet continuation support
- Paragraph spacing control
- Unicode dash normalization
- Line mode processing

---

## ??? Architecture

[11-architecture/](11-architecture/)

### Overview
- [Data Flow](11-architecture/overview/data-flow.md)
- [Component Diagram](11-architecture/overview/component-diagram.md)
- [Tech Stack](11-architecture/overview/tech-stack.md)

### ViewModels
- MainViewModel
- SettingsViewModel
- HotkeysViewModel
- SnippetsViewModel
- GlobalPhrasesViewModel

### Services
- EditorAutofocusService
- RadiumLocalSettings
- AzureSqlPhraseService
- ApiPhraseServiceAdapter

### Refactorings
- ProcedureExecutor Refactoring
- Operation Executor Consolidation
- Partial Class Split

---

## ?? Guides

[12-guides/](12-guides/)

### User Guides
- [Phrase Highlighting Usage](12-guides/user/phrase-highlighting-usage.md)
- [Snippet Logic](12-guides/user/snippet_logic.md)
- [Keyboard Shortcuts](12-guides/user/keyboard-shortcuts.md)

### Developer Guides
- [Quick Start Development](12-guides/developer/QUICKSTART_DEVELOPMENT.md)
- [Debug Logging](12-guides/developer/DEBUG_LOGGING_IMPLEMENTATION.md)
- [Editor Specification](12-guides/developer/spec_editor.md)

### Diagnostic Guides
- [Patient Number Mismatch](12-guides/diagnostics/DIAGNOSTIC_GUIDE_PatientNumberMismatch.md)
- [Technique Refresh](12-guides/diagnostics/DIAGNOSTIC_GUIDE_TechniqueRefresh.md)

---

## ?? Finding Information

### By Time Period
- **Last week** �� [00-current/](00-current/)
- **Last month** �� [01-recent/](01-recent/)
- **This quarter** �� [02-this-quarter/](02-this-quarter/)
- **Last quarter** �� [03-last-quarter/](03-last-quarter/)
- **Historical** �� [04-archive/](04-archive/)

### By Feature
- **Editor** �� [10-features/ui/editor/](10-features/ui/editor/)
- **Navigation** �� [10-features/ui/navigation/](10-features/ui/navigation/)
- **Automation** �� [10-features/automation/](10-features/automation/)
- **Phrases** �� [10-features/phrases/](10-features/phrases/)
- **SNOMED** �� [10-features/snomed/](10-features/snomed/)

### By Component
- **ViewModels** �� [11-architecture/viewmodels/](11-architecture/viewmodels/)
- **Services** �� [11-architecture/services/](11-architecture/services/)
- **Controls** �� [11-architecture/controls/](11-architecture/controls/)

### By Document Type
- **Bug Fixes** �� Search for `BUGFIX_` prefix
- **Features** �� Search for `FEATURE_` prefix
- **Enhancements** �� Search for `ENHANCEMENT_` prefix
- **Performance** �� Search for `PERFORMANCE_` prefix

---

## ?? Statistics

### Current Sprint (Week 0)
- **Documents**: 12
- **Bug Fixes**: 5
- **Enhancements**: 5
- **Critical Fixes**: 3

### Recent (Weeks 1-4)
- **Documents**: 35
- **Bug Fixes**: 12
- **Enhancements**: 18
- **Critical Fixes**: 8

### This Quarter
- **Documents**: 50+
- **Performance Optimizations**: 15
- **Major Features**: 8
- **Critical Fixes**: 12

---

## ?? Contributing

### Adding New Documentation
1. Use appropriate [template](99-templates/)
2. Follow [naming conventions](RELATIVE_TIME_ORGANIZATION.md#file-naming-convention)
3. Place in **00-current/** (will auto-age to correct location)
4. Add to this README in "Week 0" section

### Document Aging
Documents automatically move to age-appropriate folders:
- **Week 0** (0-6 days) �� **00-current/**
- **Week 1** (7-13 days) �� **01-recent/week-1/**
- **Month 1** (35-64 days) �� **02-this-quarter/month-1/**
- **Last Quarter** (91-180 days) �� **03-last-quarter/[year-quarter]/**
- **Archive** (180+ days) �� **04-archive/[year]/**

Auto-archiving runs weekly (see `auto-archive-docs.ps1`)

---

## ?? Documentation Standards

### Naming Conventions
- **Week 0**: `[TYPE]_w0_[Description].md`
- **Recent**: `[TYPE]_w[1-4]_[Description].md`
- **Quarter**: `[TYPE]_q[Q]m[M]_[Description].md`
- **Timeless**: `[TYPE]_[Component]_[Description].md`

### Document Types
- `FEATURE_` - New feature specifications
- `ENHANCEMENT_` - Feature improvements
- `BUGFIX_` - Bug fixes
- `CRITICAL_FIX_` - Critical issues
- `PERFORMANCE_` - Performance optimizations
- `REMOVAL_` - Removed features
- `SUMMARY_` - Implementation summaries

---

## ??? Maintenance

### Weekly Tasks
- Auto-archive aged documents (automatic via script)
- Update "Week 0" section in README
- Review and categorize new documents

### Monthly Tasks
- Review and update feature category READMEs
- Check for broken links
- Consolidate duplicate content

### Quarterly Tasks
- Archive quarter's work to `03-last-quarter/`
- Generate quarterly summary
- Review and update architecture docs

---

## ?? Support

- **Issues**: Create GitHub issue with `documentation` label
- **Questions**: Check [Guides](12-guides/) first
- **Improvements**: Submit PR with documentation updates

---

## ??? Related Documents

- [Reorganization Plan](REORGANIZATION_PLAN.md) - Original reorganization blueprint
- [Relative Time Organization](RELATIVE_TIME_ORGANIZATION.md) - Why relative periods?
- [Complete Index](INDEX.md) - Comprehensive searchable index
- [Phase 2 Complete](PHASE_2_COMPLETE.md) - Directory creation summary

---

**Note**: This documentation uses **relative time periods** instead of absolute dates for consistency across timezones and easier maintenance. Documents automatically age into appropriate folders based on their creation date.

**Last Major Reorganization**: 2025-11-11 (Phase 2 Complete)

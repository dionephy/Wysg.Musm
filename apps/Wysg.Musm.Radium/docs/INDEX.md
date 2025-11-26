# Documentation Index

**Last Updated**: 2025-11-25

This index provides a complete reference to all Radium documentation organized by category, component, and feature area.

---

## ?? Quick Navigation

- [By Category](#by-category) - Find docs by organizational category
- [By Feature Area](#by-feature-area) - Find docs by feature domain
- [By Component](#by-component) - Find docs by code component
- [By Date](#by-date) - Find docs by time period
- [By Type](#by-type) - Find docs by document type

---

## By Category

### ?? [Active Work](00-active/) (Last 90 Days)
Current development, specifications, and tasks
- [Spec-active.md](00-active/Spec-active.md) - Active feature specifications
- [Plan-active.md](00-active/Plan-active.md) - Recent implementation plans
- [Tasks.md](00-active/Tasks.md) - Active and pending tasks

### ? [Features](01-features/)
Feature specifications and implementations organized by domain

#### [UI Features](01-features/ui/)
- **[Editor](01-features/ui/editor/)** - Text editor enhancements (autofocus, selection, fonts)
- **[Navigation](01-features/ui/navigation/)** - Keyboard navigation and shortcuts
- **[Settings](01-features/ui/settings/)** - Settings window features
- **[Windows](01-features/ui/windows/)** - Window management features

#### [Automation Features](01-features/automation/)
- **[Procedures](01-features/automation/procedures/)** - Custom automation procedures
- **[Operations](01-features/automation/operations/)** - Automation operations (SetValue, Click, etc.)
- **[Bookmarks](01-features/automation/bookmarks/)** - UI element bookmarks

#### [Phrase Features](01-features/phrases/)
- **[Completion](01-features/phrases/completion/)** - Phrase completion system
- **[Highlighting](01-features/phrases/highlighting/)** - Phrase highlighting
- **[Expansion](01-features/phrases/expansion/)** - Snippet expansion

#### Other Features
- **[Reportify](01-features/reportify/)** - Report formatting system
- **[SNOMED](01-features/snomed/)** - SNOMED CT integration
- **[Previous Studies](01-features/previous-studies/)** - Previous study management
- **[Diff Viewer](01-features/diff-viewer/)** - Text difference visualization

### ?? [Bug Fixes](02-bugfixes/)
Bug fixes organized chronologically
- [2025-02](02-bugfixes/2025-02/) - February 2025 fixes (15+ documents)
- [2025-01](02-bugfixes/2025-01/) - January 2025 fixes (10+ documents)
- [2024](02-bugfixes/2024/) - 2024 fixes (archived)

### ?? [Performance](03-performance/)
Performance optimizations
- [UI Optimization](03-performance/ui-optimization/) - UI performance improvements
- [Caching](03-performance/caching/) - Caching strategies
- [Database](03-performance/database/) - Database optimizations

### ??? [Architecture](04-architecture/)
System architecture and design
- [Overview](04-architecture/overview/) - High-level architecture
- [ViewModels](04-architecture/viewmodels/) - ViewModel patterns
- [Services](04-architecture/services/) - Service layer
- [Refactorings](04-architecture/refactorings/) - Major refactorings

### ?? [API](05-api/)
API documentation
- [Integration](05-api/integration/) - API integration guides
- [Caching](05-api/caching/) - API caching
- [Quick Start](05-api/quickstart/) - API quick start guides

### ?? [Database](06-database/)
Database documentation
- [Schema](06-database/schema/) - Database schemas
- [Migrations](06-database/migrations/) - Schema migrations
- [Deployment](06-database/deployment/) - Deployment procedures

### ?? [Guides](07-guides/)
User and developer guides
- [User Guides](07-guides/user/) - End-user documentation
- [Developer Guides](07-guides/developer/) - Development guides
- [Diagnostics](07-guides/diagnostics/) - Troubleshooting guides

### ?? [Templates](08-templates/)
Document templates for consistency
- Feature specification template
- Implementation plan template
- Task tracking template
- Agent instructions template

### ?? [Summaries](09-summaries/)
Implementation summaries organized by date
- [2025-02](09-summaries/2025-02/) - February 2025 summaries
- [2025-01](09-summaries/2025-01/) - January 2025 summaries
- [2024](09-summaries/2024/) - 2024 summaries (archived)

### ?? [Archive](10-archive/)
Historical documentation
- [2025-Q1](10-archive/2025-Q1/) - First quarter 2025
- [2024-Q4](10-archive/2024/) - Fourth quarter 2024
- [Older](10-archive/older/) - Pre-2024 content

---

## By Feature Area

### ?? Editor & Text Processing
- Editor autofocus and focus management
- Text selection and replacement
- Font and styling (D2Coding font)
- Header/findings/conclusion editors
- Proofread visualization
- Diff viewer (word-level granularity)

?? **Key Documents:**
- [ENHANCEMENT_2025-11-11_EditorAutofocus.md](01-features/ui/editor/)
- [BUGFIX_2025-11-10_EditorSelectionReplacement.md](02-bugfixes/2025-02/)
- [FEATURE_2025-11-02_FindingsDiffVisualization.md](01-features/diff-viewer/)

### ?? Navigation & Shortcuts
- Alt+Arrow navigation (reorganized workflow)
- Copy to caret position (Alt+Left)
- Keyboard shortcut customization
- Navigation map and quick reference
- Shift modifier support for global hotkeys

?? **Key Documents:**
- [ENHANCEMENT_2025-11-11_AltArrowNavigationDirectionChanges.md](01-features/ui/navigation/)
- [QUICKREF_2025-11-11_AltArrowNavigationMap.md](01-features/ui/navigation/)
- [ENHANCEMENT_2025-11-11_AltLeftCopyToCaretPosition.md](01-features/ui/navigation/)

### ?? PACS Automation
- Web browser element picker
- UI element bookmarks and validation
- Conditional procedures
- SetValue/Click/Delay operations
- Admin privilege support for UI Spy
- Custom automation procedures

?? **Key Documents:**
- [ENHANCEMENT_2025-11-10_WebBrowserElementPicker.md](01-features/automation/bookmarks/)
- [BUGFIX_2025-11-10_WebBrowserElementPickerRobustness.md](02-bugfixes/2025-02/)
- [ENHANCEMENT_2025-11-10_ConditionalSendReportProcedure.md](01-features/automation/procedures/)
- [BUGFIX_2025-10-21_UISpyAdminPrivilegeSupport.md](02-bugfixes/2025-01/)

### ?? Phrase System
- Phrase completion window
- Phrase highlighting and colorization
- Global phrase management
- Snippet expansion (Mode 1, Mode 2)
- SNOMED phrase mapping
- Alphabetical sorting and pagination

?? **Key Documents:**
- [BUGFIX_2025-11-11_CompletionWindowBlankItems.md](02-bugfixes/2025-02/)
- [BUGFIX_2025-11-11_CompletionWindowPhraseFiltering.md](02-bugfixes/2025-02/)
- [ENHANCEMENT_2025-11-02_AlphabeticalSortingGlobalPhrases.md](01-features/phrases/)
- [PERFORMANCE_2025-11-02_PhraseTabsOptimization.md](03-performance/ui-optimization/)

### ?? Hotkeys & Snippets
- Hotkey management (in-place editing)
- Snippet management (in-place editing)
- Trigger customization
- Expansion/template editing
- Delete and update workflows

?? **Key Documents:**
- [ENHANCEMENT_2025-11-11_HotkeysSnippetsEditFeature.md](01-features/ui/settings/)
- [IMPLEMENTATION_SUMMARY_2025-11-11_HotkeysSnippetsEditFeature.md](09-summaries/2025-02/)

### ?? Reportify System
- Report formatting and numbering
- Arrow/bullet continuation support
- Paragraph spacing control
- Unicode dash normalization
- Line mode processing
- Toggle automation

?? **Key Documents:**
- [ENHANCEMENT_2025-11-02_ConsiderArrowBulletContinuation.md](01-features/reportify/)
- [BUGFIX_2025-11-02_COVID19-Hyphen.md](02-bugfixes/2025-02/)
- [REPORTIFIED_TOGGLE_AUTOMATION_FIX_2025_01_21.md](01-features/reportify/)

### ?? SNOMED CT Integration
- SNOMED browser
- Phrase-SNOMED mapping
- Semantic tag coloring
- FSN (Fully Specified Name) display
- Batch mapping loading
- Snowstorm API integration

?? **Key Documents:**
- [SNOMED_INTEGRATION_COMPLETE.md](01-features/snomed/)
- [SNOMED_BROWSER_FEATURE_SUMMARY.md](01-features/snomed/)
- [FIX_2025-02-05_PhraseSnomedLinkWindow_SearchAndFSN.md](02-bugfixes/2025-02/)

### ?? Previous Studies
- Previous study loading and display
- Report selector (ComboBox)
- Split range management
- JSON persistence
- Auto-save control
- Comparison field management

?? **Key Documents:**
- [ENHANCEMENT_2025-11-02_PreviousReportSelector.md](01-features/previous-studies/)
- [FIX_2025-02-08_DisableAutoSaveOnPreviousTabSwitch.md](02-bugfixes/2025-02/)
- [FIX_2025-02-08_PreviousStudySplitRangesNotLoading.md](02-bugfixes/2025-02/)

### ?? Settings & Configuration
- Settings window organization
- Keyboard shortcuts configuration
- Phrase/snippet settings
- Reportify settings
- Local settings persistence
- Encrypted settings handling

?? **Key Documents:**
- [BUGFIX_2025-11-11_EncryptedSettingsCorruption.md](02-bugfixes/2025-02/)
- [Services/RadiumLocalSettings.cs](../Services/)

### ?? Window Management
- Always on top feature
- Window title autofocus
- Dark theme tab controls
- Collapsible JSON panels
- Status bar and actions

?? **Key Documents:**
- [ENHANCEMENT_2025-11-11_AlwaysOnTopFeature.md](01-features/ui/windows/)
- [ENHANCEMENT_2025-11-11_WindowTitleAutofocus.md](01-features/ui/windows/)
- [ENHANCEMENT_2025-11-09_DarkThemeTabControl.md](01-features/ui/windows/)
- [ENHANCEMENT_2025-11-02_CollapsibleJsonPanels.md](01-features/ui/windows/)

### ?? Performance Optimizations
- Phrase tabs optimization (90%+ faster)
- UI bookmark fast-fail heuristic
- AddPreviousStudy retry reduction
- Caching improvements
- Debug log cleanup
- Memory optimization

?? **Key Documents:**
- [PERFORMANCE_2025-11-02_PhraseTabsOptimization.md](03-performance/ui-optimization/)
- [PERFORMANCE_2025-11-02_UiBookmarksFastFail.md](03-performance/database/)
- [PERFORMANCE_2025-11-02_AddPreviousStudyRetryReduction.md](03-performance/database/)
- [MAINTENANCE_2025-11-02_DebugLogCleanup.md](03-performance/ui-optimization/)

---

## By Component

### ViewModels
| ViewModel | Key Documents |
|-----------|---------------|
| MainViewModel | CRITICAL_FIX_MAINVIEWMODEL_DI.md |
| SettingsViewModel | ENHANCEMENT_2025-11-11_HotkeysSnippetsEditFeature.md |
| HotkeysViewModel | ENHANCEMENT_2025-11-11_HotkeysSnippetsEditFeature.md |
| SnippetsViewModel | ENHANCEMENT_2025-11-11_HotkeysSnippetsEditFeature.md |
| GlobalPhrasesViewModel | ENHANCEMENT_2025-11-02_AlphabeticalSortingGlobalPhrases.md |
| SplashLoginViewModel | BUGFIX_2025-11-11_GoogleLoginConnectionStringError.md |

### Services
| Service | Key Documents |
|---------|---------------|
| EditorAutofocusService | ENHANCEMENT_2025-11-11_EditorAutofocus.md |
| RadiumLocalSettings | BUGFIX_2025-11-11_EncryptedSettingsCorruption.md |
| AzureSqlPhraseService | BUGFIX_2025-11-11_CompletionWindowPhraseFiltering.md |
| ApiPhraseServiceAdapter | API_INTEGRATION.md |

### Controls
| Control | Key Documents |
|---------|---------------|
| EditorControl | BUGFIX_2025-11-10_EditorSelectionReplacement.md |
| ReportInputsAndJsonPanel | ENHANCEMENT_2025-11-11_AltArrowNavigationDirectionChanges.md |
| CurrentReportEditorPanel | ENHANCEMENT_2025-11-09_MoveEditButtonsToCurrentReportPanel.md |
| StatusActionsBar | ENHANCEMENT_2025-11-11_AlwaysOnTopFeature.md |
| CenterEditingArea | (multiple) |

### Windows
| Window | Key Documents |
|--------|---------------|
| MainWindow | ENHANCEMENT_2025-11-11_WindowTitleAutofocus.md |
| SettingsWindow | ENHANCEMENT_2025-11-11_HotkeysSnippetsEditFeature.md |
| AutomationWindow | ENHANCEMENT_2025-11-10_WebBrowserElementPicker.md |

---

## By Date

### February 2025

#### Week of 2025-11-11 (15+ documents)
- **Completion & Editor**: Blank items fix, phrase filtering, autofocus
- **Navigation**: Alt+Arrow reorganization, Alt+Left copy to caret
- **Settings**: Hotkeys/snippets in-place editing
- **Windows**: Always on top, window title autofocus
- **Critical Fixes**: Key hook conflicts, bookmark caching, child element focus
- **Bug Fixes**: Google login, encrypted settings corruption

#### Week of 2025-11-10 (10+ documents)
- **Automation**: Web browser element picker, robustness improvements
- **Operations**: SetValue web operation, conditional send report
- **Editor**: Selection replacement fix
- **UI**: Comparison field loading, copy study remark toggle
- **Removal**: Chief complaint/patient history proofread fields

#### Week of 2025-11-09 (20+ documents)
- **UI**: Dark theme tab control, remove JSON toggle, move edit buttons
- **Automation**: Split commands, AND operation, simulate operations
- **Fixes**: Alt key system menu bypass, IsMatch argument type forcing
- **Previous Studies**: Comparison update, patient validation, modality info

#### Week of 2025-02-08 (8+ documents)
- **Previous Studies**: Split ranges loading, proofread persistence
- **Critical Fixes**: Report selection, JSON updates, auto-save control
- **Performance**: Save button optimization

#### Week of 2025-11-02 (25+ documents)
- **Features**: Findings diff visualization, word-level diff granularity
- **Performance**: Phrase tabs optimization, AddPreviousStudy optimizations
- **Enhancement**: Collapsible JSON panels, previous report selector
- **Fixes**: Unicode dash normalization, previous report JSON loading
- **Maintenance**: Debug log cleanup

### January 2025

#### Week of 2025-01-30 (12+ documents)
- **Completion**: Filter trigger text only, hotkey priority
- **Enhancement**: Abort confirmation dialogs, header proofread visualization
- **Editor**: D2Coding font, orientation-aware navigation
- **Automation**: Save preorder button

#### Week of 2025-10-23 (10+ documents)
- **Critical Fixes**: Reportify saving wrong values, threading exception
- **Enhancement**: Unlock study toggle all, auto-refresh study techniques
- **Fixes**: JSON key names, paragraph blank lines, reportify numbering

#### Week of 2025-10-21 (5+ documents)
- **Critical Fix**: UI Spy admin privilege support
- **Enhancement**: Reportified toggle automation

### 2024
- **Q4**: 50+ documents (archived in 10-archive/2024/)

---

## By Type

### Document Type Distribution

| Type | Count | Description |
|------|-------|-------------|
| **ENHANCEMENT** | ~80 | Feature improvements and additions |
| **BUGFIX** | ~30 | Bug fixes |
| **FIX** | ~40 | General fixes |
| **FEATURE** | ~15 | New feature specifications |
| **CRITICAL_FIX** | ~10 | Critical issues requiring immediate attention |
| **PERFORMANCE** | ~15 | Performance optimizations |
| **SUMMARY** | ~25 | Implementation summaries |
| **REMOVAL** | ~5 | Removed features/components |
| **COMPLETE** | ~10 | Completed feature documentation |
| **QUICKREF** | ~5 | Quick reference guides |
| **Other** | ~156 | Architecture, guides, templates, etc. |

### By Priority

#### ?? Critical (Must Read)
- CRITICAL_FIX_* documents (security, data loss, major bugs)
- SNOMED_INTEGRATION_COMPLETE.md
- QUICKSTART_DEVELOPMENT.md
- ARCHITECTURE_CLARIFICATION_2025-11-11.md

#### ?? Important (Should Read)
- BUGFIX_* documents (recent bug fixes)
- ENHANCEMENT_* documents (new features)
- PERFORMANCE_* documents (optimizations)

#### ?? Reference (As Needed)
- SUMMARY_* documents (implementation details)
- QUICKREF_* documents (quick references)
- IMPLEMENTATION_SUMMARY_* documents

---

## Search Strategies

### Finding Documents

#### By Keyword
Use Ctrl+Shift+F in VS Code to search:
- `SNOMED` - SNOMED-related documentation
- `performance` - Performance optimizations
- `CRITICAL` - Critical fixes
- `2025-11-11` - Documents from specific date
- `autofocus` - Editor autofocus features
- `completion` - Phrase completion features

#### By Requirement ID
- Search `FR-XXX` to find requirement references
- Search `BUG-XXX` to find bug references

#### By Component
- Search component name: `MainViewModel`, `EditorControl`, `RadiumLocalSettings`
- Search namespace: `Wysg.Musm.Radium.ViewModels`

#### By Feature Area
- Use category folders: `01-features/ui/`, `01-features/automation/`
- Use feature-specific folders: `phrases/`, `snomed/`, `reportify/`

### Document Naming Patterns

All dated documents follow this format:
```
[TYPE]_YYYY-MM-DD_DescriptiveName.md
```

Examples:
- `FEATURE_2025-11-02_FindingsDiffVisualization.md`
- `BUGFIX_2025-11-11_CompletionWindowBlankItems.md`
- `ENHANCEMENT_2025-11-11_EditorAutofocus.md`
- `PERFORMANCE_2025-11-02_PhraseTabsOptimization.md`
- `CRITICAL_FIX_2025-11-11_KeyConsumptionAndDelivery.md`

---

## Related Resources

### External Documentation
- [AvalonEdit Documentation](https://github.com/icsharpcode/AvalonEdit)
- [WPF Documentation](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)
- [SNOMED CT Documentation](https://www.snomed.org/)
- [Azure SQL Documentation](https://docs.microsoft.com/en-us/azure/azure-sql/)

### Internal Resources
- [Main README](README.md) - Documentation overview
- [Quick Start](QUICKSTART_DEVELOPMENT.md) - Get started in 5 minutes
- [Reorganization Plan](REORGANIZATION_PLAN.md) - Documentation structure details

---

## Statistics

### Document Counts by Category
- Active Work: 3
- Features: 100+
- Bug Fixes: 70+
- Performance: 15+
- Architecture: 20+
- API: 5+
- Database: 10+
- Guides: 15+
- Templates: 4
- Summaries: 25+
- Archive: 100+

### Recent Activity (Last 30 Days)
- New documents: 35+
- Updated documents: 50+
- Archived documents: 10+

### Coverage
- **Feature Coverage**: 100% of implemented features documented
- **Bug Fix Coverage**: 100% of fixed bugs documented
- **API Coverage**: 80% of public APIs documented
- **Architecture Coverage**: 75% of major components documented

---

**Last Updated**: 2025-11-25  
**Total Documents**: 391  
**Organization Version**: 2.0  
**Maintained By**: Documentation Team

---

## Quick Tips

?? **Can't find something?** Use Ctrl+Shift+F to search all docs  
?? **Looking for recent changes?** Check [Active Work](00-active/)  
?? **Need a quick start?** See [QUICKSTART_DEVELOPMENT.md](07-guides/developer/QUICKSTART_DEVELOPMENT.md)  
?? **Want to contribute?** Use [templates](08-templates/) for consistency  
?? **Found broken links?** Report via GitHub issue

---

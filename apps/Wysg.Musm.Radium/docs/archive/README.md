# Documentation Archives

This directory contains archived documentation organized by quarter and feature domain.

## Archive Organization

Archives are organized by **year/quarter** and then by **feature domain** to make historical changes easy to find.

### Current Active Documentation (Last 90 Days)
- **[Spec.md](../Spec.md)** - Feature specifications (2025-10-01 onward)
- **[Plan.md](../Plan.md)** - Implementation plans (2025-10-01 onward)
- **[Tasks.md](../Tasks.md)** - Task tracking (active and recent)

### 2025 Q1 Archives (January - March 2025)

#### Foreign Text Sync & Caret Management
- **[Spec-2025-Q1-foreign-text-sync.md](2025-Q1/Spec-2025-Q1-foreign-text-sync.md)** - FR-1100..FR-1136
- **[Plan-2025-Q1-foreign-text-sync.md](2025-Q1/Plan-2025-Q1-foreign-text-sync.md)** - Foreign textbox sync, merge, caret preservation

#### Phrase-SNOMED Mapping
- **[Spec-2025-Q1-phrase-snomed.md](2025-Q1/Spec-2025-Q1-phrase-snomed.md)** - FR-900..FR-915
- **[Plan-2025-Q1-phrase-snomed.md](2025-Q1/Plan-2025-Q1-phrase-snomed.md)** - Database schema, Snowstorm API integration

#### Editor Enhancements
- **[Spec-2025-Q1-editor.md](2025-Q1/Spec-2025-Q1-editor.md)** - FR-700..FR-709 (Phrase highlighting)
- **[Plan-2025-Q1-editor.md](2025-Q1/Plan-2025-Q1-editor.md)** - Header fields, snippet logic, phrase highlighting

### 2024 Archives

#### Q4 (October - December 2024)
- **[Spec-2024-Q4.md](2024/Spec-2024-Q4.md)** - Features through December 2024
- **[Plan-2024-Q4.md](2024/Plan-2024-Q4.md)** - Implementation plans through December 2024
- **[Tasks-2024-Q4.md](2024/Tasks-2024-Q4.md)** - Completed tasks through December 2024

## Feature Domains Index

### Study Technique Management
- **Q1 2025**: Database schema design (FR-453..FR-468)
- **Q4 2024**: Edit window UX, grouped display, inline add
- **Location**: [Spec-2025-Q1-technique.md](2025-Q1/Spec-2025-Q1-technique.md)

### PACS Automation & Integration
- **Q4 2024**: Multi-PACS tenancy, automation modules, global hotkeys
- **Q4 2024**: UI Spy bookmarks, custom procedures, mouse clicks
- **Location**: [Spec-2024-Q4.md](2024/Spec-2024-Q4.md) + [Plan-2024-Q4.md](2024/Plan-2024-Q4.md)

### UI/UX Improvements
- **Q1 2025**: Dark scrollbars, DataGrid text visibility, button enablement
- **Q4 2024**: Window placement persistence, ComboBox display fixes
- **Location**: [Plan-2025-Q1-ui-ux.md](2025-Q1/Plan-2025-Q1-ui-ux.md)

### Previous Report Features
- **Q4 2024**: Field mapping changes, split view, reusable panel control
- **Location**: [Plan-2024-Q4.md](2024/Plan-2024-Q4.md)

### Reportify & Text Processing
- **Q4 2024**: Clarifications, toggle removal, preserve known tokens
- **Location**: [Plan-2024-Q4.md](2024/Plan-2024-Q4.md)

## Finding Historical Information

### By Date
1. Identify the quarter (Q1 = Jan-Mar, Q2 = Apr-Jun, Q3 = Jul-Sep, Q4 = Oct-Dec)
2. Open the appropriate year/quarter directory
3. Search by feature domain or use Ctrl+F to find specific terms

### By Feature
1. Use the Feature Domains Index above to identify the relevant archive
2. Open the linked archive file
3. Search for your feature requirements (FR-XXX) or task numbers (TXXX)

### By Requirement ID
- FR-453..FR-468: Technique database schema �� [Spec-2025-Q1-technique.md](2025-Q1/Spec-2025-Q1-technique.md)
- FR-500..FR-515: Technique autofill & refresh �� [Spec-2024-Q4.md](2024/Spec-2024-Q4.md)
- FR-540..FR-547: Automation modules �� [Spec-2024-Q4.md](2024/Spec-2024-Q4.md)
- FR-600..FR-609: Multi-PACS tenancy �� [Spec-2024-Q4.md](2024/Spec-2024-Q4.md)
- FR-700..FR-709: Phrase highlighting �� [Spec-2025-Q1-editor.md](2025-Q1/Spec-2025-Q1-editor.md)
- FR-900..FR-915: Phrase-SNOMED mapping �� [Spec-2025-Q1-phrase-snomed.md](2025-Q1/Spec-2025-Q1-phrase-snomed.md)
- FR-1100..FR-1136: Foreign text sync �� [Spec-2025-Q1-foreign-text-sync.md](2025-Q1/Spec-2025-Q1-foreign-text-sync.md)

## Archive Maintenance Guidelines

### When to Archive
- Feature complete and stable for 90+ days
- No active work or pending tasks
- Documented in at least one release

### How to Archive
1. **Identify entries** older than 90 days in main Spec/Plan/Tasks files
2. **Group by feature domain** for related functionality
3. **Create archive file** in appropriate year/quarter directory
4. **Copy entries** with full cumulative detail (don't summarize)
5. **Update main files** with cross-reference to archive
6. **Update this README** with new archive links

### Archive File Naming
- `Spec-YYYY-QX-feature-domain.md` for specifications
- `Plan-YYYY-QX-feature-domain.md` for implementation plans
- `Tasks-YYYY-QX-feature-domain.md` for completed tasks
- Use lowercase-with-hyphens for feature domain names

## Cross-References

Archives maintain full cumulative detail and cross-reference each other:
- Spec archives link to Plan archives for implementation details
- Plan archives link to Spec archives for requirements
- Task archives link to both for context
- All archives link back to active docs for recent changes

## Migration History

| Date | Action | Files Moved | Reason |
|------|--------|-------------|--------|
| 2025-10-19 | Initial archive creation | (pending) | Spec.md (979 lines), Plan.md (1317 lines), Tasks.md (665 lines) too large |

---

*Last Updated: 2025-11-25*

# Radium Documentation

**Last Updated**: 2025-01-20

---

## Quick Start

### For Current Work (Last 90 Days)
- **[Spec-active.md](Spec-active.md)** - Active feature specifications
- **[Plan-active.md](Plan-active.md)** - Recent implementation plans  
- **[Tasks.md](Tasks.md)** - Active and pending tasks

### Recent Major Features (2025-01-20)
- **[SNOMED_INTEGRATION_COMPLETE.md](SNOMED_INTEGRATION_COMPLETE.md)** - Complete SNOMED CT integration status
- **[SNOMED_BROWSER_FEATURE_SUMMARY.md](SNOMED_BROWSER_FEATURE_SUMMARY.md)** - SNOMED Browser feature specification

### For Historical Reference
- **[archive/](archive/)** - Organized by quarter and feature domain
- **[archive/README.md](archive/README.md)** - Complete archive index

---

## Document Structure

### Active Documents
Files prefixed with `-active` contain work from the last 90 days:
- Small, focused, easy to navigate
- Only current and recent features
- Cross-references to archives for history

### Feature Documentation
Complete feature specifications and implementation guides:
- **SNOMED Integration** - Full SNOMED CT terminology integration
  - `SNOMED_INTEGRATION_COMPLETE.md` - Implementation status and testing
  - `SNOMED_BROWSER_FEATURE_SUMMARY.md` - Feature specification and architecture
  - `SNOMED_CSHARP_INTEGRATION_STEPS.md` - Original integration steps

### Archives
Located in `archive/` directory:
- Organized by year/quarter (e.g., `2024/`, `2025-Q1/`)
- Grouped by feature domain (e.g., `foreign-text-sync`, `pacs-automation`)
- Maintain full cumulative detail
- Never deleted, only updated with corrections

---

## Finding Information

### By Recency
| Time Period | Location |
|-------------|----------|
| Last 7 days | `SNOMED_INTEGRATION_COMPLETE.md`, `SNOMED_BROWSER_FEATURE_SUMMARY.md` |
| Last 90 days | `Spec-active.md`, `Plan-active.md` |
| 2025 Q1 (older) | `archive/2025-Q1/` |
| 2024 Q4 | `archive/2024/` |

### By Feature Domain
| Domain | Documentation Location |
|--------|------------------------|
| **SNOMED CT Integration** | `SNOMED_INTEGRATION_COMPLETE.md`, `SNOMED_BROWSER_FEATURE_SUMMARY.md` |
| Foreign Text Sync | `archive/2025-Q1/Spec-2025-Q1-foreign-text-sync.md` |
| PACS Automation | `archive/2024/Spec-2024-Q4.md` |
| Study Techniques | `archive/2024/Spec-2024-Q4.md` |
| Multi-PACS Tenancy | `archive/2024/Plan-2024-Q4.md` |
| Phrase-SNOMED Mapping | `archive/2025-Q1/Spec-2025-Q1-phrase-snomed.md` |

### By Requirement ID
Use workspace search (Ctrl+Shift+F) to find specific FR-XXX requirements across all docs.

---

## Other Documentation

### Feature-Specific Guides
- `phrase-highlighting-usage.md` - How to use phrase highlighting feature
- `snippet_logic.md` - Snippet expansion rules
- `data_flow.md` - Data flow diagrams

### Architecture & Design
- `spec_editor.md` - Editor component specification
- `snomed-semantic-tag-debugging.md` - SNOMED integration debugging

### SNOMED CT Integration
- `SNOMED_INTEGRATION_COMPLETE.md` - Complete implementation status
- `SNOMED_BROWSER_FEATURE_SUMMARY.md` - Browser feature specification
- `SNOMED_CSHARP_INTEGRATION_STEPS.md` - Step-by-step integration guide

### Database Documentation
- `db/README_SCHEMA_REFERENCE.md` - Database schema reference
- `db/DEPLOYMENT_GUIDE.md` - Database deployment procedures
- `db/db_central_azure_migration_20251019.sql` - SNOMED schema migration

### Historical Summaries
- `CHANGELOG_*.md` - Feature-specific change logs
- `*_SUMMARY.md` - Implementation summaries for major refactorings

### Templates
- `spec-template.md` - Template for new feature specifications
- `plan-template.md` - Template for implementation plans
- `tasks-template.md` - Template for task tracking
- `agent-file-template.md` - Template for AI agent instructions

---

## Recent Updates (2025-01-20)

### SNOMED CT Browser - Complete Implementation ?

**What's New:**
1. **Full SNOMED Browser UI** - Browse 7 semantic tag domains with pagination
2. **Smart Phrase Management** - Add terms as global phrases with automatic concept mapping
3. **Existence Checking** - Prevents duplicate phrase+concept mappings across all phrases (not just first 100!)
4. **Visual Feedback** - Dark red concept panels for existing mappings
5. **Delete Functionality** - Soft delete global phrases with confirmation
6. **Edit Functionality** - Modify phrase text while preserving SNOMED mappings

**Key Files Added:**
- `Views/SnomedBrowserWindow.xaml` - Browser UI
- `ViewModels/SnomedBrowserViewModel.cs` - Browser logic
- `Services/SnowstormClient.cs` - ECL query support + dual-endpoint strategy
- `SNOMED_BROWSER_FEATURE_SUMMARY.md` - Complete feature documentation

**Performance Improvements:**
- Removed hardcoded `LIMIT 100` from global phrase loading
- Now loads ALL global phrases for 100% accurate existence checks
- Typical memory footprint: ~300 KB per browser session

See **[SNOMED_INTEGRATION_COMPLETE.md](SNOMED_INTEGRATION_COMPLETE.md)** for full details.

---

## Migration Notice

We recently reorganized documentation to improve maintainability:
- **Old files** (Spec.md, Plan.md) are being phased out
- **New structure** uses active docs + archives
- **Transition period**: Until 2025-02-18

See [MIGRATION.md](MIGRATION.md) for details.

---

## Contributing

### Adding New Features
1. Add specification to `Spec-active.md`
2. Add implementation plan to `Plan-active.md`
3. Add tasks to `Tasks.md`
4. Follow cumulative format (append, don't delete)
5. Create feature summary document (see `SNOMED_BROWSER_FEATURE_SUMMARY.md` as example)

### Updating Documentation
- **Recent features**: Update active docs directly
- **Archived features**: Update archive file with correction note
- **Cross-references**: Maintain links between related features
- **Major features**: Create dedicated feature summary document

### Archival Process
Every 90 days:
1. Identify entries older than 90 days in active docs
2. Group by feature domain
3. Move to appropriate archive directory
4. Update `archive/README.md` index
5. Add cross-references in active docs

---

## Documentation Principles

### Cumulative Format
- **Never delete** historical information
- **Always append** new information
- **Mark corrections** with date and reason

### Cross-References
- Link requirements to implementation plans
- Link plans to task lists
- Link archives to related archives
- Link active docs to archives for context

### Clarity
- Use consistent terminology
- Include examples where helpful
- Provide context for decisions
- Document alternatives considered

### Feature Documentation
For major features (like SNOMED Browser):
- Create dedicated feature summary document
- Include user stories and acceptance criteria
- Document architecture with diagrams
- Provide testing guidelines
- List known limitations and future enhancements

---

## File Size Targets

| File Type | Target Size | Action Threshold |
|-----------|-------------|------------------|
| Active Spec | < 500 lines | Archive at 90 days |
| Active Plan | < 500 lines | Archive at 90 days |
| Feature Summary | Any size | Split by sub-feature if > 2000 lines |
| Archive File | Any size | Split by feature if > 2000 lines |

---

## Need Help?

### Can't Find Something?
1. Check `archive/README.md` for complete index
2. Use workspace search (Ctrl+Shift+F)
3. Check recent updates section above
4. Ask team members

### Documentation Issues?
- Broken links? Report immediately
- Missing information? Check archives first
- Unclear sections? Propose improvements
- New major feature? Create feature summary document

---

*For complete archive index, see [archive/README.md](archive/README.md)*  
*For SNOMED integration status, see [SNOMED_INTEGRATION_COMPLETE.md](SNOMED_INTEGRATION_COMPLETE.md)*

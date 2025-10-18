# Radium Documentation

**Last Updated**: 2025-01-19

---

## Quick Start

### For Current Work (Last 90 Days)
- **[Spec-active.md](Spec-active.md)** - Active feature specifications
- **[Plan-active.md](Plan-active.md)** - Recent implementation plans  
- **[Tasks.md](Tasks.md)** - Active and pending tasks

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
| Last 90 days | `Spec-active.md`, `Plan-active.md` |
| 2025 Q1 (older) | `archive/2025-Q1/` |
| 2024 Q4 | `archive/2024/` |

### By Feature Domain
| Domain | Archive Location |
|--------|------------------|
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

### Historical Summaries
- `CHANGELOG_*.md` - Feature-specific change logs
- `*_SUMMARY.md` - Implementation summaries for major refactorings

### Templates
- `spec-template.md` - Template for new feature specifications
- `plan-template.md` - Template for implementation plans
- `tasks-template.md` - Template for task tracking
- `agent-file-template.md` - Template for AI agent instructions

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

### Updating Documentation
- **Recent features**: Update active docs directly
- **Archived features**: Update archive file with correction note
- **Cross-references**: Maintain links between related features

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

---

## File Size Targets

| File Type | Target Size | Action Threshold |
|-----------|-------------|------------------|
| Active Spec | < 500 lines | Archive at 90 days |
| Active Plan | < 500 lines | Archive at 90 days |
| Archive File | Any size | Split by feature if > 2000 lines |

---

## Need Help?

### Can't Find Something?
1. Check `archive/README.md` for complete index
2. Use workspace search (Ctrl+Shift+F)
3. Ask team members

### Documentation Issues?
- Broken links? Report immediately
- Missing information? Check archives first
- Unclear sections? Propose improvements

---

*For complete archive index, see [archive/README.md](archive/README.md)*

# Documentation Migration Guide

**Date**: 2025-10-19  
**Purpose**: Guide for transitioning from monolithic documentation to archive-based structure

---

## Overview

We've reorganized documentation to improve maintainability and reduce file sizes:

### Before (Monolithic)
```
docs/
������ Spec.md (979 lines, 63 KB) �� Too large
������ Plan.md (1,317 lines, 88 KB) �� Too large
������ Tasks.md (665 lines, 62 KB) �� Too large
```

### After (Archive-Based)
```
docs/
������ Spec-active.md (Current, ~200 lines)
������ Plan-active.md (Current, ~200 lines)
������ Tasks.md (Active only, ~200 lines)
������ archive/
��   ������ README.md (Index of all archives)
��   ������ 2024/
��   ��   ������ Spec-2024-Q4.md
��   ��   ������ Plan-2024-Q4.md
��   ��   ������ Tasks-2024-Q4.md
��   ������ 2025-Q1/
��       ������ Spec-2025-Q1-foreign-text-sync.md
��       ������ Plan-2025-Q1-foreign-text-sync.md
��       ������ ... (more feature-specific archives)
������ MIGRATION.md (this file)
```

---

## Using the New Structure

### Finding Current Information (Last 90 Days)
1. Open `Spec-active.md` for active feature specifications
2. Open `Plan-active.md` for recent implementation plans
3. Open `Tasks.md` for active/pending tasks

### Finding Historical Information
1. Open `archive/README.md` for complete index
2. Use Feature Domains Index to find your topic
3. Navigate to specific archive file

### Example Searches

**"I need the Foreign Text Sync requirements"**
�� FR-1100..FR-1136 in `archive/2025-Q1/Spec-2025-Q1-foreign-text-sync.md`

**"How was Study Technique management implemented?"**
�� `archive/2024/Plan-2024-Q4.md` (search for "Study Technique")

**"What tasks were completed for PACS automation?"**
�� `archive/2024/Tasks-2024-Q4.md` (search for "T540..T680")

---

## For Developers

### Reading Documentation
- **Start with active docs** (`Spec-active.md`, `Plan-active.md`) for recent work
- **Check archive index** if you need historical context
- **Follow cross-references** between active and archived docs

### Writing New Documentation
1. **Add to active docs** (Spec-active.md, Plan-active.md, Tasks.md)
2. **Follow cumulative format** (don't delete, only append)
3. **Archive quarterly** when entries are 90+ days old

### Referencing Requirements
- **Recent features**: Link directly to active docs
- **Historical features**: Link to archive location
- **Mixed**: Use both with clear delineation

---

## Migration Timeline

### Phase 1: Create Archive Structure ?
- [x] Create archive directories (2024, 2025-Q1)
- [x] Create archive README index
- [x] Create template archive files

### Phase 2: Extract Archives (In Progress)
- [x] Extract FR-1100..FR-1136 (Foreign Text Sync)
- [ ] Extract FR-900..FR-915 (Phrase-SNOMED Mapping)
- [ ] Extract 2024-Q4 entries (PACS, Techniques, etc.)
- [ ] Extract older 2025-Q1 entries

### Phase 3: Create Active Docs (In Progress)
- [x] Create Spec-active.md with last 90 days
- [x] Create Plan-active.md with last 90 days
- [ ] Trim Tasks.md to active only

### Phase 4: Update References
- [ ] Update README.md to reference new structure
- [ ] Update PR templates to reference active docs
- [ ] Update developer onboarding guide

### Phase 5: Deprecate Old Files
- [ ] Rename Spec.md �� Spec-old.md (keep for reference)
- [ ] Rename Plan.md �� Plan-old.md (keep for reference)
- [ ] Add deprecation notice to old files
- [ ] Remove after 30 days if no issues

---

## Transition Period (30 Days)

### During Transition
Both old and new structures coexist:
- **Old files** (Spec.md, Plan.md) marked with deprecation notice
- **New files** (Spec-active.md, Plan-active.md, archives) are canonical
- **New work** goes only in new files

### After Transition (Post Feb 18, 2025)
- Old files removed
- Spec-active.md renamed to Spec.md
- Plan-active.md renamed to Plan.md
- Archive structure becomes standard

---

## FAQ

### Q: Where should I add new feature requirements?
**A**: Add to `Spec-active.md` following the existing format.

### Q: What if I need to update an archived feature?
**A**: 
1. If it's a new requirement, add to `Spec-active.md` and reference the archive
2. If it's a correction, update the archive file and note the correction date
3. Never delete archived content, only append corrections

### Q: How often should documents be archived?
**A**: Quarterly review (every 90 days). Features 90+ days old move to archives.

### Q: Can I still search across all documentation?
**A**: Yes! Use your editor's workspace-wide search (Ctrl+Shift+F) to search both active and archived docs.

### Q: What if an archived feature gets reactivated?
**A**: Add a new section in `Spec-active.md` that references the archive for history and documents the new work.

### Q: Are archives read-only?
**A**: No, archives can be updated for corrections or clarifications. Just note the update date.

---

## Benefits of New Structure

### Reduced Cognitive Load
- Active docs contain only recent, relevant information
- No scrolling through 1000+ lines to find current work
- Clear separation between active and historical

### Better Organization
- Features grouped by domain in archives
- Easy to find related work
- Cross-references maintained

### Improved Performance
- Smaller files load faster in editors
- Faster search within active docs
- No merge conflicts in huge files

### Maintainability
- Easier to review recent changes
- Clear archival process
- Cumulative format preserved

---

## Need Help?

### Issues During Transition
- **Can't find something?** Check `archive/README.md` index first
- **Links broken?** Most cross-references preserved; report if you find issues
- **Unsure where to add?** Default to active docs, we'll archive later

### Feedback
If you have suggestions for improving the archive structure, please discuss with the team.

---

*Migration initiated: 2025-10-19*  
*Transition period: 30 days (until 2025-02-18)*  
*Status: Phase 2 in progress*

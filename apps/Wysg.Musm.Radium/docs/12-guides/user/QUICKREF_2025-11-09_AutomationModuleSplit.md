# Quick Reference: Automation Module Split and Related Studies ID (2025-11-09)

**Date**: 2025-11-25  
**Type**: Quick Reference  
**Category**: User Reference  
**Status**: ? Active

---

## Summary

This guide provides quick reference information for users. For detailed implementation information, see the related plan and specification documents.

---

# Quick Reference: Automation Module Split and Related Studies ID (2025-11-09)

## Summary
Two improvements to automation system:
1. ? Added "Get Selected Id From Related Studies List" to SpyWindow Custom Procedures
2. ? Split "UnlockStudy" module into "UnlockStudy" (patient/study) + "ToggleOff" (proofread/reportified)

## Module Behavior Changes

### UnlockStudy (Modified)
**Toggles OFF**: PatientLocked, StudyOpened  
**Preserves**: ProofreadMode, Reportified  
**Status**: "Study unlocked (patient/study toggles off)"

### ToggleOff (New)
**Toggles OFF**: ProofreadMode, Reportified  
**Preserves**: PatientLocked, StudyOpened  
**Status**: "Toggles off (proofread/reportified off)"

## Migration

**OLD sequence (all toggles off)**:
```
UnlockStudy
```

**NEW equivalent sequence**:
```
UnlockStudy
ToggleOff
```

## Quick Test
1. Settings → Automation → Verify "ToggleOff" in library
2. Create test sequence: "UnlockStudy" → Verify only patient/study toggles off
3. Create test sequence: "ToggleOff" → Verify only proofread/reportified toggles off
4. Create test sequence: "UnlockStudy, ToggleOff" → Verify all 4 toggles off

## Files Changed
- `SpyWindow.PacsMethodItems.xaml` - Added GetSelectedIdFromRelatedStudies
- `MainViewModel.Commands.Automation.cs` - Split module logic
- `SettingsViewModel.cs` - Added ToggleOff to library

## Build Status
? Build succeeded (checked 2025-11-09)


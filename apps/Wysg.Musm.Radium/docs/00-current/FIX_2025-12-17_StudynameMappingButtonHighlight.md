# Fix: Highlight Manage Studyname Button When Mapping Missing
**Date:** 2025-12-17  
**Status:** Complete

## Problem
Users could unknowingly work on a study whose studyname either does not exist in the mapping database or lacks LOINC part associations. The "Manage studyname" button offered no visual clue in these cases, so users often discovered the issue only after automation failed to find modality parts.

## Resolution
1. **MainViewModel flag**
   - Added `StudynameMappingNeedsAttention` flag that checks the current studyname against the LOINC repository whenever the "Current Study Studyname" global field changes or after the Manage Studynames window closes.
   - The check first verifies the studyname exists; if it does, it ensures at least one LOINC part mapping exists. Missing rows or empty mappings both trigger the warning.

2. **UI indicator**
   - The `StatusActionsBar` now highlights the "Manage studyname" button (background + bold text) and enables a tooltip whenever the flag is raised.
   - Default state uses a dark charcoal background so the control blends with the rest of the Radium chrome, while the warning state switches to a vivid red background/white text combination to make the issue obvious.

3. **Window-close refresh**
   - When the Studyname LOINC window closes, the MainViewModel re-runs the mapping check to reflect any updates made during that session.

## Impact
- Radiologists see an immediate cue that the current studyname still needs LOINC work before running automation.
- Tooltip text guides them toward the Manage Studyname dialog rather than guessing why automation might fail later.
- No behavioral change occurs when the studyname already has mappings; the button remains in its normal state.

## Verification
- Load a study with a fully mapped studyname ¡æ button stays neutral.
- Load a study with a new/unknown studyname ¡æ button highlights and tooltip appears.
- Add mappings in the Manage window, close it ¡æ highlight clears automatically.

# Summary: SendReportWithoutHeader Implementation

**Date:** 2025-11-10  
**Type:** Enhancement/Replacement  
**Impact:** Low (Method replacement)

## What Changed
Replaced "Send report retry" PACS method with "Send report without header" in SpyWindow Custom Procedures.

## Key Points
1. **Removed:** SendReportRetry method and dropdown item
2. **Added:** SendReportWithoutHeader method and dropdown item
3. **Location:** SpyWindow �� Custom Procedures �� PACS Methods
4. **Service:** PacsService.SendReportWithoutHeaderAsync()

## Why This Change
The SendReportRetry functionality was redundant with the comprehensive retry logic already built into the SendReport automation module. The new SendReportWithoutHeader method provides a more useful feature for PACS workflows that need to send reports without header information.

## User Impact
- Existing configurations using SendReportRetry will need to be updated to use the SendReport module's built-in retry logic or reconfigured to use SendReportWithoutHeader if appropriate
- New configurations can use SendReportWithoutHeader for header-less report sending

## Configuration Required
Users must configure the SendReportWithoutHeader custom procedure in SpyWindow per their PACS requirements. This typically involves:
1. Opening SpyWindow
2. Selecting "Send report without header" from PACS methods dropdown
3. Defining the custom procedure operations (e.g., SetFocus, SetValue, Invoke, etc.)
4. Testing with the Run button
5. Saving the procedure

## Related Documentation
- ENHANCEMENT_2025-11-10_SendReportWithoutHeader.md (detailed change log)
- Spec.md FR-1190 to FR-1198 (updated specifications)
- ENHANCEMENT_2025-11-09_SendReportRetryLogic.md (original retry logic implementation)

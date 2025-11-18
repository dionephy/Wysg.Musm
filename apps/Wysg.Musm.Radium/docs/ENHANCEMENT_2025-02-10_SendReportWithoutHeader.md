# Enhancement: Replace SendReportRetry with SendReportWithoutHeader

**Date:** 2025-02-10  
**Type:** Enhancement  
**Status:** Completed

## Overview
Removed the "Send report retry" PACS method item and replaced it with "Send report without header" to provide a cleaner alternative for sending reports without the header component.

## Changes Made

### 1. SpyWindow.PacsMethodItems.xaml
- **Removed:** `<ComboBoxItem Tag="SendReportRetry">Send report retry</ComboBoxItem>`
- **Added:** `<ComboBoxItem Tag="SendReportWithoutHeader">Send report without header</ComboBoxItem>`

### 2. PacsService.cs
- **Removed:** `SendReportRetryAsync()` method
- **Added:** `SendReportWithoutHeaderAsync()` method that executes the `SendReportWithoutHeader` custom procedure tag

## Rationale
The "Send report retry" item was redundant with the existing retry logic built into the SendReport automation module (FR-1280 to FR-1289). The new "Send report without header" method provides a cleaner way to send reports that don't include header information, which is a common requirement in certain PACS workflows.

## Usage
Users can configure the "Send report without header" custom procedure in SpyWindow to define the exact UI interaction sequence for sending reports without headers. This procedure is configured per-PACS profile and must be authored explicitly by the user.

## Related Features
- FR-1190 to FR-1198: Original InvokeSendReport and SendReportRetry implementation
- FR-1280 to FR-1289: SendReport module retry logic
- FR-1220 to FR-1226: ClearReport method

## Testing
- Build verification: No compilation errors
- Manual testing required: Verify SpyWindow Custom Procedures dropdown shows "Send report without header" instead of "Send report retry"

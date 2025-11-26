# Quick Reference: SendReportWithoutHeader

**Date**: 2025-11-25  
**Type**: Quick Reference  
**Category**: User Reference  
**Status**: ? Active

---

## Summary

This guide provides quick reference information for users. For detailed implementation information, see the related plan and specification documents.

---

# Quick Reference: SendReportWithoutHeader

## PACS Method
**Name:** Send report without header  
**Tag:** `SendReportWithoutHeader`  
**Service Method:** `PacsService.SendReportWithoutHeaderAsync()`

## Purpose
Provides a custom procedure entry point for sending reports without header information. Useful for PACS systems that require reports without header components or for specific workflow scenarios.

## Configuration Location
AutomationWindow ¡æ Custom Procedures ¡æ PACS Method: "Send report without header"

## Typical Procedure Steps
```
1. SetFocus (findings field)
2. SetValue (findings field, findings content)
3. SetFocus (conclusion field)
4. SetValue (conclusion field, conclusion content)
5. Invoke (send button)
6. IsVisible (validation check)
```

## Comparison with Other Send Methods

| Method | Purpose | Header Included | Use Case |
|--------|---------|----------------|----------|
| SendReport | Standard send with retry logic | Yes | Normal workflow with automatic retry |
| InvokeSendReport | Simple send invoke | Yes | Final step after successful validation |
| SendReportWithoutHeader | Send without header | No | PACS requiring header-less reports |

## Migration from SendReportRetry
If you were using SendReportRetry:
- **Option 1:** Use the built-in retry logic in the SendReport automation module (recommended)
- **Option 2:** Reconfigure to use SendReportWithoutHeader if your workflow requires header-less reports
- **Option 3:** Implement manual retry logic using IsVisible checks and conditional branching

## Example Usage Pattern
```
// Validation-based send without header
1. GetText (findings field) ¡æ var1
2. IsMatch (var1, "") ¡æ var2
3. [If var2="false"] SetValue (findings field, content)
4. SendReportWithoutHeader procedure
5. IsVisible (success indicator) ¡æ var3
6. IsMatch (var3, "true") ¡æ final result
```

## Notes
- Must be configured per-PACS profile
- No auto-seeded defaults provided
- Test thoroughly before deploying in production
- Success/failure determined by PACS state validation


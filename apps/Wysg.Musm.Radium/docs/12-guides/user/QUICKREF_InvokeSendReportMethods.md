# Quick Reference: New PACS Methods - InvokeSendReport and SendReportRetry

**Date**: 2025-11-25  
**Type**: Quick Reference  
**Category**: User Reference  
**Status**: ? Active

---

## Summary

This guide provides quick reference information for users. For detailed implementation information, see the related plan and specification documents.

---

# Quick Reference: New PACS Methods - InvokeSendReport and SendReportRetry

## What Was Added?
Two new PACS method items in UI Spy window's Custom Procedures:
1. **Invoke send report** (Tag: `InvokeSendReport`)
2. **Send report retry** (Tag: `SendReportRetry`)

## Where to Find Them?
**UI Spy Window** ¡æ **Custom Procedures** section ¡æ **PACS Method** dropdown ¡æ Look for new items at the bottom of the list

## Quick Configuration Steps

### Step 1: Open UI Spy
- From Radium main window: Tools menu ¡æ UI Spy
- Or call: `AutomationWindow.ShowInstance()`

### Step 2: Select Your PACS
- Top-left dropdown shows current PACS profile (e.g., "default_pacs", "infinitt", etc.)
- Configuration saved per PACS profile

### Step 3: Configure "Invoke send report"
1. Select "Invoke send report" from PACS Method dropdown
2. Click "Add" to create operation steps
3. Common steps:
   ```
   Operation: IsVisible     Arg1: ReportTextEditor (Element)    ¡æ Check if report is open
   Operation: ClickElement  Arg1: SendReportButton (Element)    ¡æ Click send button
   Operation: Delay         Arg1: 500 (Number)                   ¡æ Wait for dialog
   Operation: ClickElement  Arg1: ConfirmButton (Element)       ¡æ Confirm send
   ```
4. Click "Save" to persist
5. Click "Run" to test

### Step 4: Configure "Send report retry"
1. Select "Send report retry" from PACS Method dropdown
2. Click "Add" to create retry-specific steps
3. Common steps:
   ```
   Operation: IsVisible     Arg1: ErrorDialog (Element)          ¡æ Check for error
   Operation: ClickElement  Arg1: CloseErrorButton (Element)    ¡æ Close error dialog
   Operation: Delay         Arg1: 1000 (Number)                  ¡æ Wait longer
   Operation: ClickElement  Arg1: SendReportButton (Element)    ¡æ Retry send
   Operation: Delay         Arg1: 500 (Number)                   ¡æ Wait for response
   ```
4. Click "Save"
5. Click "Run" to test

## Example Usage in Code

### From Automation Sequence
```json
// automation.json for your PACS
{
  "SendReportSequence": "InvokeSendReport,SendReportRetry"
}
```

### From C# Code
```csharp
var pacs = new PacsService();

// Primary send attempt
await pacs.InvokeSendReportAsync();

// If failed, retry
await pacs.SendReportRetryAsync();
```

## Common Operations for Send Report Procedures

| Operation | Purpose | Arguments | Example |
|-----------|---------|-----------|---------|
| `IsVisible` | Check if element visible | Arg1: Element (bookmark) | Check report editor open |
| `ClickElement` | Click UI element | Arg1: Element (bookmark) | Click send button |
| `Invoke` | Invoke button/control | Arg1: Element (bookmark) | Invoke send action |
| `Delay` | Pause execution | Arg1: Number (milliseconds) | Wait 500ms for dialog |
| `GetText` | Read element text | Arg1: Element (bookmark) | Read status message |
| `SetFocus` | Focus on element | Arg1: Element (bookmark) | Focus on text field |

## Bookmarks You May Need to Create

### For Send Report
- **SendReportButton** - Main send button control
- **ReportTextEditor** - Report text field (for validation)
- **ConfirmButton** - Confirmation dialog button
- **SuccessMessage** - Success status indicator

### For Retry Scenario
- **ErrorDialog** - Error message dialog
- **CloseErrorButton** - Close/dismiss error button
- **RetryButton** - Explicit retry button (if different from send)
- **StatusText** - Status text for validation

## Tips and Best Practices

### ? Do:
- Test procedures with "Run" button before using in automation
- Add validation steps (IsVisible, GetText) to verify state
- Use adequate Delay operations for UI stabilization
- Save different configurations for different PACS systems
- Document your bookmark mappings

### ? Don't:
- Use hardcoded mouse coordinates (use bookmarks instead)
- Skip validation steps (causes silent failures)
- Use too short delays (causes race conditions)
- Copy procedures between PACS profiles (UI differs per system)
- Forget to test retry scenarios

## Troubleshooting

### Procedure doesn't execute
- Check PACS profile selected matches active PACS
- Verify procedure saved (check ui-procedures.json file)
- Check Debug output for error messages
- Re-capture bookmarks if UI changed

### Buttons not clicking
- Verify bookmark resolves (use "Resolve" button in UI Spy)
- Check element visible (use IsVisible operation first)
- Add Delay before click to allow UI stabilization
- Try "Invoke" operation instead of "ClickElement"

### Retry doesn't work
- Check error dialog bookmark valid
- Verify error state detection logic
- Add longer delays for error dialogs to appear
- Test retry scenario manually first

## File Locations

### Configuration Storage
Per-PACS procedures saved at:
```
%AppData%\Wysg.Musm\Radium\Pacs\{pacs_key}\ui-procedures.json
```

### Bookmark Storage
Per-PACS bookmarks saved at:
```
%AppData%\Wysg.Musm\Radium\Pacs\{pacs_key}\bookmarks.json
```

## Related Documentation
- Full details: `ENHANCEMENT_2025-11-09_InvokeSendReportMethods.md`
- Feature specs: `Spec.md` (FR-1190 through FR-1198)
- Summary: `SUMMARY_2025-11-09_InvokeSendReportMethods.md`

## Support
If you encounter issues:
1. Check Debug output in Visual Studio
2. Validate bookmarks resolve correctly
3. Test operations individually using "Set" button
4. Review existing SendReport procedure as reference
5. Consult docs/Spec.md for complete feature requirements


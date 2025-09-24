# Radium Implementation Plan (Cumulative)

## Completed (previous iterations)
- PP1: Tree uses ResolvePath for parity with crawl editor; chain to level 4 then expand children.
- PP2: Stable operation presets and editor switching.
- PP3: PACS method "Get selected ID from search results list" implemented.

## Current iteration goals
- R1: Studynames list and filtering ? DONE
- R2: LOINC Parts UI with preview ? DONE
- R3: Common parts list ? DONE
- R4: Equal-height rows; vertical-only scroll; no horizontal scroll ? DONE
- R5: Double-click reliability with code-behind ? DONE
- R6: UX polish: editable order in preview; add/remove ? DONE
- R7: Playbook suggestions grouped by loinc_number ? DONE
- R8: Ensure only one MainWindow is shown on startup ? DONE
- R9: Lower playbook threshold from 3 to 2 ? DONE (enforced in XAML converter and VM logic)
- R10: Add Custom Procedure operation GetValueFromSelection (Arg1 Element list, Arg2 header string) ? DONE
- R11: Preserve blank cells in Row Data & GetValueFromSelection alignment ? DONE
- R12: Preserve blank header columns (leading / internal) to stop value shifting ? DONE
- R13: Add ToDateTime operation (Var -> DateTime parse) ? DONE
- R14: Add multiple PACS selection getter methods (name, sex, birth date, age, studyname, study date time, radiologist, study remark, report date time for search & related lists) ? DONE
- R15: Add Studyname DB diagnostics (counts + connection metadata) ? DONE
- R16: Add Postgres first-chance exception sampler + repo error logging ? DONE
- R17: Refactor PhraseService to use settings-based connection (remove hardcoded) ? DONE
- R18: Playbook matches ListBox vertical scrollbar ? DONE
- R19: Double-click playbook match imports all parts (skip duplicates by PartNumber+SequenceOrder) ? DONE
- R20: Double-click playbook part adds that part if absent (PartNumber+SequenceOrder uniqueness) ? DONE
- R21: Fix PP1 layout so scrollbar shows (StackPanel¡æGrid re-layout) ? DONE
- R22: Fix PP2 null/iteration timing via async wait before bulk import ? DONE
- R23: Fix PP3 duplicate rule (allow same PartNumber different sequence) ? DONE
- R24: Remove Diagnostics UI and related VM code ? DONE
- R25: Ensure Save button enabled (depends on SelectedStudyname) and functional ? DONE
- R26: Add status textbox showing recent action messages ? DONE
- R27: Implement working Close button handler ? DONE

## Breakdown and status
- R18.1: Add ScrollViewer.VerticalScrollBarVisibility=Auto to PlaybookMatches ListBox ? DONE
- R19.1: Code-behind double-click handler loads parts (ensures selection) and bulk-adds missing MappingPreviewItems (pair uniqueness) ? DONE
- R20.1: Code-behind double-click handler for single PlaybookPart adds if not present (pair uniqueness) ? DONE
- R21.1: Replace right panel StackPanel with Grid to constrain ListBox height (scrollbar visible) ? DONE
- R22.1: Make handler async + short retry loop for PlaybookParts population ? DONE
- R23.1: Update duplicate checks to PartNumber + PartSequenceOrder ? DONE
- R24.1: Delete diagnostics button/textbox from XAML ? DONE
- R24.2: Remove DiagnosticsCommand, DiagnosticsInfo, and related Debug output from VM ? DONE
- R25.1: Confirm SaveCommand CanExecute bound to SelectedStudyname and UI button present ? DONE
- R26.1: Implement status message logic in VM and bind to StatusTextBox in UI ? DONE
- R27.1: Hook up Close button in UI to execute CloseCommand in VM ? DONE

## Next
- N1: Relax matching rule (>= instead of = count) if partial matches desired
- N2: Show LOINC code next to long_common_name
- N3: Add a button to import selected playbook into preview (double-click already does import)
- N4: Hook procedure runner to call service methods by tag and display results
- N5: JSON export of Row Data preserving blanks

## References
- App: App.xaml / App.xaml.cs
- View: Views/StudynameLoincWindow.xaml / .xaml.cs
- View: Views/SpyWindow.xaml / .xaml.cs (GetValueFromSelection + blank cell/header preservation)
- VM: ViewModels/StudynameLoincViewModel.cs
- Repo: Services/StudynameLoincRepository.cs

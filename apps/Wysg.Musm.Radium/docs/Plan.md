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

## Breakdown and status
- R8.1: Remove StartupUri from App.xaml and open MainWindow only after SplashLogin success ? DONE
- R9.1: Lower converter Minimum to 2 and VM threshold check to 2 ? DONE
- R10.1: XAML add operation item; preset arg types in OnProcOpChanged; implement extraction logic in ExecuteSingle + RunProcedure ? DONE
- R11.1: Modify GetRowCellValues to always add placeholders (no skip of blanks) ? DONE
- R12.1: Modify GetHeaderTexts to add placeholder entries for blank header cells (no skip) ? DONE
- R12.2: Adjust Row Data serialization: omit pair only if header+value both blank, value-only token if header blank but value present ? DONE
- R13.1: Add ToDateTime to Op ComboBox, preset Arg1=Var, Arg2 disabled, implement parse helper ? DONE
- R14.1: Add PACS method ComboBox items ? DONE
- R14.2: Implement PacsService header-based selection helpers ? DONE
- R15.1: Add GetDiagnosticsAsync to repository + implement queries ? DONE
- R15.2: Add DiagnosticsCommand + DiagnosticsInfo to VM and UI button/textbox ? DONE
- R16.1: Add PgDebug first-chance handler ? DONE
- R16.2: Initialize handler at startup ? DONE
- R16.3: Add targeted logging for repo methods ? PARTIAL (GetStudynamesAsync, EnsureStudynameAsync)
- R17.1: Replace hardcoded connection with LocalConnectionString fallback ? DONE
- R17.2: Add logging + note future central migration ? DONE

## Next
- N1: Relax matching rule (>= instead of = count) if partial matches desired
- N2: Show LOINC code next to long_common_name
- N3: Add a button to import selected playbook into preview
- N4: Hook procedure runner to call service methods by tag and display results
- N5: JSON export of Row Data preserving blanks

## References
- App: App.xaml / App.xaml.cs
- View: Views/StudynameLoincWindow.xaml / .xaml.cs
- View: Views/SpyWindow.xaml / .xaml.cs (GetValueFromSelection + blank cell/header preservation)
- VM: ViewModels/StudynameLoincViewModel.cs
- Repo: Services/StudynameLoincRepository.cs

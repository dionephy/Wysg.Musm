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

## Breakdown and status
- R8.1: Remove StartupUri from App.xaml and open MainWindow only after SplashLogin success ? DONE
- R9.1: Lower converter Minimum to 2 and VM threshold check to 2 ? DONE

## Next
- N1: Relax matching rule (>= instead of = count) if partial matches desired
- N2: Show LOINC code next to long_common_name
- N3: Add a button to import selected playbook into preview

## References
- App: App.xaml / App.xaml.cs
- View: Views/StudynameLoincWindow.xaml / .xaml.cs
- VM: ViewModels/StudynameLoincViewModel.cs
- Repo: Services/StudynameLoincRepository.cs

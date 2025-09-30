# Implementation Plan: Radium Cumulative (Reporting Workflow + Editor + Mapping + PACS)

## Change Log Addition
- **2025-10-03**: Adaptive completion popup height auto-sizing implemented (FR-134) – dynamic measurement of first item, exact height for ≤8 items, clamped height with scrollbar for larger sets; re-adjust on selection & rebuild.
- **2025-10-02**: Completion popup bounded height + single-step navigation stabilization implemented (FR-133) – internal navigation index prevents skip-over, ListBox height dynamically constrained to 8 visible items.
- **2025-10-01**: Completion popup navigation recursion fix implemented (FR-132) – added guard flag in MusmCompletionWindow to prevent infinite loops during programmatic selection changes while preserving legitimate keyboard navigation.
- **2025-09-30**: Focus-aware first navigation guard implemented (FR-131) – resets navigation state whenever the completion list rebuilds so the first Down/Up selects the boundary item, and editor now handles all subsequent Up/Down keys directly using guard-silent selection updates so the very next key advances to the adjacent item without duplicate presses or guard clears.
- **2025-01-01**: First navigation detection improvement implemented (FR-130) - added navigation state tracking to ensure first Down key selects first item.
- **2025-01-01**: Multiple event handling improvement implemented (FR-129) - enhanced selection preservation for keyboard navigation.
- **2025-01-01**: Selection guard recursion bug fix implemented (FR-128) - prevented recursive clearing of completion popup selections.
- **2025-01-01**: Bug fixes implemented - phrase extraction service injection (FR-126) and completion popup navigation reliability (FR-127).
- **2025-01-01**: Editor completion improvements implemented - completion cache invalidation (FR-124) and keyboard navigation fix (FR-125).
- Paragraph-based conclusion numbering implemented (multi-paragraph only).
- Dereportify now normalizes '-->' to '--> ' with single space.
- Added GetTextOCR procedure op + PACS banner helpers (current patient number, study date time).

(Update: account_id migration + phrase snapshot + OCR additions + completion improvements + bug fixes + selection guard fixes + multiple event handling + navigation state tracking + focus-aware first navigation guard + manual editor navigation handling + guard-silent selection updates + recursive guard protection)

**Branch**: `[radium-cumulative]` | **Date**: 2025-01-01 | **Spec**: Spec.md (same folder)
**Input**: Cumulative feature specification (Spec.md)

---
## Summary
Primary goal: Unify automated radiology reporting pipeline (current + previous study parsing, LOINC mapping, technique acquisition), enhanced editor assistance (completion, snippets, ghost suggestions), reliable PACS submission with structured persistence, plus OCR-based banner extraction utilities (GetTextOCR op and patient/study banner helpers).

---
## Technical Context
**Language/Version**: .NET 9 (WPF desktop)  
**Primary Dependencies**: WPF, AvalonEdit, Npgsql, Serilog, (LLM provider abstraction) [NEEDS CLARIFICATION: actual provider], internal RuleEngine, Windows OCR API (indirect via OcrReader)  
**Storage**: Postgres (phrase & mapping), local persistence (file or DB) [NEEDS CLARIFICATION: local DB type], in-memory caches  
**Testing**: xUnit (present) + golden text fixtures + OCR op unit test with engine-availability mock (future)  
**Target Platform**: Windows 10+ desktop  
**Performance Goals**: <1s metadata acquisition; <300ms completion popup open; ghost dispatch <100ms post idle; postprocess pipeline <5s; GetTextOCR typical <1.2s; banner heuristics <150ms; completion cache refresh <50ms; keyboard navigation <50ms; selection guard event handling <10ms; multiple event processing <5ms; navigation state tracking <1ms; recursive guard handling <1ms.  
**Constraints**: Graceful degradation if OCR engine disabled; no UI thread blocking for network / OCR heavy operations; prevent selection guard recursion loops; handle multiple SelectionChanged events gracefully; maintain accurate navigation state tracking with focus-aware reset when list loses keyboard focus; ensure editor-owned Up/Down handling remains deterministic without relying on ListBox focus state; prevent infinite loops during programmatic selection changes.

---
## Constitution Check
No architecture deviations: OCR op implemented in procedure executor. Banner helpers encapsulated inside PacsService. Low coupling maintained. Completion cache invalidation follows existing service patterns. Bug fixes maintain existing architectural patterns. Selection guard improvements preserve existing event handling architecture. Multiple event handling maintains WPF event model compliance. Navigation state tracking uses simple boolean flag pattern. Recursive guard protection uses standard guard pattern without architectural changes.

---
## Project Structure (Affected)
- apps/Wysg.Musm.Radium/Views/SpyWindow.* (procedure op + menu entries)
- apps/Wysg.Musm.Radium/Services/PacsService.cs (banner parsing)
- apps/Wysg.Musm.Radium/Services/PhraseService.cs (cache invalidation)
- apps/Wysg.Musm.Radium/Services/IPhraseCache.cs (Clear method)
- apps/Wysg.Musm.Radium/Views/MainWindow.xaml.cs (phrase extraction DI fix)
- apps/Wysg.Musm.Radium/App.xaml.cs (DI registration for PhraseExtractionViewModel)
- src/Wysg.Musm.Editor/Completion/MusmCompletionWindow.cs (selection guard + keyboard navigation + multiple event handling + enter/home/end handling + silent selection helper + recursive guard protection)
- src/Wysg.Musm.Editor/Controls/EditorControl.Popup.cs (navigation event handling + focus management + navigation state tracking + manual editor-driven Up/Down handling)
- tests/Wysg.Musm.Tests/CompletionNavigationTests.cs (unit tests for completion navigation)
- docs (Spec/Plan/Tasks updated cumulatively)

---
## Phase 0: Outline & Research
Pending clarifications extended to OCR (engine availability, fallback heuristics) and reliability markers.

---
## Phase 1: Design & Contracts
(UNCHANGED baseline) – Add note: ProcedureOp set extended with GetTextOCR (Arg1=Element, Arg2 disabled). PacsService contract implicitly extended (non-interface) with `GetCurrentPatientNumberAsync` & `GetCurrentStudyDateTimeAsync`. IPhraseCache extended with Clear method for cache invalidation. DI container configured for proper service resolution. Selection guard enhanced to prevent recursion and handle multiple events. Navigation state tracking added for accurate first/subsequent navigation detection with focus-aware reset so first navigation after typing hits the boundary item, and editor now owns Up/Down handling to keep behaviour deterministic without relying on ListBox focus. Recursive guard protection added to prevent infinite loops during programmatic changes.

---
## Phase 2: Task Planning Extension
Added incremental tasks (see Tasks.md T205..T208) covering implementation & spec alignment for FR-098..FR-099, FR-123. Added T209-T211 for completion improvements FR-124..FR-125. Added T214-T217 for bug fixes FR-126..FR-127. Added T218-T219 for selection guard recursion fix FR-128. Added T220-T221 for multiple event handling FR-129. Added T222-T223 for navigation state tracking FR-130. Added T224-T225 for focus-aware first navigation guard FR-131. Added T226-T229 for manual editor-driven navigation handling FR-131. Added T232-T233 for recursive guard protection FR-132. Added T234-T235 for completion popup bounded height FR-133. Added T236-T237 for adaptive completion popup height FR-134.

---
## Phase 3+: Future
Add tests later for OCR op fallbacks (engine missing) & banner regex accuracy (digit vs mixed token collision). Consider performance testing for completion cache refresh latency, keyboard navigation responsiveness, selection guard event handling efficiency, multiple event processing performance, navigation state tracking accuracy, and recursive guard protection effectiveness.

---
## Complexity Tracking
No new complexity exceptions.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|--------------------------------------|
| (none) | | |

---
## Progress Tracking
GetTextOCR + banner helpers implemented (status: Done). Editor completion improvements implemented (status: Done). Bug fixes implemented (status: Done). Selection guard recursion fix implemented (status: Done). Multiple event handling improvement implemented (status: Done). Navigation state tracking implemented (status: Done). Focus-aware first navigation guard implemented (status: Done). Manual editor-driven navigation implemented (status: Done). Guard-silent selection helper implemented (status: Done). Recursive guard protection implemented (status: Done). Completion popup bounded height implemented (status: Done). Adaptive completion popup height implemented (status: Done). Unit tests added (status: Done). Documentation updated.

---
- **Bug Fix: Recursive guard protection**: MusmCompletionWindow now uses _handlingSelectionChange flag to prevent infinite loops during programmatic selection changes while preserving legitimate keyboard navigation.
- **Bug Fix: Focus-aware navigation guard**: EditorControl now resets `_hasUserNavigated` whenever completions refresh and handles boundary detection entirely in-editor so the first Down/Up key selects index 0/last without relying on ListBox focus.
- **Bug Fix: Manual editor navigation**: EditorControl consumes all Up/Down keys, mutating the completion list selection directly so the very next key advances to the adjacent item with scroll-into-view support.
- **Bug Fix: Guard-silent selection helper**: MusmCompletionWindow exposes a helper to adjust ListBox selection without firing the guard, preventing automatic clear-outs after editor-driven navigation.

## Recent Adjustments (New - 2025-01-01)
- **Bug Fix: Navigation State Tracking**: Added _hasUserNavigated flag to EditorControl to properly track whether user has used keyboard navigation, ensuring first Down key always selects first item (index 0) regardless of existing selections from exact matching. Flag is reset when creating new completion window and when closing, providing accurate first vs subsequent navigation detection.

## Previous Adjustments (2025-01-01)
- **Bug Fix: Multiple Event Handling**: Enhanced MusmCompletionWindow selection guard to detect and preserve selections that are added via keyboard navigation by analyzing SelectionChanged event patterns (added vs removed items). Added logic to prevent immediate clearing of new selections that don't have corresponding removals.
- **Keyboard Focus Management Enhancement**: Improved EditorControl keyboard navigation to ensure ListBox gets proper focus during navigation, which enhances the keyboard focus detection in the selection guard and improves overall navigation reliability.

## Previous Adjustments (2025-01-01)
- **Bug Fix: Selection Guard Recursion Prevention**: Enhanced MusmCompletionWindow selection guard to prevent recursive clearing by temporarily disabling SelectionChanged event handler during programmatic selection changes. Added detection logic for programmatic clearing operations and improved event flow to handle multiple selection events from single user actions.
- **Navigation Event Handling Enhancement**: Improved EditorControl keyboard navigation with enhanced debugging and better flow control to ensure AllowSelectionByKeyboardOnce is called at the appropriate time before selection changes occur.

## Previous Adjustments (2025-01-01)
- **Bug Fix: Phrase Extraction Service Injection**: Modified OnExtractPhrases in MainWindow.xaml.cs to use GetService<PhraseExtractionViewModel>() instead of manual instantiation, and registered PhraseExtractionViewModel in DI container. This resolves runtime errors during phrase save operations by ensuring all dependencies are properly injected.
- **Bug Fix: Completion Navigation Reliability**: Enhanced Up/Down key handling in EditorControl.Popup.cs by explicitly managing selection state changes and proper event handling. Improved selection guard logic in MusmCompletionWindow.cs to be more permissive for keyboard navigation while preventing unwanted programmatic selection changes.

## Previous Adjustments (2025-01-01)
- **Completion Cache Invalidation**: Added IPhraseCache.Clear() method and implemented cache invalidation in PhraseService.UpsertPhraseAsync() and ToggleActiveAsync(). This ensures completion list refreshes immediately when phrases are added via phrase extractor or phrase manager without requiring application restart.
- **Keyboard Navigation Fix**: Modified MusmCompletionWindow selection guard mechanism to allow keyboard navigation (Down/Up keys) while maintaining exact-match-only selection for non-keyboard events. Selection changes are now permitted when ListBox has keyboard focus.

## Recent Adjustments (2025-09-29)
- Added GetTextOCR operation (SpyWindow) with OCR region = element bounds (naive bounding; future improvement: clamp inside visible window rect).
- Added PacsService methods extracting patient number (longest 5+ digit sequence) and study date time (YYYY-MM-DD + optional HH:mm[:ss]).
- Updated App shutdown mode to OnMainWindowClose after successful login for expected desktop behavior.

---
(Existing historical adjustments retained below)

## Recent Adjustments
- Central phrase service now loads per-account snapshot (AccountPhraseState) and serves editor completions from in-memory data.
- All future references use account_id (legacy tenant_id alias retained at boundaries).
- Added non-blocking retry prefetch for phrase snapshot.
- PhraseCache supports Clear.

## Recent Adjustment (Editor Idle Behavior)
- Removed legacy idle auto-close of completion popup.

## Recent Adjustment (Completion Navigation)
- Initial Up/Down selects boundary item immediately.

## Recent UI Adjustment
- Dark theme styling extended.

## Added Implementation (Reportified Toggle)
- Toggle handlers and reversible dereportify algorithm (see Spec) simplified by inversion logic.

# Implementation Plan: Radium Cumulative (Reporting Workflow + Editor + Mapping + PACS)

## Change Log Addition (2025-10-05)
- Added AI orchestration skeleton (Domain interfaces + UseCases ReportPipeline + Infrastructure NoOp skills + DI AddMusmAi extension + API registration). Implements FR-AI-001..FR-AI-008 partial (FR-AI-009/010 future enhancements).

## AI Architecture Overview (New Section)
Layer Responsibilities:
1. Domain (Wysg.Musm.Domain.AI)
   - Pure contracts & record types (provider agnostic) : ILLMClient, IModelRouter, skill interfaces, ReportState, context records.
2. UseCases (Wysg.Musm.UseCases.AI)
   - Orchestrator (ReportPipeline) composing skill interfaces; no vendor code.
3. Infrastructure (Wysg.Musm.Infrastructure.AI)
   - Adapters (future: OpenAI/Ollama), routing, telemetry, prompt templates. Currently: NoOp implementations for development.
4. API Host (Wysg.Musm.Api)
   - Registers AddMusmAi(); future Minimal API endpoints expose structured skills (e.g., POST /api/report/current-study-intake, POST /api/report/postprocess). No direct raw prompt endpoint.

Dependency Direction: UI/Clients -> API (HTTP) -> UseCases -> Domain; Infrastructure implements Domain & is registered only at host boundary.

Extensibility Path:
- Add real provider: implement ILLMClient + skill classes; adjust AddMusmAi(useNoOp:false) + config binding.
- Add new skill: define interface in Domain, implement Infrastructure adapter, extend ReportPipeline or new orchestrator.

Fallback Strategy:
- NoOp skills return empty strings/unchanged text enabling UI operation without blocking (graceful degradation while server offline).

Telemetry Plan:
- IInferenceTelemetry future implementation logs structured event {skill, model, ms, success, tokens} (Serilog sink + optional metrics).

Next Steps (AI):
- Define JSON contract validation & retry guard.
- Implement HeaderSplitter, HeaderParser, Proofreader (real) + routing heuristics.
- Add error isolation (try/catch around each stage) fulfilling FR-AI-009.
- Add configuration section: "Ai:Provider:OpenAI" etc.


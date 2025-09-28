# MUSM Editor Specification

This document defines the current (new) editor behavior in Wysg.Musm.Editor / Radium, contrasted where useful with the legacy Wynolab.Musm.A.Editor implementation. It supersedes earlier short notes.

---
## 1. Components Overview
- MusmEditor: Thin subclass of AvalonEdit TextEditor providing bindable dependency properties (DocumentText, CaretOffsetBindable, selection mirrors, SelectedTextBindable). No completion/snippet orchestration logic lives here.
- EditorControl (UserControl): Wraps a MusmEditor instance and owns higher?level features: completion popup lifecycle, snippet trigger, server ghost (AI) suggestions, idle/debounce timers, current-word highlight, key routing.
- MusmCompletionWindow: Lightweight CompletionWindow specialization replacing exactly the current word span (StartOffset..EndOffset) and enforcing ¡°exact match or nothing¡± selection unless arrow keys are used.
- Snippet system: CodeSnippet + SnippetInputHandler (placeholder expansion, navigation). Exposed through ISnippetProvider provided to EditorControl.
- Ghost suggestions: Two kinds are currently scaffolded (multi?line ghost renderer + possible inline ghost). Activated only after an idle interval; never while the user is actively typing into a completion session.

Legacy differences:
- Legacy window (Wynolab.Musm.A.Editor.MusmCompletionWindow) managed more concerns (dynamic removal length, custom tooltip, broader selection rules). New design intentionally isolates responsibilities and uses a strict word boundary model.

---
## 2. Terminology
- editor_text: Entire document text.
- loi (line of interest): The current line containing the caret.
- woi (word of interest): The contiguous run of letters (char.IsLetter == true) that the caret is inside or immediately adjacent to on its right edge. Current implementation only treats letters A?Z/a?z as word chars (digits, underscore terminate the span).
- woi_prefix: Substring of woi from its start to (caret - 1).
- woi_suffix: Substring of woi from caret to woi end.
- replace range: [StartOffset, EndOffset] maintained by MusmCompletionWindow; covers the whole current woi (prefix + suffix) at popup creation; caret is somewhere inside that closed-open range.
- snippet placeholder: Token of form ${...} inside snippet template (see ¡×7).
- server ghost: AI suggestion(s) rendered visually after user idle.

---
## 3. Timers and High?Level Event Flow
- Debounce timer (configurable DebounceMs, default 200 ms): Used ONLY for light UI refresh operations (e.g., potential future ghost previews). It must NOT perform server calls or forcibly close the popup.
- Idle timer (GhostIdleMs, default 2000 ms): Restarted on text change / caret move / selection changes (unless suppressed). When it elapses:
  1. Completion popup (if open) is closed.
  2. IdleElapsed event fires ¡æ ViewModel/API may request server ghost suggestions.
- Scrolling, losing focus, or explicit pause conditions stop both timers.

---
## 4. Completion Window Lifecycle (Current Logic)
### 4.1 Open Conditions
Triggered inside EditorControl.OnTextEntered after a character is inserted:
1. AutoSuggestOnTyping == true.
2. SnippetProvider != null.
3. Compute woi by scanning left from caret while char.IsLetter; woi length >= 1.
4. woi length >= MinCharsForSuggest (default 2).
5. SnippetProvider.GetCompletions(Editor) returns at least one item.
6. If already open but woi start or text changed, window is rebuilt; otherwise ignored.

If conditions pass: create MusmCompletionWindow (if null), set StartOffset to woi start, repopulate all items, then attempt exact selection.

### 4.2 Persist Conditions
Popup stays open while ALL hold:
- Caret offset remains within [StartOffset, EndOffset].
- Current woi (recomputed on left/right or other key moves) is non-empty.
- Editor has keyboard focus.
- Not closed due to idle or explicit key command.

### 4.3 Recompute / Selection Updates
- Typing letters extends woi; window¡¯s EndOffset automatically moves because AvalonEdit inserts text before caret but StartOffset stays fixed; selection re-evaluated by recomputing word.
- Left/Right arrow: recompute woi; if empty ¡æ close.
- Home/End: caret moved to line boundary; if outside [StartOffset, EndOffset] ¡æ close.

### 4.4 Filtering & Selection Semantics
- Underlying CompletionList.IsFiltering = true (AvalonEdit built-in filtering by completionSegment text). We repopulate full candidate list on open; subsequent filtering is handled internally.
- Exact-match selection rule: On each word update we call SelectExactOrNone(word). No fuzzy / prefix auto-selection.
- Up/Down arrows: Before handling, AllowSelectionByKeyboardOnce() is called so one selection change is permitted; subsequent automatic selection still cleared unless exact match.

### 4.5 Insertion Triggers
Insertion happens via AvalonEdit completion pipeline when CompletionList.RequestInsertion is invoked. Triggers:
- Pressing Enter/Return when a completion item is selected.
- Typing a non-letter/digit character (OnTextEntering detects and calls RequestInsertion). Typical delimiters: space, punctuation, etc.
- (Legacy design supported Tab for insertion; NEW design reserves Tab for ghost acceptance; raw Tab insertion into document is prevented.)

Special cases:
- Enter/Return with NO selection: Popup closes; a newline is inserted manually (ensures single press behavior, not swallowed by CompletionWindow default handling).

### 4.6 Close Conditions
Popup closes immediately when any of:
- Caret leaves [StartOffset, EndOffset].
- woi becomes empty (after deletion/backspace or moving off letters).
- Esc key pressed (handled by AvalonEdit default or upstream logic).
- Focus is lost (CloseAutomatically = true).
- Idle timer elapses (2s inactivity) prior to requesting server ghosts.
- A completion item insertion occurs.

---
## 5. Word Boundary Definition (Current vs Legacy)
Current implementation: A word is restricted to letters only (char.IsLetter). Digits, underscore, and other symbols terminate the span. Legacy editor allowed letter/digit/underscore runs.
Future extension: If broader tokenization needed (e.g., identifiers with digits/underscore), adjust GetCurrentWord and MusmCompletionWindow.ComputeReplaceRegionFromCaret accordingly.

---
## 6. Completion Item Types
Represented by MusmCompletionData:
- Token: Simple replacement (Text == Replacement).
- Hotkey: Shortcut expansion (Text is typed key sequence; Replacement is expanded phrase).
- Snippet: Provides shortcut; on insertion empty text is first replaced by snippet expansion managed by SnippetInputHandler.

Properties:
- Text: Display & match key (shortcut).
- Replacement: Inserted text for non-snippet items.
- Preview: Shown in potential preview systems / ghost overlay (unused directly yet for inline ghost shadowing).
- Description: Tooltip / explanatory text.

---
## 7. Snippet Logic
### 7.1 Placeholder Syntax
General forms inside template string:
- ${identifier} ¡æ free text placeholder (initial content = identifier; user can overwrite).
- ${number} ¡æ free text placeholder (numeric default suggestion).
- ${date} ¡æ free text placeholder (expected date value).
- ${index^single choice=key^Value|key2^Value2|...}
  * Single choice: caret lands here; user can cycle/replace.
- ${index^multiple choices^or=key^Value|b^Value2}
  * Semantic variant (keyword "or" captures multi-choice semantics; engine presently treats same as choice list, could be extended for multi-select UI).
- ${index^replace=key^Value|...}
  * Replace semantics (user types key; engine substitutes mapped Value).
- ${named} (where named is any token not matching pattern with ^) behaves as a free placeholder.

Index: Integer ordering of navigation. Placeholders without explicit numeric index follow natural left-to-right navigation order after numbered ones.

### 7.2 Example
"i have ${1^fruit=a^apple|b^bannana|3^watermelon} and ${2^juices=a^cola|b^cider}, and i want to eat with ${friend}, but ${3^family=mm^mom|dd^dad} said no..."

### 7.3 Expansion Flow
1. On snippet completion insert, the raw shortcut region is replaced with expanded template.
2. SnippetInputHandler parses placeholders ¡æ builds a map (anchor segments for each placeholder span + variant metadata).
3. Caret is placed at first placeholder (lowest index; if none indexed, first encountered placeholder).
4. Navigation (Tab/Shift+Tab or custom keys ? to be specified if added) cycles through placeholders.
5. Placeholder replacement updates document while protecting against intermediate AvalonEdit undo conflicts (mutation shielding pattern may be applied elsewhere, outside snippet expansion core presently).

---
## 8. Server Ghost Suggestions (Idle AI)
### 8.1 Trigger
- Idle timer (GhostIdleMs) elapses (no text/caret/selection activity) ¡æ CloseCompletionWindow() ¡æ RequestServerGhostsAsync() (fires IdleElapsed event; actual network request resides in consumer ViewModel/service).

### 8.2 Rendering
- MultiLineGhostRenderer draws a list of alternatives anchored at caret/line context.
- GhostStore maintains Items and a SelectedIndex.

### 8.3 Interaction
- When popup CLOSED and ghosts visible:
  * Up/Down: Move selection within ghosts (handled in OnTextAreaPreviewKeyDown before completion logic).
  * Tab: Accept selected ghost (AcceptSelectedServerGhost). Tab insertion into editor text is globally suppressed (DisableTabInsertion) so acceptance logic has priority.
- Typing (any character), Backspace, Delete, or Enter clears ghost suggestions and restarts idle timer.

### 8.4 Acceptance (High-Level)
- Inserting a ghost replaces (or inserts after) current caret context (exact replacement span subject to ghost renderer policy). After acceptance, ghosts are cleared and idle timer restarts.

### 8.5 Coexistence with Completion Popup
- Ghosts never appear while completion popup is open (idle closes popup first).
- Opening completion popup clears existing ghosts immediately.

---
## 9. Key Handling Summary (Current)
While completion popup open:
- Letter: Extends woi; refilter; maintain range.
- Non-letter (space, punctuation): Triggers insertion if an item selected; else passes through (may close if woi emptied).
- Enter/Return: If selection present ¡æ insert; else force close + newline insertion.
- Up/Down: Permit one selection change (AllowSelectionByKeyboardOnce). No cycling beyond list (AvalonEdit standard behavior).
- Left/Right: Move caret; recompute woi; close if empty.
- Home/End: Move caret to line boundary; close if outside range.
- Esc: Close.
- Tab: (Reserved ? not used for insertion).

While ghosts visible (no popup):
- Up/Down: Cycle ghost selection.
- Tab: Accept selected ghost.
- Any editing key (text input, Backspace, Delete, Enter with newline) clears ghosts.

Global:
- Idle (2s): Close popup (if any) then fetch ghosts.

---
## 10. Differences vs Legacy Design (Concise)
| Aspect | Legacy | Current |
|--------|--------|---------|
| Word chars | Letters/digits/underscore (implied) | Letters only (char.IsLetter) |
| Selection auto-pick | First/prefix match often preselected | None unless exact match or user arrow selects |
| Tab behavior | Could insert completion/snippet | Reserved for ghost accept; completion insertion via Enter or delimiter |
| Backspace logic | Manual recalculation of remove length | Relies on word recompute & automatic filtering, closes when empty |
| Tooltip | Manual ToolTip management | (Optional) Description property; no explicit tooltip wiring yet |
| Snippet insertion | StartOffset/EndOffset + custom anchor substitution | MusmCompletionData + SnippetInputHandler with standardized expansion |
| Ghost AI | Not integrated | Idle-triggered multi-line ghost suggestions |

---
## 11. Extension Points / TODOs
- Broaden word boundary rules (allow digits/underscore) ¡æ adjust GetCurrentWord + MusmCompletionWindow.ComputeReplaceRegionFromCaret.
- Inline ghost preview utilizing MusmCompletionData.Preview.
- Configurable insertion delimiters list (currently implicit via non-letter check).
- Snippet multi-select placeholder semantics (true multi-choice acceptance for "multiple choices^or").
- Tooltip/description hover integration for completion items (currently available but not shown).
- Ghost acceptance span policy (line tail vs. dynamic prefix replacement) formalization.

---
## 12. Minimal State Machine (Completion Popup)
States: Closed, Open.

Events:
- TypeLetter(w) ¡æ if Closed & conditions (len>=MinChars, provider items>0) => Open; if Open recompute; else ignore.
- TypeNonLetter(ch) ¡æ if Open and selection present => Insert+Closed; else maybe close (if woi empty after insertion of ch).
- MoveCaretInsideRange ¡æ stay Open.
- MoveCaretOutsideRange | WoiEmpty ¡æ Close.
- Up/Down (Open) ¡æ Allow one selection (no state change).
- Enter (Open, selection) ¡æ Insert+Close; Enter (Open, no selection) ¡æ Close + Newline.
- Esc ¡æ Close.
- IdleElapsed ¡æ Close.

---
## 13. Acceptance Rules (Deterministic Summary)
1. Completion insertion never partially replaces prefix differently from suffix; entire [StartOffset, EndOffset] replaced (tokens/hotkeys) unless snippet (window removes segment then snippet expands at StartOffset).
2. Snippet caret ends at first placeholder (lowest index) after expansion.
3. Ghost acceptance (when implemented) inserts its full text; no attempt to merge with open completion session.

---
## 14. Backwards Compatibility Notes
- If legacy behavior (digits+underscore) is required for migration, add char.IsLetterOrDigit || ch=='_' checks uniformly in GetCurrentWord and boundary helper.
- To restore Tab insertion, remove suppression in DisableTabInsertion and map Tab to CompletionList.RequestInsertion when popup open.

---
## 15. Example Timeline
User types: m ¡æ (len=1, if MinChars=2 no popup) ¡æ i ¡æ word="mi" (len=2) ¡æ popup opens with StartOffset at word start.
User types: c ¡æ word="mic"; if exact token "mic" exists, it becomes selected else selection cleared.
User presses Enter:
- If selection present: replace [StartOffset, EndOffset] with replacement.
- If none: popup closes, newline inserted explicitly.
Idle 2s after last modification ¡æ popup closed (if still open) ¡æ IdleElapsed event fired ¡æ ghost request begins.

---
## 16. Snippet Placeholder Navigation (Planned/Typical)
(Not all keys wired yet in current code; documented intent.)
- Tab ¡æ Next placeholder.
- Shift+Tab ¡æ Previous placeholder.
- Enter inside final placeholder may exit snippet mode (implementation detail of SnippetInputHandler).

---
## 17. Quality / Edge Considerations
- Undo safety: MusmEditor.SafeReplace handles AvalonEdit invalid operation during undo/redo by deferring replacements via Dispatcher.
- Selection loops: SelectedTextBindable updates suppressed during mirror propagation to avoid recursion.
- When Completion popup is open, Home/End keep intuitive line navigation without leaving residual popup state.

---
## 18. Open Questions
(Track for future refinement.)
- Should ghost acceptance respect indentation / line context merging? (Currently unspecified.)
- Should completion filter include fuzzy scoring beyond prefix? (Currently prefix/exact via AvalonEdit default.)
- Multi-line snippet field editing interactions with ghost suggestion timing (currently ghosts suppressed by any popup activity).

---
End of specification.


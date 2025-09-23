# Radium: PACS Reporting Spec

## Requests (this iteration)
1) Spy UI Tree focus behavior
- Show a single chain down to level 4 (root ¡æ ... ¡æ level 4).
- Expand the children of the level-4 element; depth bounded by `FocusSubtreeMaxDepth`.
- Depths configurable via constants in `SpyWindow`.

2) Procedures grid argument editors
- Operation presets must set Arg1/Arg2 types and enablement, with immediate editor switch.
- Element/Var editors use dark ComboBoxes.

## Implemented
- PP1: Tree now mirrors crawl editor logic. Chain is built to level 4 and children of that element are populated (not deeper ancestors), with caps.
- PP2: Removed commit/refresh and forced re-open from SelectionChanged to stop infinite re-open loop and row recycling/removal; preset still sets Arg types and enablement.
- `ProcArg`/`ProcOpRow` implement INotifyPropertyChanged so templates update immediately when types change.

## Debugging
- PP1: Confirm chain matches crawl editor path and children under the level-4 node are visible.
- PP2: Selecting GetText should not cause row removal or sluggishness.

## PP1 ? Tree/Crawl parity
- The UI Tree now uses `UiBookmarks.ResolvePath` to obtain the exact path (root ¡æ ¡¦ ¡æ final) used by the resolver.
- Display logic: build a single chain to level 4 from that path, then show all children of the level-4 element up to `FocusSubtreeMaxDepth`.
- Fallbacks:
  1) If process windows are not found, attempt desktop-wide discovery of the first included node, then ascend to the top-level Window/Pane and rebuild.
  2) If a path still cannot be formed, use the previous heuristics (first-child chain + subtree).

## PP2 ? Procedures Grid
- Stable after removing force-reopen and grid commits from SelectionChanged; presets set types/enabled; editors switch immediately.

## Limits
- Children per node are capped to 100, subtree depth is bounded by `FocusSubtreeMaxDepth`.

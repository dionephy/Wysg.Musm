# Fix: Prevent concurrent automation sessions from starting (2026-01-13)

**Status:** Complete

## Problem
Starting multiple automation sessions in quick succession could overlap because the dispatcher fired `RunModulesSequentially` without awaiting. Concurrent runs reused the shared ProcedureExecutor session id and UI state, causing unpredictable behavior.

## Solution
Added a lightweight session lock around `RunModulesSequentially` in `MainViewModel.Commands.Automation.Core.cs`:
- Introduced a `SemaphoreSlim` `_automationSessionLock` (1 permit per VM instance).
- Attempt a non-blocking `WaitAsync(0)`; if another session is active, the new request is rejected with a status message and debug log.
- Release the lock in a `finally` block to cover all exit paths (success, abort, or exception).

## Behavior
- When an automation session is already running, subsequent triggers immediately report `">> <sequence> already running"` and do not start another session.
- Only one automation session executes at a time, eliminating session id and cache collisions.

## Files
- `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Commands.Automation.Core.cs`

## Testing
- Trigger automation A, then trigger automation B before A completes: B is rejected with the "already running" status and no steps execute.
- Run a single automation sequence: executes normally and completes; subsequent runs start after the first finishes.
- Build succeeds (no new warnings/errors).

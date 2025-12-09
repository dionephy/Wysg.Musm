# FEATURE: Message Prompt If/Else Automation (2025-12-09)

## Summary
- Added a new custom module type `If message {procedure} is Yes` that executes a custom procedure, shows the returned string in a Yes/No dialog, and branches based on the user's response.
- Introduced a built-in control module `Else If Message is No` that marks the alternate branch inside the same `If` block. The block must still be terminated by `End if`.
- Runtime now jumps over the non-selected branch, ensuring that "Yes" flows skip the `Else` section and "No" flows jump directly to it.
- Message dialogs spawned by these modules now always display the title **"Confirmation"** for consistent UX copy.

## Authoring Guidance
1. Create a custom module using the new **If message is Yes** type and select the procedure whose result should appear in the prompt.
2. Place the module inside an automation list, add any steps that should run when the user clicks **Yes**.
3. (Optional) Insert the built-in `Else If Message is No` module after the "Yes" branch and add the "No" branch modules following it.
4. Close the block with `End if`. Only a single `Else If Message is No` is allowed between the opening `If message ...` and its `End if`.

## Validation & Safety
- Saving automation now fails if `Else If Message is No` is missing its owning `If message ...` block, appears more than once, or falls outside the block.
- If the user clicks **No** and no `Else If` is present, execution jumps straight to the closing `End if`.
- Message prompts are suppressed automatically when the parent `If` block is already skipped, preventing stray modal dialogs during skipped execution paths.

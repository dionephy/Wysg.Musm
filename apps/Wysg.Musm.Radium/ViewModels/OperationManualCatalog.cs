using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Provides manual descriptions for each procedure operation.
    /// </summary>
    internal static class OperationManualCatalog
    {
        public static IReadOnlyList<OperationManualEntry> Entries { get; } = BuildEntries();

        private static IReadOnlyList<OperationManualEntry> BuildEntries()
        {
            var list = new List<OperationManualEntry>
            {
                new(
                    "GetText",
                    "Reads text from a UI Automation element, falling back to Name/Legacy patterns.",
                    new[]
                    {
                        OperationManualArgument.Required("Arg1", "Element | Var", "Bookmark or cached element to read."),
                    },
                    "Outputs the captured text or null when the element cannot be resolved.",
                    "Element I/O"),
                new(
                    "GetTextOnce",
                    "Single-attempt version of GetText (no retries when the element is missing).",
                    new[]
                    {
                        OperationManualArgument.Required("Arg1", "Element | Var", "Bookmark or cached element to read."),
                    },
                    "Outputs the captured text or null when resolution fails.",
                    "Element I/O"),
                new(
                    "GetTextWait",
                    "Polls for the target element up to 5 seconds until it becomes visible, then reads text.",
                    new[]
                    {
                        OperationManualArgument.Required("Arg1", "Element | Var", "Element to monitor for text availability."),
                    },
                    "Outputs text when available or null if the timeout expires.",
                    "Element I/O"),
                new(
                    "GetName",
                    "Reads the UI Automation Name property only (useful for labels that have no ValuePattern).",
                    new[]
                    {
                        OperationManualArgument.Required("Arg1", "Element | Var", "Target element."),
                    },
                    "Outputs the Name property or empty string.",
                    "Element I/O"),
                new(
                    "GetTextOCR",
                    "Captures a screenshot of the element header region and runs OCR to extract text.",
                    new[]
                    {
                        OperationManualArgument.Required("Arg1", "Element", "Element whose visible contents should be recognized."),
                    },
                    "Outputs OCR text when recognition succeeds.",
                    "Element I/O"),
                new(
                    "Split",
                    "Splits a string/variable using a literal or regex separator and returns a specific slice.",
                    new[]
                    {
                        OperationManualArgument.Required("Arg1", "Var", "Source variable that holds the text to split."),
                        OperationManualArgument.Required("Arg2", "String", "Separator text. Prefix with 're:' or 'regex:' for Regex."),
                        OperationManualArgument.Required("Arg3", "Number", "Zero-based index of the part to return."),
                    },
                    "Outputs the requested slice or null when the index is out of range.",
                    "String"),
                new(
                    "Trim",
                    "Trims whitespace from the start and end of the supplied value.",
                    new[]
                    {
                        OperationManualArgument.Required("Arg1", "Var", "Variable holding the value to trim."),
                    },
                    "Outputs the trimmed text.",
                    "String"),
                new(
                    "TrimString",
                    "Removes a specific substring repeatedly from the beginning and end of the source value.",
                    new[]
                    {
                        OperationManualArgument.Required("Arg1", "String | Var", "Source text."),
                        OperationManualArgument.Required("Arg2", "String | Var", "Substring to strip from both sides."),
                    },
                    "Outputs the trimmed result (unchanged if substring not found).",
                    "String"),
                new(
                    "Invoke",
                    "Invokes the element via InvokePattern or TogglePattern (clicks buttons, toggles checkboxes).",
                    new[]
                    {
                        OperationManualArgument.Required("Arg1", "Element", "Element to invoke."),
                    },
                    "Produces no output; effect happens inside the target app.",
                    "Element action"),
                new(
                    "GetValueFromSelection",
                    "Reads a column value from the first selected row in a list/grid element.",
                    new[]
                    {
                        OperationManualArgument.Required("Arg1", "Element", "List or grid that supports SelectionPattern."),
                        OperationManualArgument.Optional("Arg2", "String", "Header/column name to extract (defaults to 'ID')."),
                    },
                    "Outputs the column text or null when the header cannot be matched.",
                    "Element I/O"),
                new(
                    "GetDateFromSelectionWait",
                    "Same as GetValueFromSelection but keeps polling up to 5 seconds until the column value parses as a date.",
                    new[]
                    {
                        OperationManualArgument.Required("Arg1", "Element", "List or grid containing the selection."),
                        OperationManualArgument.Optional("Arg2", "String", "Header/column name to inspect (defaults to 'ID')."),
                    },
                    "Outputs the trimmed column text once it parses as a DateTime; returns null after timeout.",
                    "Element I/O"),
                new(
                    "GetSelectedElement",
                    "Captures the currently selected row element so it can be reused via a Var argument.",
                    new[]
                    {
                        OperationManualArgument.Required("Arg1", "Element", "List or grid to inspect."),
                    },
                    "Stores a cache key (Var value) that later operations can pass as Arg1 Type=Var.",
                    "Element I/O"),
                new(
                    "Replace",
                    "Performs string.Replace after unescaping input, useful for newline tokens, etc.",
                    new[]
                    {
                        OperationManualArgument.Required("Arg1", "Var", "Source text."),
                        OperationManualArgument.Required("Arg2", "String | Var", "Text to find (supports escape sequences)."),
                        OperationManualArgument.Required("Arg3", "String | Var", "Replacement text."),
                    },
                    "Outputs the transformed text.",
                    "String"),
                new(
                    "GetHTML",
                    "Downloads a URL and decodes the HTML using smart Korean/UTF encodings.",
                    new[]
                    {
                        OperationManualArgument.Required("Arg1", "String | Var", "HTTP/HTTPS URL to fetch."),
                    },
                    "Outputs the HTML payload.",
                    "Web"),
                new(
                    "MouseClick",
                    "Performs an absolute screen click at the provided coordinates.",
                    new[]
                    {
                        OperationManualArgument.Required("Arg1", "Number", "Screen X coordinate (pixels)."),
                        OperationManualArgument.Required("Arg2", "Number", "Screen Y coordinate (pixels)."),
                    },
                    "No output; physically clicks the location.",
                    "System"),
                new(
                    "ClickElement",
                    "Clicks the center of the provided element and restores the mouse cursor to its prior position.",
                    new[]
                    {
                        OperationManualArgument.Required("Arg1", "Element | Var", "Bookmark or cached element."),
                    },
                    "No output; mouse returns to original location.",
                    "Element action"),
                new(
                    "ClickElementAndStay",
                    "Clicks the element but leaves the mouse cursor at the clicked coordinates (useful for drag workflows).",
                    new[]
                    {
                        OperationManualArgument.Required("Arg1", "Element | Var", "Target element."),
                    },
                    "No output; cursor remains at element center.",
                    "Element action"),
                new(
                    "MouseMoveToElement",
                    "Moves the mouse pointer to the element center without clicking.",
                    new[]
                    {
                        OperationManualArgument.Required("Arg1", "Element", "Element that defines the destination point."),
                    },
                    "No output; cursor is repositioned.",
                    "Element action"),
                new(
                    "IsVisible",
                    "Checks whether an element has a non-empty bounding rectangle (width/height > 0).",
                    new[]
                    {
                        OperationManualArgument.Required("Arg1", "Element", "Element to inspect."),
                    },
                    "Outputs 'true' if visible, otherwise 'false'.",
                    "Element I/O"),
                new(
                    "SetFocus",
                    "Brings the owning window to the foreground and calls Focus() with retries.",
                    new[]
                    {
                        OperationManualArgument.Required("Arg1", "Element", "Element to focus."),
                    },
                    "No output; focus moves when successful.",
                    "Element action"),
                new(
                    "SetValue",
                    "Uses ValuePattern.SetValue to write text into an editable UI Automation control.",
                    new[]
                    {
                        OperationManualArgument.Required("Arg1", "Element", "Editable element with ValuePattern."),
                        OperationManualArgument.Required("Arg2", "String | Var", "Text to write (null becomes empty)."),
                    },
                    "No output; relies on ValuePattern support.",
                    "Element action"),
                new(
                    "SetValueWeb",
                    "Simulates clipboard-free typing for web/electron controls (focus, Ctrl+A, SendKeys).",
                    new[]
                    {
                        OperationManualArgument.Required("Arg1", "Element", "Editable web control."),
                        OperationManualArgument.Required("Arg2", "String | Var", "Text to type (special keys escaped automatically)."),
                    },
                    "No output; text is typed character by character.",
                    "Element action"),
                new(
                    "SetClipboard",
                    "Copies the supplied text into the Windows clipboard on a temporary STA thread.",
                    new[]
                    {
                        OperationManualArgument.Required("Arg1", "String | Var", "Text to store in the clipboard."),
                    },
                    "No output; clipboard contents change.",
                    "System"),
                new(
                    "SimulateTab",
                    "Sends the TAB key (like pressing Tab once).",
                    Array.Empty<OperationManualArgument>(),
                    "No output; focus moves per OS rules.",
                    "System"),
                new(
                    "SimulatePaste",
                    "Sends Ctrl+V to paste clipboard contents.",
                    Array.Empty<OperationManualArgument>(),
                    "No output; relies on clipboard content.",
                    "System"),
                new(
                    "SimulateSelectAll",
                    "Sends Ctrl+A to select all text in the focused control.",
                    Array.Empty<OperationManualArgument>(),
                    "No output.",
                    "System"),
                new(
                    "SimulateDelete",
                    "Sends the Delete key once.",
                    Array.Empty<OperationManualArgument>(),
                    "No output.",
                    "System"),
                new(
                    "Echo",
                    "Returns the provided value unchanged (handy for copying built-in properties to vars).",
                    new[]
                    {
                        OperationManualArgument.Required("Arg1", "String | Var", "Value to echo."),
                    },
                    "Outputs the same text.",
                    "String"),
                new(
                    "IsMatch",
                    "Performs a case-sensitive equality check between two strings.",
                    new[]
                    {
                        OperationManualArgument.Required("Arg1", "String | Var", "Left-hand value."),
                        OperationManualArgument.Required("Arg2", "String | Var", "Right-hand value."),
                    },
                    "Outputs 'true' when the values match exactly.",
                    "Logic"),
                new(
                    "IsAlmostMatch",
                    "Compares strings using normalized text plus OCR-friendly datetime heuristics.",
                    new[]
                    {
                        OperationManualArgument.Required("Arg1", "String | Var", "First value."),
                        OperationManualArgument.Required("Arg2", "String | Var", "Second value."),
                    },
                    "Outputs 'true' when considered equivalent (exact, normalized, or datetime-similar).",
                    "Logic"),
                new(
                    "And",
                    "Boolean AND that evaluates 'true' only when both inputs are the string 'true'.",
                    new[]
                    {
                        OperationManualArgument.Required("Arg1", "Var", "First boolean result variable."),
                        OperationManualArgument.Required("Arg2", "Var", "Second boolean result variable."),
                    },
                    "Outputs 'true' or 'false'.",
                    "Logic"),
                new(
                    "Not",
                    "Boolean NOT ? returns true when the input is not the string 'true'.",
                    new[]
                    {
                        OperationManualArgument.Required("Arg1", "Var", "Boolean variable to invert."),
                    },
                    "Outputs 'true' when Arg1 != 'true'.",
                    "Logic"),
                new(
                    "IsBlank",
                    "Checks whether the input is null, empty, or whitespace only.",
                    new[]
                    {
                        OperationManualArgument.Required("Arg1", "String | Var", "Value to check."),
                    },
                    "Outputs 'true' if blank.",
                    "Logic"),
                new(
                    "GetLongerText",
                    "Compares two values and returns whichever contains more characters (ties prefer Arg1).",
                    new[]
                    {
                        OperationManualArgument.Required("Arg1", "String | Var", "First value."),
                        OperationManualArgument.Required("Arg2", "String | Var", "Second value."),
                    },
                    "Outputs the longer string.",
                    "String"),
                new(
                    "Merge",
                    "Concatenates two values with an optional separator in Arg3.",
                    new[]
                    {
                        OperationManualArgument.Required("Arg1", "String | Var", "Left text."),
                        OperationManualArgument.Required("Arg2", "String | Var", "Right text."),
                        OperationManualArgument.Optional("Arg3", "String | Var", "Separator inserted between values."),
                    },
                    "Outputs the merged result.",
                    "String"),
                new(
                    "Delay",
                    "Sleeps the automation thread for the specified number of milliseconds.",
                    new[]
                    {
                        OperationManualArgument.Required("Arg1", "Number", "Milliseconds to wait."),
                    },
                    "No output; execution pauses.",
                    "System"),
             };

            return new ReadOnlyCollection<OperationManualEntry>(list);
        }
    }

    internal sealed record OperationManualEntry(
        string Name,
        string Summary,
        IReadOnlyList<OperationManualArgument> Arguments,
        string OutputNotes,
        string Category)
    {
        public string ArgumentSummary => Arguments.Count == 0 ? "No arguments" : $"{Arguments.Count} argument(s)";
    }

    internal sealed record OperationManualArgument(string Name, string AcceptedTypes, string Description, bool IsRequired)
    {
        public string Requirement => IsRequired ? "Required" : "Optional";

        public static OperationManualArgument Required(string name, string acceptedTypes, string description) =>
            new(name, acceptedTypes, description, true);

        public static OperationManualArgument Optional(string name, string acceptedTypes, string description) =>
            new(name, acceptedTypes, description, false);
    }
}

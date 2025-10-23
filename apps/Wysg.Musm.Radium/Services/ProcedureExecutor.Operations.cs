using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FlaUI.Core.AutomationElements;

namespace Wysg.Musm.Radium.Services
{
    internal static partial class ProcedureExecutor
    {
        // Operations that require async execution
        private static bool NeedsAsync(string? op)
        {
            return string.Equals(op, "GetTextOCR", StringComparison.OrdinalIgnoreCase)
                || string.Equals(op, "GetHTML", StringComparison.OrdinalIgnoreCase);
        }

        private static (string preview, string? value) ExecuteRow(ProcOpRow row, Dictionary<string, string?> vars)
        {
            // Check if operation needs async - if so, run synchronously via Task.Run().Result
            if (NeedsAsync(row.Op))
            {
                Debug.WriteLine($"[ProcedureExecutor][ExecuteRow] Operation '{row.Op}' requires async, executing via Task");
                return ExecuteRowAsync(row, vars).GetAwaiter().GetResult();
            }

            // Delegate to shared OperationExecutor with appropriate resolution functions
            return OperationExecutor.ExecuteOperation(
                row.Op,
                resolveArg1Element: () => ResolveElement(row.Arg1, vars),
                resolveArg1String: () => ResolveString(row.Arg1, vars),
                resolveArg2String: () => ResolveString(row.Arg2, vars),
                resolveArg3String: () => ResolveString(row.Arg3, vars),
                elementCache: _elementCache
            );
        }

        private static async Task<(string preview, string? value)> ExecuteRowAsync(ProcOpRow row, Dictionary<string, string?> vars)
        {
            // Delegate to async OperationExecutor for operations that need it
            return await OperationExecutor.ExecuteOperationAsync(
                row.Op,
                resolveArg1Element: () => ResolveElement(row.Arg1, vars),
                resolveArg1String: () => ResolveString(row.Arg1, vars),
                resolveArg2String: () => ResolveString(row.Arg2, vars),
                resolveArg3String: () => ResolveString(row.Arg3, vars),
                elementCache: _elementCache
            );
        }

        private static (string preview, string? value) ExecuteElemental(ProcOpRow row, Dictionary<string, string?> vars)
        {
            // Redirect to ExecuteRow which now uses OperationExecutor
            return ExecuteRow(row, vars);
        }
    }
}

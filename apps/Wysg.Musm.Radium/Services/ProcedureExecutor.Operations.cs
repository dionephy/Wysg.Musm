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
            var totalSw = Stopwatch.StartNew();
            Debug.WriteLine($"[ProcedureExecutor][ExecuteRow] START Op='{row.Op}', Arg1.Type='{row.Arg1.Type}', Arg1.Value='{row.Arg1.Value}'");
            
            // Check if operation needs async - if so, run synchronously via Task.Run().Result
            if (NeedsAsync(row.Op))
            {
                Debug.WriteLine($"[ProcedureExecutor][ExecuteRow] Operation '{row.Op}' requires async, executing via Task");
                var result = ExecuteRowAsync(row, vars).GetAwaiter().GetResult();
                totalSw.Stop();
                Debug.WriteLine($"[ProcedureExecutor][ExecuteRow] END Op='{row.Op}' (async), total={totalSw.ElapsedMilliseconds}ms");
                return result;
            }

            // Time element resolution separately
            // Use single-attempt resolution for GetTextOnce to fail fast
            AutomationElement? resolvedElement = null;
            long resolveMs = 0;
            if (row.Arg1.Type == "Element")
            {
                var resolveSw = Stopwatch.StartNew();
                // GetTextOnce uses single-attempt resolution (no retries) for faster failure
                resolvedElement = row.Op == "GetTextOnce" 
                    ? ResolveElementOnce(row.Arg1, vars) 
                    : ResolveElement(row.Arg1, vars);
                resolveSw.Stop();
                resolveMs = resolveSw.ElapsedMilliseconds;
                Debug.WriteLine($"[ProcedureExecutor][ExecuteRow] Element resolution for '{row.Arg1.Value}' took {resolveMs}ms, element={(resolvedElement != null ? "found" : "null")}");
            }

            // Time operation execution
            var opSw = Stopwatch.StartNew();
            
            // Delegate to shared OperationExecutor with appropriate resolution functions
            var opResult = OperationExecutor.ExecuteOperation(
                row.Op,
                resolveArg1Element: () => resolvedElement ?? (row.Op == "GetTextOnce" 
                    ? ResolveElementOnce(row.Arg1, vars) 
                    : ResolveElement(row.Arg1, vars)),
                resolveArg1String: () => ResolveString(row.Arg1, vars),
                resolveArg2String: () => ResolveString(row.Arg2, vars),
                resolveArg3String: () => ResolveString(row.Arg3, vars),
                elementCache: _elementCache
            );
            
            opSw.Stop();
            totalSw.Stop();
            
            Debug.WriteLine($"[ProcedureExecutor][ExecuteRow] END Op='{row.Op}', resolveMs={resolveMs}, operationMs={opSw.ElapsedMilliseconds}, total={totalSw.ElapsedMilliseconds}ms, preview='{opResult.preview?.Substring(0, Math.Min(opResult.preview?.Length ?? 0, 50))}'");
            
            return opResult;
        }

        private static async Task<(string preview, string? value)> ExecuteRowAsync(ProcOpRow row, Dictionary<string, string?> vars)
        {
            var totalSw = Stopwatch.StartNew();
            
            // Delegate to async OperationExecutor for operations that need it
            var result = await OperationExecutor.ExecuteOperationAsync(
                row.Op,
                resolveArg1Element: () => ResolveElement(row.Arg1, vars),
                resolveArg1String: () => ResolveString(row.Arg1, vars),
                resolveArg2String: () => ResolveString(row.Arg2, vars),
                resolveArg3String: () => ResolveString(row.Arg3, vars),
                elementCache: _elementCache
            );
            
            totalSw.Stop();
            Debug.WriteLine($"[ProcedureExecutor][ExecuteRowAsync] Op='{row.Op}' completed in {totalSw.ElapsedMilliseconds}ms");
            
            return result;
        }

        private static (string preview, string? value) ExecuteElemental(ProcOpRow row, Dictionary<string, string?> vars)
        {
            // Redirect to ExecuteRow which now uses OperationExecutor
            return ExecuteRow(row, vars);
        }
    }
}

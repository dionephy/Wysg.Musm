using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using FlaUI.Core.AutomationElements;

namespace Wysg.Musm.Radium.Services
{
    internal static partial class ProcedureExecutor
    {
        private static (string preview, string? value) ExecuteRow(ProcOpRow row, Dictionary<string, string?> vars)
        {
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

        private static (string preview, string? value) ExecuteElemental(ProcOpRow row, Dictionary<string, string?> vars)
        {
            // Redirect to ExecuteRow which now uses OperationExecutor
            return ExecuteRow(row, vars);
        }
    }
}

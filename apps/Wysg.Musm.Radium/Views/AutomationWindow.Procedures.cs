using System;
using System.Net.Http;
using System.Text;

namespace Wysg.Musm.Radium.Views
{
    // Split into partials for maintainability:
    // - AutomationWindow.Procedures.Encoding.cs (encoding helpers, BOM checks, charset repair)
    // - AutomationWindow.Procedures.Http.cs (Http client, smart HTML fetch)
    // - AutomationWindow.Procedures.Model.cs (ArgKind, ProcArg, ProcOpRow, ProcStore)
    // - AutomationWindow.Procedures.Exec.cs (execution engine, UI handlers)
    public partial class AutomationWindow
    {
        // All implementation moved to partial files listed above
    }
}

using System;
using System.Net.Http;
using System.Text;

namespace Wysg.Musm.Radium.Views
{
    // Split into partials for maintainability:
    // - SpyWindow.Procedures.Encoding.cs (encoding helpers, BOM checks, charset repair)
    // - SpyWindow.Procedures.Http.cs (Http client, smart HTML fetch)
    // - SpyWindow.Procedures.Model.cs (ArgKind, ProcArg, ProcOpRow, ProcStore)
    // - SpyWindow.Procedures.Exec.cs (execution engine, UI handlers)
    public partial class SpyWindow
    {
        // All implementation moved to partial files listed above
    }
}

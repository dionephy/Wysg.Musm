using System.Collections.Generic;

namespace Wysg.Musm.Radium.Services
{
    internal static partial class ProcedureExecutor
    {
        private sealed class ProcStore 
        { 
            public Dictionary<string, List<ProcOpRow>> Methods { get; set; } = new(); 
        }

        private sealed class ProcOpRow
        {
            public string Op { get; set; } = string.Empty;
            public ProcArg Arg1 { get; set; } = new();
            public ProcArg Arg2 { get; set; } = new();
            public ProcArg Arg3 { get; set; } = new();
            public bool Arg1Enabled { get; set; } = true;
            public bool Arg2Enabled { get; set; } = true;
            public bool Arg3Enabled { get; set; } = false;
            public string? OutputVar { get; set; }
            public string? OutputPreview { get; set; }
        }

        private sealed class ProcArg 
        { 
            public string Type { get; set; } = "String"; 
            public string? Value { get; set; } 
        }

        private enum ArgKind 
        { 
            Element, 
            String, 
            Number, 
            Var 
        }
    }
}

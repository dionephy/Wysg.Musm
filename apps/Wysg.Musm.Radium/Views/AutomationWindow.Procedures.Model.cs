using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Wysg.Musm.Radium.Views
{
    public partial class AutomationWindow
    {
        private enum ArgKind { Element, String, Number, Var }

        private sealed class ProcArg : INotifyPropertyChanged
        {
            private string _type = nameof(ArgKind.String);
            private string? _value;
            public string Type { get => _type; set => SetField(ref _type, value); }
            public string? Value { get => _value; set => SetField(ref _value, value); }
            public event PropertyChangedEventHandler? PropertyChanged;
            private void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
            private bool SetField<T>(ref T f, T v, [CallerMemberName] string? n = null)
            { if (EqualityComparer<T>.Default.Equals(f, v)) return false; f = v; OnPropertyChanged(n); return true; }
        }

        private sealed class ProcOpRow : INotifyPropertyChanged
        {
            private string _op = string.Empty;
            private ProcArg _arg1 = new();
            private ProcArg _arg2 = new();
            private ProcArg _arg3 = new();
            private bool _arg1Enabled = true;
            private bool _arg2Enabled = true;
            private bool _arg3Enabled = false;
            private string? _outputVar;
            private string? _outputPreview;
            public string Op { get => _op; set => SetField(ref _op, value); }
            public ProcArg Arg1 { get => _arg1; set => SetField(ref _arg1, value); }
            public ProcArg Arg2 { get => _arg2; set => SetField(ref _arg2, value); }
            public ProcArg Arg3 { get => _arg3; set => SetField(ref _arg3, value); }
            public bool Arg1Enabled { get => _arg1Enabled; set => SetField(ref _arg1Enabled, value); }
            public bool Arg2Enabled { get => _arg2Enabled; set => SetField(ref _arg2Enabled, value); }
            public bool Arg3Enabled { get => _arg3Enabled; set => SetField(ref _arg3Enabled, value); }
            public string? OutputVar { get => _outputVar; set => SetField(ref _outputVar, value); }
            public string? OutputPreview { get => _outputPreview; set => SetField(ref _outputPreview, value); }
            public event PropertyChangedEventHandler? PropertyChanged;
            private void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
            private bool SetField<T>(ref T f, T v, [CallerMemberName] string? n = null)
            { if (EqualityComparer<T>.Default.Equals(f, v)) return false; f = v; OnPropertyChanged(n); return true; }
        }

        private sealed class ProcStore { public Dictionary<string, List<ProcOpRow>> Methods { get; set; } = new(); }
    }
}

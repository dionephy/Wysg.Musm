using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Diagnostics;
using System.Threading;
using System.ComponentModel;
using Wysg.Musm.Infrastructure.ViewModels;
using Wysg.Musm.Radium.Services;
using Wysg.Musm.Radium.Services.Procedures;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// MainViewModel (core partial).
    ///
    /// The original file was very large. It has been split into multiple partial class files for readability
    /// and maintainability. Each partial groups a cohesive concern:
    ///   MainViewModel.cs (this file)          : Core fields, constructor, high-level status helpers
    ///   MainViewModel.CurrentStudy.cs         : Current study metadata fetch & persistence
    ///   MainViewModel.Editor.cs               : Current editor (reportified) text + JSON sync logic
    ///   MainViewModel.PreviousStudies.cs      : Previous studies loading, selection, JSON sync
    ///   MainViewModel.ReportifyHelpers.cs     : Reportify / dereportify helpers & regex utilities
    ///   MainViewModel.Commands.cs             : Commands, delegate command, automation sequences
    ///
    /// Splitting keeps cross-cutting private fields centralized here so other partials only focus on logic.
    /// New developers: start reading from this file, then navigate to the specific concern you need.
    /// </summary>
    public partial class MainViewModel : BaseViewModel
    {
        // ------------------------------------------------------------------
        // Core service dependencies (injected)
        // ------------------------------------------------------------------
        private readonly IPhraseService _phrases;
        private readonly ITenantContext _tenant;
        private readonly IPhraseCache _cache;
        private readonly IHotkeyService _hotkeys; // new: hotkey service for editor completion
        private readonly PacsService _pacs = new();
        private readonly ICentralDataSourceProvider? _centralProvider; // (reserved for future use)
        private readonly IRadStudyRepository? _studyRepo;
        private readonly INewStudyProcedure? _newStudyProc;
        private readonly IRadiumLocalSettings? _localSettings;
        private readonly ILockStudyProcedure? _lockStudyProc;

        // ------------------------------------------------------------------
        // Status / UI flags
        // ------------------------------------------------------------------
        private string _statusText = "Ready"; public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
        private bool _statusIsError; public bool StatusIsError { get => _statusIsError; set => SetProperty(ref _statusIsError, value); }
        private void SetStatus(string message, bool isError = false) { StatusText = message; StatusIsError = isError; }

        // ------------------------------------------------------------------
        // Constructor wires commands (definitions in Commands partial) & loads caches in Editor partial.
        // ------------------------------------------------------------------
        public MainViewModel(
            IPhraseService phrases,
            ITenantContext tenant,
            IPhraseCache cache,
            IHotkeyService hotkeys,
            IRadStudyRepository? studyRepo = null,
            INewStudyProcedure? newStudyProc = null,
            IRadiumLocalSettings? localSettings = null,
            ILockStudyProcedure? lockStudyProc = null)
        {
            _phrases = phrases; _tenant = tenant; _cache = cache; _hotkeys = hotkeys;
            _studyRepo = studyRepo; _newStudyProc = newStudyProc; _localSettings = localSettings; _lockStudyProc = lockStudyProc;
            PreviousStudies = new ObservableCollection<PreviousStudyTab>();
            InitializeCommands(); // implemented in Commands partial
        }

        // ------------------------------------------------------------------
        // Internal wrappers (used by automation / procedures) kept here so they remain visible from other partials
        // ------------------------------------------------------------------
        internal async Task FetchCurrentStudyAsyncInternal() => await FetchCurrentStudyAsync();
        internal void UpdateCurrentStudyLabelInternal() => UpdateCurrentStudyLabel();
        internal void SetStatusInternal(string msg, bool err = false) => SetStatus(msg, err);
    }
}
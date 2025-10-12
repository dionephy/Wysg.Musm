using System.Collections.ObjectModel;
using System.Windows.Input;
using Wysg.Musm.Infrastructure.ViewModels;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.ViewModels
{
    public sealed class StudynameTechniqueViewModel : BaseViewModel
    {
        private readonly ITechniqueRepository _repo;
        public StudynameTechniqueViewModel(ITechniqueRepository repo) { _repo = repo; }
        public string Header { get => _header; set => SetProperty(ref _header, value); }
        private string _header = string.Empty;
        public ObservableCollection<ComboRow> Combinations { get; } = new();
        public ComboRow? SelectedCombination { get => _sel; set => SetProperty(ref _sel, value); }
        private ComboRow? _sel;
        public ICommand SetDefaultCommand => new RelayCommand(async _ => { if (_studynameId.HasValue && SelectedCombination != null) await _repo.SetDefaultForStudynameAsync(_studynameId.Value, SelectedCombination.CombinationId); });
        private long? _studynameId;
        public long? StudynameIdPublic => _studynameId;
        public async void Initialize(long studynameId, string studyname)
        {
            _studynameId = studynameId; Header = $"Studyname: {studyname}";
            await ReloadAsync();
        }
        public async System.Threading.Tasks.Task ReloadAsync()
        {
            if (!_studynameId.HasValue) return;
            Combinations.Clear();
            var rows = await _repo.GetCombinationsForStudynameAsync(_studynameId.Value);
            foreach (var c in rows) Combinations.Add(new ComboRow { CombinationId = c.CombinationId, Display = c.Display, IsDefault = c.IsDefault });
        }
        public sealed class ComboRow : BaseViewModel { public long CombinationId { get; set; } public string Display { get; set; } = string.Empty; public bool IsDefault { get; set; } }
        private sealed class RelayCommand : ICommand
        {
            private readonly System.Action<object?> _exec; private readonly System.Predicate<object?>? _can;
            public RelayCommand(System.Action<object?> exec, System.Predicate<object?>? can = null) { _exec = exec; _can = can; }
            public bool CanExecute(object? parameter) => _can?.Invoke(parameter) ?? true;
            public void Execute(object? parameter) => _exec(parameter);
            public event System.EventHandler? CanExecuteChanged; public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, System.EventArgs.Empty);
        }
    }
}

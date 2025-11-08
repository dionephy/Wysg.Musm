using System.Windows;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Views
{
    public partial class PhraseExtractionWindow : Window
    {
        private static PhraseExtractionWindow? _instance;
        private ICSharpCode.AvalonEdit.TextEditor? _editor;

        public PhraseExtractionWindow()
        {
            InitializeComponent();
            Closed += OnClosed;
        }

        public static PhraseExtractionWindow GetOrCreateInstance()
        {
            if (_instance == null || !_instance.IsLoaded)
            {
                _instance = new PhraseExtractionWindow();
            }
            return _instance;
        }

        private void OnClosed(object? sender, System.EventArgs e)
        {
            _instance = null;
        }

        public void Load(string header, string findings, string conclusion)
        {
            if (DataContext is PhraseExtractionViewModel vm)
                vm.LoadFromDeReportified(header, findings, conclusion);
        }

        private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            // Setup selection change monitoring for the underlying AvalonEdit TextEditor
            // DuplicateEditor is the EditorControl, we need to access its inner MusmEditor
            if (DuplicateEditor != null && DataContext is PhraseExtractionViewModel vm)
            {
                // Access the MusmEditor inside EditorControl
                _editor = DuplicateEditor.FindName("Editor") as ICSharpCode.AvalonEdit.TextEditor;
                if (_editor != null)
                {
                    // Monitor selection changes in the underlying TextEditor
                    _editor.TextArea.SelectionChanged += (s, ev) =>
                    {
                        // Update ViewModel with selected text
                        try
                        {
                            var selectedText = _editor.SelectedText ?? string.Empty;
                            // Use Dispatcher to ensure we're on UI thread
                            Dispatcher.BeginInvoke(new System.Action(() =>
                            {
                                if (DataContext is PhraseExtractionViewModel viewModel)
                                {
                                    viewModel.SelectedText = selectedText;
                                }
                            }), System.Windows.Threading.DispatcherPriority.Background);
                        }
                        catch (System.Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[PhraseExtractionWindow] Selection changed error: {ex.Message}");
                        }
                    };
                    
                    System.Diagnostics.Debug.WriteLine("[PhraseExtractionWindow] Selection change handler attached successfully");

                    // Monitor ViewModel SelectedText property to clear editor selection when it becomes empty
                    vm.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(PhraseExtractionViewModel.SelectedText))
                        {
                            if (string.IsNullOrEmpty(vm.SelectedText) && _editor != null)
                            {
                                // Clear editor selection
                                Dispatcher.BeginInvoke(new System.Action(() =>
                                {
                                    try
                                    {
                                        if (_editor.TextArea != null && _editor.TextArea.Selection != null)
                                        {
                                            // Clear selection by setting it to an empty range at offset 0
                                            _editor.TextArea.Selection = ICSharpCode.AvalonEdit.Editing.Selection.Create(_editor.TextArea, 0, 0);
                                            _editor.TextArea.Caret.Offset = 0;
                                        }
                                    }
                                    catch (System.Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[PhraseExtractionWindow] Clear selection error: {ex.Message}");
                                    }
                                }), System.Windows.Threading.DispatcherPriority.Background);

                                // Notify ViewModel that selection was cleared
                                vm.OnEditorSelectionCleared();
                            }
                        }
                    };
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[PhraseExtractionWindow] Failed to find Editor in DuplicateEditor");
                }
            }

            // Copy phrase snapshot and semantic tags from MainWindow's MainViewModel to this ViewModel
            // This enables phrase coloring in the duplicate editor
            try
            {
                var mainWindow = Application.Current.MainWindow;
                if (mainWindow?.DataContext is MainViewModel mainVM)
                {
                    // Bind to MainViewModel's phrase properties via reflection to avoid tight coupling
                    var currentPhraseSnapshotProp = mainVM.GetType().GetProperty("CurrentPhraseSnapshot");
                    var phraseSemanticTagsProp = mainVM.GetType().GetProperty("PhraseSemanticTags");
                    
                    if (currentPhraseSnapshotProp != null)
                    {
                        var snapshot = currentPhraseSnapshotProp.GetValue(mainVM) as System.Collections.Generic.IReadOnlyList<string>;
                        if (snapshot != null)
                        {
                            // Set the phrase snapshot on the editor directly
                            DuplicateEditor.PhraseSnapshot = snapshot;
                            System.Diagnostics.Debug.WriteLine($"[PhraseExtractionWindow] Phrase snapshot copied: {snapshot.Count} phrases");
                        }
                    }
                    
                    if (phraseSemanticTagsProp != null)
                    {
                        var semanticTags = phraseSemanticTagsProp.GetValue(mainVM) as System.Collections.Generic.IReadOnlyDictionary<string, string?>;
                        if (semanticTags != null)
                        {
                            // Set the semantic tags on the editor directly
                            DuplicateEditor.PhraseSemanticTags = semanticTags;
                            System.Diagnostics.Debug.WriteLine($"[PhraseExtractionWindow] Semantic tags copied: {semanticTags.Count} tags");
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PhraseExtractionWindow] Failed to copy phrase data: {ex.Message}");
            }
        }
    }
}

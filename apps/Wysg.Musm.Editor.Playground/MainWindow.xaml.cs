using System.Windows;
using Wysg.Musm.Editor.Controls;

namespace Wysg.Musm.Editor.Playground
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // The XAML should name the editor control: x:Name="Editor"
            var vm = new MainViewModel(Editor);
            DataContext = vm;

            // No debug seeding, no anchors — production-clean startup.
            Loaded += (_, __) =>
            {
                Editor.SnippetProvider = new WordListCompletionProvider(new[]
{
    "thalamus", "microangiopathy",
    "no", "no acute intracranial abnormality", "no acute intracranial hemorrhage",
    "infarction", "hydrocephalus"
});
                Editor.MinCharsForSuggest = 2;
                Editor.AutoSuggestOnTyping = true;

                // If you want a basic word-list completion for smoke tests, you can re-enable:
                // Editor.SnippetProvider = new WordListCompletionProvider(new [] {"thalamus","microangiopathy","no acute intracranial abnormality"});
                // Editor.MinCharsForSuggest = 2;
                // Editor.AutoSuggestOnTyping = true;
            };


        }

        // Add this handler (must match XAML Click="OnForceGhost")
        private async void OnForceGhost(object sender, RoutedEventArgs e)
        {
            // Preferred: call VM to hit the API and update editor ghosts
            if (DataContext is MainViewModel vm)
            {
                await vm.ForceServerGhostsAsync();
                return;
            }

            // Fallback: direct seed to prove renderer path
            Editor.DebugSeedGhosts();
        }
    }
}

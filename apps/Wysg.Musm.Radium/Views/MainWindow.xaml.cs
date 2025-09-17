using System.Windows;
using Wysg.Musm.Radium.ViewModels;
using Wysg.Musm.Editor.Controls;

namespace Wysg.Musm.Radium.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void InitEditor(MainViewModel vm, EditorControl ctl)
        {
            vm.InitializeEditor(ctl);
            ctl.EnableGhostDebugAnchors(false);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;
            InitEditor(vm, EditorHeader);
            InitEditor(vm, EditorFindings);
            InitEditor(vm, EditorConclusion);
            InitEditor(vm, EditorPreviousHeader);
            InitEditor(vm, EditorSuggestion);
            InitEditor(vm, EditorPreviousFindings);
        }

        private void OnForceGhost(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                // Force ghost refresh on a primary editor (Findings)
                EditorFindings.DebugSeedGhosts();
            }
        }

        private void OnOpenSettings(object sender, RoutedEventArgs e)
        {
            var win = new SettingsWindow { Owner = this };
            win.ShowDialog();
        }
    }
}
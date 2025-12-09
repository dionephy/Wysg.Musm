using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Wysg.Musm.Radium.Views.SettingsTabs
{
    public partial class AutomationSettingsTab : UserControl
    {
        public AutomationSettingsTab()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Initialize automation lists via parent window
            var settingsWindow = Window.GetWindow(this) as SettingsWindow;
            if (settingsWindow != null)
            {
                // Pass ListBox references to parent for initialization
                settingsWindow.InitializeAutomationListBoxes(lstNewStudy, lstAddStudy, lstLibrary, 
                    lstShortcutOpenNew, lstSendReport,
                    lstSendReportPreview, lstShortcutSendReportPreview, lstTest);
                
                // Set current PACS key
                txtCurrentPacsKey.Text = settingsWindow.CurrentPacsKey;
            }
        }

        private void OnProcDrag(object sender, MouseEventArgs e)
        {
            var settingsWindow = Window.GetWindow(this) as SettingsWindow;
            settingsWindow?.OnProcDrag(sender, e);
        }

        private void OnProcDrop(object sender, DragEventArgs e)
        {
            var settingsWindow = Window.GetWindow(this) as SettingsWindow;
            settingsWindow?.OnProcDrop(sender, e);
        }

        private void OnListDragLeave(object sender, DragEventArgs e)
        {
            var settingsWindow = Window.GetWindow(this) as SettingsWindow;
            settingsWindow?.OnListDragLeave(sender, e);
        }

        private void OnRemoveModuleClick(object sender, RoutedEventArgs e)
        {
            var settingsWindow = Window.GetWindow(this) as SettingsWindow;
            settingsWindow?.OnRemoveModuleClick(sender, e);
        }

        private void OnOpenSpy(object sender, RoutedEventArgs e)
        {
            var settingsWindow = Window.GetWindow(this) as SettingsWindow;
            settingsWindow?.OnOpenSpy(sender, e);
        }
    }
}

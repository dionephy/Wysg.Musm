using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Wysg.Musm.Radium.Views.SettingsTabs
{
    public partial class KeyboardSettingsTab : UserControl
    {
        public KeyboardSettingsTab()
        {
            InitializeComponent();
        }

        private void OnHotkeyTextBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Bubble to parent SettingsWindow
            var settingsWindow = Window.GetWindow(this) as SettingsWindow;
            settingsWindow?.OnHotkeyTextBoxPreviewKeyDown(sender, e);
        }
    }
}

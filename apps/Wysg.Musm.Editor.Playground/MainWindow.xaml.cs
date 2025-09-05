using System.Windows;

namespace Wysg.Musm.Editor.Playground;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}

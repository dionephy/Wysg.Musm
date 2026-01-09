using System.Windows;
using Wysg.Musm.LlamaClient.ViewModels;

namespace Wysg.Musm.LlamaClient.Views;

/// <summary>
/// Interaction logic for McpConfigWindow.xaml
/// </summary>
public partial class McpConfigWindow : Window
{
    public McpConfigWindow()
    {
        InitializeComponent();
        DataContext = new McpConfigViewModel();
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        // Optional: Save on close
        if (DataContext is McpConfigViewModel vm)
        {
            vm.SaveCommand.Execute(null);
        }
    }
}
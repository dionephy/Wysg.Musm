using System.Windows;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Views
{
    public partial class SignUpWindow : Window
    {
        public SignUpWindow(SignUpViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Wysg.Musm.Radium.Controls
{
    public partial class StatusPanel : UserControl
    {
        public StatusPanel()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        public static readonly DependencyProperty TopContentProperty =
            DependencyProperty.Register(nameof(TopContent), typeof(object), typeof(StatusPanel), new PropertyMetadata(null));

        public object? TopContent
        {
            get => GetValue(TopContentProperty);
            set => SetValue(TopContentProperty, value);
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is ViewModels.MainViewModel oldVm)
            {
                oldVm.PropertyChanged -= OnViewModelPropertyChanged;
            }
            
            if (e.NewValue is ViewModels.MainViewModel newVm)
            {
                newVm.PropertyChanged += OnViewModelPropertyChanged;
                UpdateStatusText(newVm.StatusText, newVm.StatusIsError);
            }
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModels.MainViewModel.StatusText) ||
                e.PropertyName == nameof(ViewModels.MainViewModel.StatusIsError))
            {
                if (sender is ViewModels.MainViewModel vm)
                {
                    UpdateStatusText(vm.StatusText, vm.StatusIsError);
                }
            }
        }

        private void UpdateStatusText(string text, bool isError)
        {
            if (richStatusBox == null) return;

            var doc = richStatusBox.Document;
            doc.Blocks.Clear();

            if (string.IsNullOrEmpty(text))
            {
                richStatusBox.ScrollToEnd();
                return;
            }

            // Split text into lines and colorize line-by-line
            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
            var para = new Paragraph { Margin = new Thickness(0), LineHeight = 1 };

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                
                // Detect completion lines (containing "completed successfully" or starting with checkmark)
                bool isCompletionLine = line.IndexOf("completed successfully", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                        line.TrimStart().StartsWith("?", System.StringComparison.Ordinal);
                
                // Detect error lines (containing "error", "failed", "exception", etc.)
                bool isErrorLine = !isCompletionLine && (
                    line.IndexOf("error", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    line.IndexOf("failed", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    line.IndexOf("exception", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    line.IndexOf("validation failed", System.StringComparison.OrdinalIgnoreCase) >= 0);

                // Choose color: green for completion, red for error, default gray otherwise
                var run = new Run(line)
                {
                    Foreground = isCompletionLine ? new SolidColorBrush(Color.FromRgb(0x90, 0xEE, 0x90)) :  // Light green
                                 isErrorLine ? new SolidColorBrush(Color.FromRgb(0xFF, 0x5A, 0x5A)) :       // Red
                                               new SolidColorBrush(Color.FromRgb(0xD0, 0xD0, 0xD0))        // Default gray
                };
                para.Inlines.Add(run);
                
                // Only add line break if not the last line
                if (i < lines.Length - 1)
                {
                    para.Inlines.Add(new LineBreak());
                }
            }

            doc.Blocks.Add(para);
            
            // Auto-scroll to end
            richStatusBox.ScrollToEnd();
        }
    }
}

using System;
using System.Windows;
using System.Windows.Threading;

namespace Wysg.Musm.Radium.Views
{
    public partial class HighlightOverlay : Window
    {
        private readonly DispatcherTimer _timer;
        public HighlightOverlay()
        {
            InitializeComponent();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
            _timer.Tick += (_, __) => { _timer.Stop(); this.Hide(); };
        }

        public void ShowForRect(System.Drawing.Rectangle r)
        {
            this.Left = r.Left;
            this.Top = r.Top;
            this.Width = Math.Max(1, r.Width);
            this.Height = Math.Max(1, r.Height);
            this.Show();
            _timer.Stop();
            _timer.Start();
        }
    }
}

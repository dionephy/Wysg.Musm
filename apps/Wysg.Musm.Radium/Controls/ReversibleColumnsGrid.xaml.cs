using System.Windows;
using System.Windows.Controls;

namespace Wysg.Musm.Radium.Controls
{
    public partial class ReversibleColumnsGrid : UserControl
    {
        public ReversibleColumnsGrid()
        {
            InitializeComponent();
            Loaded += (_, __) => ApplyReverse();
        }

        public static readonly DependencyProperty LeftProperty =
            DependencyProperty.Register(nameof(Left), typeof(object), typeof(ReversibleColumnsGrid), new PropertyMetadata(null));

        public static readonly DependencyProperty RightProperty =
            DependencyProperty.Register(nameof(Right), typeof(object), typeof(ReversibleColumnsGrid), new PropertyMetadata(null));

        public static readonly DependencyProperty ReverseProperty =
            DependencyProperty.Register(nameof(Reverse), typeof(bool), typeof(ReversibleColumnsGrid), new PropertyMetadata(false, OnReverseChanged));

        public object? Left
        {
            get => GetValue(LeftProperty);
            set => SetValue(LeftProperty, value);
        }

        public object? Right
        {
            get => GetValue(RightProperty);
            set => SetValue(RightProperty, value);
        }

        public bool Reverse
        {
            get => (bool)GetValue(ReverseProperty);
            set => SetValue(ReverseProperty, value);
        }

        private static void OnReverseChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ReversibleColumnsGrid self)
                self.ApplyReverse();
        }

        private void ApplyReverse()
        {
            if (PART_Left == null || PART_Right == null) return;
            var leftCol = Grid.GetColumn(PART_Left);
            var rightCol = Grid.GetColumn(PART_Right);
            if (Reverse)
            {
                Grid.SetColumn(PART_Left, 2);
                Grid.SetColumn(PART_Right, 0);
            }
            else
            {
                Grid.SetColumn(PART_Left, 0);
                Grid.SetColumn(PART_Right, 2);
            }
        }
    }
}

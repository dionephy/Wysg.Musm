using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using Wysg.Musm.Radium.ViewModels;
using Wysg.Musm.Editor.Controls;

namespace Wysg.Musm.Radium.Views
{
    public partial class MainWindow : Window
    {
        private bool _reportsReversed = false;
        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            SizeChanged += OnWindowSizeChanged;
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
            InitEditor(vm, EditorPreviousFindings);

            // Ensure initial sizing and positioning of the editor grid
            UpdateGridCenterSize();
            UpdateGridCenterPositioning();
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateGridCenterSize();
            UpdateGridCenterPositioning();
        }

        private void UpdateGridCenterSize()
        {
            if (gridCenter?.Parent is not Grid parent) return;

            int col = Grid.GetColumn(gridCenter);
            int row = Grid.GetRow(gridCenter);

            // available size inside the cell (excluding borders of other rows/cols)
            double availW = parent.ColumnDefinitions[col].ActualWidth;
            double availH = parent.RowDefinitions[row].ActualHeight;

            if (double.IsNaN(availW) || double.IsNaN(availH) || availW <= 0 || availH <= 0)
                return;

            double side = Math.Min(availW, availH);
            if (side < 0) side = 0;

            gridCenter.Width = side;
            gridCenter.Height = side;
        }

        private void UpdateGridCenterPositioning()
        {
            // Landscape = wider than tall
            bool isLandscape = ActualWidth >= ActualHeight;
            if (isLandscape)
            {
                gridCenter.VerticalAlignment = VerticalAlignment.Center;
                gridCenter.HorizontalAlignment = (tglAlignRight?.IsChecked == true)
                    ? HorizontalAlignment.Right
                    : HorizontalAlignment.Left;
            }
            else
            {
                // Portrait: keep it centered horizontally
                gridCenter.HorizontalAlignment = HorizontalAlignment.Center;
                gridCenter.VerticalAlignment = VerticalAlignment.Center;
            }
        }

        private void OnAlignRightToggled(object sender, RoutedEventArgs e)
        {
            UpdateGridCenterPositioning();
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

        private void OnReverseReportsChecked(object sender, RoutedEventArgs e)
        {
            // Toggle _reportsReversed
            _reportsReversed = chkReverseReports.IsChecked == true;
            SwapReportEditors(_reportsReversed);
        }

        private void SwapReportEditors(bool reversed)
        {
            // 좌우 영역의 row를 서로 바꾼다
            var children = gridCenter.Children;
            var currentReportGrid = children[0] as UIElement;
            var previousReportGrid = children[2] as UIElement;
            if (currentReportGrid == null || previousReportGrid == null) return;
            if (reversed)
            {
                // 이전 보고서 영역을 왼쪽, 현재 보고서 영역을 오른쪽으로 이동
                Grid.SetColumn(currentReportGrid, 2); // Current Report Grid -> 오른쪽
                Grid.SetColumn(previousReportGrid, 0); // Previous Report Grid -> 왼쪽
            }
            else
            {
                // 현재 보고서 영역을 왼쪽, 이전 보고서 영역을 오른쪽으로 이동
                Grid.SetColumn(currentReportGrid, 0); // Current Report Grid -> 왼쪽
                Grid.SetColumn(previousReportGrid, 2); // Previous Report Grid -> 오른쪽
            }
        }
    }
}
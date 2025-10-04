using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using FlaUI.Core.AutomationElements;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.Views
{
    /// <summary>
    /// Partial: (Currently minimal) ancestry tree support.
    /// </summary>
    public partial class SpyWindow
    {
        private class TreeNode
        {
            public string? Name { get; set; }
            public string? ClassName { get; set; }
            public int? ControlTypeId { get; set; }
            public string? AutomationId { get; set; }
            public List<TreeNode> Children { get; } = new();
            public int Level { get; set; }
            public Brush? Highlight { get; set; }
        }
        private TreeNode? _ancestryRoot;

        private void ShowAncestryTree(UiBookmarks.Bookmark b)
        {
            if (_chkEnableTree?.IsChecked != true) { tvAncestry.ItemsSource = System.Array.Empty<object>(); return; }
            tvAncestry.ItemsSource = System.Array.Empty<object>();
        }

        // Handler required by XAML (currently no detailed node inspection implemented)
        private void OnAncestrySelected(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is null) { txtNodeProps.Text = string.Empty; return; }
            txtNodeProps.Text = string.Empty; // Placeholder
        }

        private static void PopulateChildrenTree(TreeNode node, AutomationElement element, int maxDepth)
        {
            if (maxDepth <= 0) return;
            try
            {
                node.Children.Clear();
                var kids = element.FindAllChildren();
                var limit = System.Math.Min(kids.Length, 100);
                for (int i = 0; i < limit; i++)
                {
                    var k = kids[i];
                    var childNode = new TreeNode
                    {
                        Name = Safe(k, e => e.Name),
                        ClassName = Safe(k, e => e.ClassName),
                        ControlTypeId = Safe(k, e => (int?)e.Properties.ControlType.Value),
                        AutomationId = Safe(k, e => e.AutomationId),
                        Level = node.Level + 1
                    };
                    node.Children.Add(childNode);
                    PopulateChildrenTree(childNode, k, maxDepth - 1);
                }
            }
            catch (System.Exception ex) { Debug.WriteLine("Tree populate error: " + ex.Message); }
            static T? Safe<T>(AutomationElement e, System.Func<AutomationElement, T?> f) { try { return f(e); } catch { return default; } }
        }
    }
}

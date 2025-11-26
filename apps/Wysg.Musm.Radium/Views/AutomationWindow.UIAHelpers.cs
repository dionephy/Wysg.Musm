using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Wysg.Musm.Radium.Views
{
    public partial class AutomationWindow
    {
        private static TParent? FindParent<TParent>(DependencyObject? child) where TParent : DependencyObject
        {
            while (child != null)
            {
                var parent = VisualTreeHelper.GetParent(child);
                if (parent is TParent tp) return tp;
                child = parent;
            }
            return null;
        }
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject d) where T : DependencyObject
        {
            if (d == null) yield break;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(d); i++)
            {
                var c = VisualTreeHelper.GetChild(d, i);
                if (c is T t) yield return t;
                foreach (var cc in FindVisualChildren<T>(c)) yield return cc;
            }
        }
    }
}

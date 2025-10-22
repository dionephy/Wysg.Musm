using System;
using System.Collections.Generic;
using FlaUI.Core.AutomationElements;

namespace Wysg.Musm.Radium.Services
{
    /// <summary>
    /// OperationExecutor partial class: UI element reading and header parsing helpers.
    /// Contains helper methods for reading element text, parsing table headers, and extracting cell values.
    /// </summary>
    internal static partial class OperationExecutor
    {
        #region Header Helper Methods

        private static string NormalizeHeader(string h)
        {
            h = (h ?? string.Empty).Trim();
            if (string.Equals(h, "Accession", StringComparison.OrdinalIgnoreCase)) return "Accession No.";
            if (string.Equals(h, "Study Description", StringComparison.OrdinalIgnoreCase)) return "Study Desc";
            if (string.Equals(h, "Institution Name", StringComparison.OrdinalIgnoreCase)) return "Institution";
            if (string.Equals(h, "BirthDate", StringComparison.OrdinalIgnoreCase)) return "Birth Date";
            if (string.Equals(h, "BodyPart", StringComparison.OrdinalIgnoreCase)) return "Body Part";
            return h;
        }

        private static List<string> GetHeaderTexts(AutomationElement list)
        {
            var headers = new List<string>();
            try
            {
                var kids = list.FindAllChildren();
                if (kids.Length > 0)
                {
                    var headerRow = kids[0];
                    var cells = headerRow.FindAllChildren();
                    foreach (var c in cells)
                    {
                        string txt = TryRead(c);
                        if (string.IsNullOrWhiteSpace(txt))
                        {
                            foreach (var g in c.FindAllChildren())
                            {
                                txt = TryRead(g);
                                if (!string.IsNullOrWhiteSpace(txt)) break;
                            }
                        }
                        headers.Add(string.IsNullOrWhiteSpace(txt) ? string.Empty : txt.Trim());
                    }
                }
            }
            catch { }
            return headers;
        }

        private static List<string> GetRowCellValues(AutomationElement row)
        {
            var vals = new List<string>();
            try
            {
                var children = row.FindAllChildren();
                foreach (var c in children)
                {
                    string txt = TryRead(c).Trim();
                    if (string.IsNullOrEmpty(txt))
                    {
                        foreach (var gc in c.FindAllChildren())
                        {
                            var t = TryRead(gc).Trim();
                            if (!string.IsNullOrEmpty(t))
                            {
                                txt = t;
                                break;
                            }
                        }
                    }
                    vals.Add(txt);
                }
            }
            catch { }
            return vals;
        }

        private static string TryRead(AutomationElement el)
        {
            try
            {
                var vp = el.Patterns.Value.PatternOrDefault;
                if (vp != null && vp.Value.TryGetValue(out var v) && !string.IsNullOrWhiteSpace(v)) return v;
            }
            catch { }
            try
            {
                var n = el.Name;
                if (!string.IsNullOrWhiteSpace(n)) return n;
            }
            catch { }
            try
            {
                var l = el.Patterns.LegacyIAccessible.PatternOrDefault?.Name;
                if (!string.IsNullOrWhiteSpace(l)) return l;
            }
            catch { }
            return string.Empty;
        }

        #endregion
    }
}

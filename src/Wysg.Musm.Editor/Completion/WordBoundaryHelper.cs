using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wysg.Musm.Editor.Completion
{
    /// <summary>
    /// Internal helper that finds word boundaries in a line given a caret index.
    /// Exposed as internal so we can unit-test it without WPF/AvalonEdit.
    /// </summary>
    public class WordBoundaryHelper
    {
        public static (int startLocal, int endLocal) ComputeWordSpan(string lineText, int caretLocal)
        {
            if (string.IsNullOrEmpty(lineText)) return (0, 0);
            caretLocal = Math.Clamp(caretLocal, 0, lineText.Length);

            int left = caretLocal - 1;
            while (left >= 0 && IsWordChar(lineText[left])) left--;
            int start = left + 1;

            int right = caretLocal;
            while (right < lineText.Length && IsWordChar(lineText[right])) right++;
            int end = right;

            return (start, end);
        }

        private static bool IsWordChar(char c)
            => char.IsLetterOrDigit(c) || c == '_' || c == '-';
    }
}

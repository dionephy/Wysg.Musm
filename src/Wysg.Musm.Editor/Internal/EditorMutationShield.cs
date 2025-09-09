using System;
using System.Runtime.CompilerServices;
using ICSharpCode.AvalonEdit.Editing;

namespace Wysg.Musm.Editor.Internal
{
    /// <summary>
    /// Guards programmatic mutations so other listeners (e.g., selection mirrors)
    /// can skip/react accordingly and avoid nested document-change exceptions.
    /// </summary>
    public static class EditorMutationShield
    {
        private sealed class Counter { public int Count; }

        // Associates a tiny counter with each TextArea; auto-collected with the TextArea.
        private static readonly ConditionalWeakTable<TextArea, Counter> _map = new();

        /// <summary>
        /// Begin a guarded mutation block for the given TextArea.
        /// </summary>
        public static IDisposable Begin(TextArea area)
        {
            if (area is null) throw new ArgumentNullException(nameof(area));
            var ctr = _map.GetOrCreateValue(area);
            ctr.Count++;
            return new Scope(area);
        }

        /// <summary>
        /// Returns true while any Begin(...)/Dispose scope is active for this TextArea.
        /// </summary>
        public static bool IsActive(TextArea area)
        {
            return area != null
                   && _map.TryGetValue(area, out var ctr)
                   && ctr.Count > 0;
        }

        private sealed class Scope : IDisposable
        {
            private TextArea? _area;
            public Scope(TextArea area) => _area = area;

            public void Dispose()
            {
                var area = _area;
                if (area is null) return;
                _area = null;

                if (_map.TryGetValue(area, out var ctr))
                {
                    ctr.Count = Math.Max(0, ctr.Count - 1);
                    if (ctr.Count == 0)
                    {
                        // Optional: remove entry to keep the table tidy.
                        _map.Remove(area);
                    }
                }
            }
        }
    }
}

using ICSharpCode.AvalonEdit.Document;

namespace Wysg.Musm.Editor.Internal
{
    /// <summary>Lightweight public ISegment for ad-hoc ranges.</summary>
    public readonly struct InlineSegment : ISegment
    {
        public int Offset { get; }
        public int Length { get; }
        public int EndOffset => Offset + Length;

        public InlineSegment(int offset, int length)
        {
            Offset = offset < 0 ? 0 : offset;
            Length = length < 0 ? 0 : length;
        }
    }
}

using Wysg.Musm.Editor.Completion;
using FluentAssertions;
using Xunit;

namespace Wysg.Musm.Tests;

public class WordBoundaryHelperTests
{
    [Theory]
    [InlineData("hello world", 0, 0, 0)]   // caret at start, empty span
    [InlineData("hello world", 1, 0, 5)]   // inside "hello"
    [InlineData("hello world", 4, 0, 5)]
    [InlineData("hello world", 5, 0, 5)]   // at boundary (space)
    [InlineData("hello world", 6, 6, 11)]  // inside "world"
    [InlineData("abc_def-ghi", 4, 0, 11)]  // underscores/hyphen are word chars
    [InlineData("   brain-MRI  normal", 5, 3, 12)]
    [InlineData("한글 테스트", 1, 0, 2)]     // Hangul: char.IsLetterOrDigit == true
    public void ComputeWordSpan_Works(string line, int caretLocal, int expectedStart, int expectedEnd)
    {
        var (start, end) = WordBoundaryHelper.ComputeWordSpan(line, caretLocal);
        start.Should().Be(expectedStart);
        end.Should().Be(expectedEnd);
    }
}

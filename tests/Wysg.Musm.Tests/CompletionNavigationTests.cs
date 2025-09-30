using System.Windows.Controls;
using System.Windows.Input;
using FluentAssertions;
using Wysg.Musm.Editor.Completion;
using Xunit;
using ICSharpCode.AvalonEdit;
using System.Collections.Generic;
using ICSharpCode.AvalonEdit.CodeCompletion;

namespace Wysg.Musm.Tests
{
    public class CompletionNavigationTests
    {
        private class TestCompletionData : ICompletionData
        {
            public string Text { get; }
            public object Content => Text;
            public object Description => Text;
            public double Priority => 0;
            public System.Windows.Media.ImageSource? Image => null;

            public TestCompletionData(string text)
            {
                Text = text;
            }

            public void Complete(ICSharpCode.AvalonEdit.Editing.TextArea textArea, ICSharpCode.AvalonEdit.Document.ISegment completionSegment, System.EventArgs insertionRequestEventArgs)
            {
                // Not needed for navigation tests
            }
        }

        [Fact]
        public void SetSelectionSilently_ShouldChangeSelectionWithoutTriggeringEvents()
        {
            // Arrange
            var editor = new TextEditor();
            var window = new MusmCompletionWindow(editor);
            var items = new List<ICompletionData>
            {
                new TestCompletionData("first"),
                new TestCompletionData("second"),
                new TestCompletionData("third")
            };

            foreach (var item in items)
            {
                window.CompletionList.CompletionData.Add(item);
            }

            // Act
            window.SetSelectionSilently(1);

            // Assert
            window.CompletionList.ListBox.SelectedIndex.Should().Be(1);
        }

        [Fact]
        public void SetSelectionSilently_WithInvalidIndex_ShouldClearSelection()
        {
            // Arrange
            var editor = new TextEditor();
            var window = new MusmCompletionWindow(editor);
            var items = new List<ICompletionData>
            {
                new TestCompletionData("first"),
                new TestCompletionData("second")
            };

            foreach (var item in items)
            {
                window.CompletionList.CompletionData.Add(item);
            }

            // Act
            window.SetSelectionSilently(-1);

            // Assert
            window.CompletionList.ListBox.SelectedIndex.Should().Be(-1);
        }

        [Fact]
        public void SetSelectionSilently_WithIndexOutOfRange_ShouldClearSelection()
        {
            // Arrange
            var editor = new TextEditor();
            var window = new MusmCompletionWindow(editor);
            var items = new List<ICompletionData>
            {
                new TestCompletionData("first"),
                new TestCompletionData("second")
            };

            foreach (var item in items)
            {
                window.CompletionList.CompletionData.Add(item);
            }

            // Act
            window.SetSelectionSilently(10); // Index beyond range

            // Assert
            window.CompletionList.ListBox.SelectedIndex.Should().Be(-1);
        }

        [Fact]
        public void SelectExactOrNone_WithExactMatch_ShouldSelectItem()
        {
            // Arrange
            var editor = new TextEditor();
            var window = new MusmCompletionWindow(editor);
            var items = new List<ICompletionData>
            {
                new TestCompletionData("first"),
                new TestCompletionData("second"),
                new TestCompletionData("third")
            };

            foreach (var item in items)
            {
                window.CompletionList.CompletionData.Add(item);
            }

            // Act
            window.SelectExactOrNone("second");

            // Assert
            window.CompletionList.ListBox.SelectedIndex.Should().Be(1);
        }

        [Fact]
        public void SelectExactOrNone_WithNoMatch_ShouldClearSelection()
        {
            // Arrange
            var editor = new TextEditor();
            var window = new MusmCompletionWindow(editor);
            var items = new List<ICompletionData>
            {
                new TestCompletionData("first"),
                new TestCompletionData("second"),
                new TestCompletionData("third")
            };

            foreach (var item in items)
            {
                window.CompletionList.CompletionData.Add(item);
            }

            // Act
            window.SelectExactOrNone("nomatch");

            // Assert
            window.CompletionList.ListBox.SelectedIndex.Should().Be(-1);
        }
    }
}
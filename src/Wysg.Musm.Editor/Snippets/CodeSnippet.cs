using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Snippets;

namespace Wysg.Musm.Editor.Snippets
{
    /// <summary>
    /// ${...} 플레이스홀더가 들어간 스니펫을 AvalonEdit에 삽입/확장합니다.
    ///   ${0^Impression}
    ///   ${1^Lesion=a^lesion|b^mass}
    ///   ${2^Sites=li^left insula|ri^right insula}
    ///   ${3^ReplaceWith}
    /// </summary>
    public sealed class CodeSnippet
    {
        // ${header} 또는 ${header=defaults}
        // Group 1: header (예 "1^Lesion")
        // Group 3: defaults/options (예 "a^lesion|b^mass")
        private static readonly Regex PlaceholderPattern =
            new(@"\$\{([^\}=]+)(=([^\}]*))?\}", RegexOptions.Compiled);

        public string Shortcut { get; }
        public string Description { get; }
        public string Text { get; }

        /// <summary>파싱 중 발견된 플레이스홀더 컬렉션 (삽입 시 채워짐)</summary>
        public List<Placeholder> Placeholders { get; } = new();

        public CodeSnippet(string shortcut, string description, string text)
        {
            Shortcut = shortcut;
            Description = description;
            Text = text ?? string.Empty;
        }

        /// <summary>
        /// 현재 캐럿 위치에서 사용자가 타이핑한 프리픽스를 제거하고 스니펫을 삽입합니다.
        /// </summary>
        public void Insert(TextArea textArea, ISegment completionSegment)
        {
            if (textArea is null) throw new ArgumentNullException(nameof(textArea));
            if (completionSegment is null) throw new ArgumentNullException(nameof(completionSegment));

            // 완성창을 띄우게 했던 "단어/프리픽스"를 먼저 제거
            int start = completionSegment.Offset;
            var doc = textArea.Document;
            while (start > 0 && char.IsLetterOrDigit(doc.GetCharAt(start - 1)))
                start--;

            doc.Remove(start, completionSegment.EndOffset - start);

            // 스니펫 구성 및 삽입
            var snippet = CreateSnippetFromText(Text, start, doc);
            snippet.Insert(textArea);

            textArea.DefaultInputHandler.NestedInputHandlers.Add(
    new SnippetInputHandler(textArea, Placeholders));


            // 필요 시 플레이스홀더 네비게이션 핸들러 연결
            // textArea.DefaultInputHandler.NestedInputHandlers.Add(
            //     new SnippetInputHandler(textArea, snippet, Placeholders, start));
        }

        /// <summary>
        /// Text를 AvalonEdit Snippet으로 파싱하고 Placeholders를 채웁니다.
        /// </summary>
        private Snippet CreateSnippetFromText(string snippetText, int startOffset, TextDocument doc)
        {
            Placeholders.Clear();

            var snippet = new Snippet();
            int curDocOffset = startOffset;
            int last = 0;
            int elementIndex = 0;

            foreach (Match m in PlaceholderPattern.Matches(snippetText))
            {
                // 플레이스홀더 이전의 일반 텍스트
                if (m.Index > last)
                {
                    var before = snippetText.Substring(last, m.Index - last);
                    snippet.Elements.Add(new SnippetTextElement { Text = before });
                    curDocOffset += before.Length;
                    elementIndex++;
                }

                // 플레이스홀더 파싱
                var header = m.Groups[1].Value;                         // 예: "1^Lesion"
                var defaults = m.Groups[3].Success ? m.Groups[3].Value : string.Empty; // 예: "a^lesion|b^mass"

                var (ph, initialText) = ParsePlaceholder(header, defaults);

                var replaceable = new SnippetReplaceableTextElement { Text = initialText };
                ph.Element = replaceable;

                // TextSegment는 StartOffset/EndOffset으로 설정 (Offset/Length 아님)
                ph.Segment = new TextSegment
                {
                    StartOffset = curDocOffset,
                    EndOffset = curDocOffset + initialText.Length
                };
                ph.SnippetTextElementIndex = elementIndex;

                Placeholders.Add(ph);

                snippet.Elements.Add(replaceable);
                curDocOffset += ph.Segment.Length;
                elementIndex++;

                last = m.Index + m.Length;
            }

            // 마지막 남은 일반 텍스트
            if (last < snippetText.Length)
                snippet.Elements.Add(new SnippetTextElement { Text = snippetText[last..] });

            return snippet;
        }

        /// <summary>
        /// 헤더/옵션 문자열을 강타입 Placeholder와 초기 텍스트로 변환합니다.
        /// Header:  {Type}^{Title}[^{Extra...}]
        /// Options: defaults 쪽(= 뒤)은 "key^value"를 '|'로 구분
        /// </summary>
        private static (Placeholder ph, string initialText) ParsePlaceholder(string header, string defaults)
        {
            // 헤더 파싱
            var parts = header.Split('^');
            string typeToken;
            string title;

            if (parts.Length > 1 && int.TryParse(parts[0], out _))
            {
                // "1^Lesion^extra"
                typeToken = parts[0];
                title = parts[1];
            }
            else
            {
                // "Impression" (명시적 타입 없음 -> FreeText)
                typeToken = "0";
                title = parts[0];
            }

            // 옵션 파싱
            var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(defaults))
            {
                foreach (var token in defaults.Split('|', StringSplitOptions.RemoveEmptyEntries))
                {
                    var kv = token.Split('^');
                    if (kv.Length == 2)
                        options[kv[0]] = kv[1];
                    else if (kv.Length == 1)
                        options[kv[0]] = kv[0]; // "value" 단독 형태 허용
                }
            }

            // 타이틀 뒤의 여분 메타데이터
            string? metadata = parts.Length > 2 ? string.Join("^", parts.Skip(2)) : null;

            var ph = new Placeholder
            {
                Type = typeToken switch
                {
                    "1" => PlaceholderType.SingleChoice,
                    "2" => PlaceholderType.MultiSelect,
                    "3" => PlaceholderType.Replacement,
                    _ => PlaceholderType.FreeText
                },
                InitialDescription = title,
                Options = options,
                Metadata = metadata
            };

            // 에디터에 보일 초기 텍스트
            var initialText = title;
            return (ph, initialText);
        }
    }
}

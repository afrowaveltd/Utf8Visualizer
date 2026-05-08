using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace Utf8Visualizer
{
    /// <summary>
    /// Lehký classification tagger pro oranžové zvýraznění non-ASCII znaků.
    /// Classification tag je mnohem efektivnější než IntraTextAdornmentTag –
    /// nevytváří WPF elementy, jen přiřazuje klasifikační typ.
    /// </summary>
    internal sealed class Utf8NonAsciiTagger : ITagger<ClassificationTag>
    {
        private static readonly Regex EscapeRegex = new Regex(
            @"\\[uU]([0-9a-fA-F]{4,8})",
            RegexOptions.Compiled);

        private readonly ITextBuffer _buffer;
        private readonly string _filePath;
        private readonly IClassificationType _classificationType;

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public Utf8NonAsciiTagger(ITextBuffer buffer, IClassificationTypeRegistryService classificationRegistry)
        {
            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            _filePath = GetDocumentPath(buffer);
            _classificationType = classificationRegistry.GetClassificationType("Utf8Visualizer.NonAscii");

            _buffer.Changed += OnBufferChanged;
            Utf8VisualizationState.StateChanged += OnVisualizationStateChanged;
        }

        public IEnumerable<ITagSpan<ClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (!Utf8VisualizationState.IsEnabledForDocument(_filePath) || spans.Count == 0)
            {
                yield break;
            }

            var snapshot = spans[0].Snapshot;
            var text = snapshot.GetText();

            // Najdeme všechny escape range, ať v nich non-ASCII nezvýrazňujeme
            var escapeRanges = new List<Span>();
            foreach (Match match in EscapeRegex.Matches(text))
            {
                escapeRanges.Add(new Span(match.Index, match.Length));
            }

            var tag = new ClassificationTag(_classificationType);

            for (var i = 0; i < text.Length; i++)
            {
                var ch = text[i];
                if (ch <= 127)
                {
                    continue;
                }

                if (IsInsideEscapeRange(i, escapeRanges))
                {
                    continue;
                }

                if (char.IsHighSurrogate(ch) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    var charSpan = new SnapshotSpan(snapshot, i, 2);
                    if (spans.Any(s => s.IntersectsWith(charSpan)))
                    {
                        yield return new TagSpan<ClassificationTag>(charSpan, tag);
                    }

                    i++;
                    continue;
                }

                if (char.IsLowSurrogate(ch))
                {
                    continue;
                }

                var singleSpan = new SnapshotSpan(snapshot, i, 1);
                if (spans.Any(s => s.IntersectsWith(singleSpan)))
                {
                    yield return new TagSpan<ClassificationTag>(singleSpan, tag);
                }
            }
        }

        private static bool IsInsideEscapeRange(int index, List<Span> escapeRanges)
        {
            foreach (var range in escapeRanges)
            {
                if (index >= range.Start && index < range.End)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            foreach (var change in e.Changes)
            {
                TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(e.After, change.NewSpan)));
            }
        }

        private void OnVisualizationStateChanged(object sender, EventArgs e)
        {
            var snapshot = _buffer.CurrentSnapshot;
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(snapshot, 0, snapshot.Length)));
        }

        private static string GetDocumentPath(ITextBuffer buffer)
        {
            if (buffer?.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument document) == true
                && document != null)
            {
                return document.FilePath;
            }

            return null;
        }
    }
}

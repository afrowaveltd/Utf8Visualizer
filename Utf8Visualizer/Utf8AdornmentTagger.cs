using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Utf8Visualizer
{
    internal sealed class Utf8AdornmentTagger : ITagger<IntraTextAdornmentTag>
    {
        private static readonly Regex UnicodeEscapeRegex = new Regex(@"\\u[0-9a-fA-F]{4}", RegexOptions.Compiled);
        private static readonly Brush ForegroundBrush = new SolidColorBrush(Colors.Teal);
        private static readonly Brush BorderBrush = new SolidColorBrush(Colors.Teal);
        private static readonly Brush BackgroundBrush = new SolidColorBrush(Color.FromArgb(30, 78, 201, 176));

        private readonly ITextView _view;
        private readonly ITextBuffer _buffer;

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        static Utf8AdornmentTagger()
        {
            ForegroundBrush.Freeze();
            BorderBrush.Freeze();
            BackgroundBrush.Freeze();
        }

        public Utf8AdornmentTagger(ITextView view, ITextBuffer buffer)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            _buffer.Changed += OnBufferChanged;
            _view.Closed += OnViewClosed;
            Utf8VisualizationState.IsEnabledChanged += OnVisualizationStateChanged;
        }

        public IEnumerable<ITagSpan<IntraTextAdornmentTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (!Utf8VisualizationState.IsEnabled || spans.Count == 0)
            {
                yield break;
            }

            var snapshot = spans[0].Snapshot;
            var text = snapshot.GetText();

            foreach (Match match in UnicodeEscapeRegex.Matches(text))
            {
                var escapeSpan = new SnapshotSpan(snapshot, match.Index, match.Length);
                if (!spans.Any(span => span.IntersectsWith(escapeSpan)))
                {
                    continue;
                }

                var decodedChar = DecodeEscapeSequence(match.Value);
                var adornment = CreateAdornment(decodedChar, match.Value);
                var tag = new IntraTextAdornmentTag(adornment, null, PositionAffinity.Successor);

                yield return new TagSpan<IntraTextAdornmentTag>(escapeSpan, tag);
            }
        }

        private void OnBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            foreach (var change in e.Changes)
            {
                RaiseTagsChanged(new SnapshotSpan(e.After, change.NewSpan));
            }
        }

        private void OnViewClosed(object sender, EventArgs e)
        {
            _buffer.Changed -= OnBufferChanged;
            _view.Closed -= OnViewClosed;
            Utf8VisualizationState.IsEnabledChanged -= OnVisualizationStateChanged;
        }

        private void OnVisualizationStateChanged(object sender, EventArgs e)
        {
            var snapshot = _buffer.CurrentSnapshot;
            RaiseTagsChanged(new SnapshotSpan(snapshot, 0, snapshot.Length));
        }

        private void RaiseTagsChanged(SnapshotSpan span)
        {
            var handler = TagsChanged;
            if (handler != null)
            {
                handler(this, new SnapshotSpanEventArgs(span));
            }
        }

        private static char DecodeEscapeSequence(string escapeSequence)
        {
            var hex = escapeSequence.Substring(2);
            var codePoint = Convert.ToInt32(hex, 16);
            return (char)codePoint;
        }

        private static Border CreateAdornment(char decodedChar, string originalEscape)
        {
            var textBlock = new TextBlock
            {
                Text = decodedChar.ToString(),
                Foreground = ForegroundBrush,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                ToolTip = $"Unicode escape: {originalEscape} -> {decodedChar} (U+{originalEscape.Substring(2).ToUpperInvariant()})"
            };

            return new Border
            {
                Child = textBlock,
                Background = BackgroundBrush,
                CornerRadius = new CornerRadius(2),
                Padding = new Thickness(1, 0, 1, 0),
                BorderBrush = BorderBrush,
                BorderThickness = new Thickness(0.5)
            };
        }
    }
}

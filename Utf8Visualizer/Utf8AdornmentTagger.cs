using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Utf8Visualizer
{
    /// <summary>
    /// Adornment tagger pro escape sekvence.
    /// Vykresluje tyrkysový WPF adornment s glyphem nad \uXXXX a \UXXXXXXXX.
    /// </summary>
    internal sealed class Utf8AdornmentTagger : ITagger<IntraTextAdornmentTag>
    {
        private static readonly Regex EscapeRegex = new Regex(
            @"\\[uU]([0-9a-fA-F]{4,8})",
            RegexOptions.Compiled);

        private static readonly Brush ForegroundBrush = CreateFrozenBrush(Color.FromRgb(0, 128, 128));
        private static readonly Brush BackgroundBrush = CreateFrozenBrush(Color.FromArgb(35, 78, 201, 176));
        private static readonly Brush BorderBrush = CreateFrozenBrush(Color.FromRgb(0, 128, 128));

        private readonly ITextView _view;
        private readonly ITextBuffer _buffer;
        private readonly string _filePath;

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        private static Brush CreateFrozenBrush(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        public Utf8AdornmentTagger(ITextView view, ITextBuffer buffer)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));

            _filePath = GetDocumentPath(buffer);

            _buffer.Changed += OnBufferChanged;
            _view.Closed += OnViewClosed;
            Utf8VisualizationState.StateChanged += OnVisualizationStateChanged;
        }

        public IEnumerable<ITagSpan<IntraTextAdornmentTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (!Utf8VisualizationState.IsEnabledForDocument(_filePath) || spans.Count == 0)
            {
                yield break;
            }

            var snapshot = spans[0].Snapshot;
            var text = snapshot.GetText();

            foreach (Match match in EscapeRegex.Matches(text))
            {
                var escapeSpan = new SnapshotSpan(snapshot, match.Index, match.Length);
                if (!spans.Any(s => s.IntersectsWith(escapeSpan)))
                {
                    continue;
                }

                var decoded = DecodeEscapeToDisplayText(match.Groups[1].Value);
                if (decoded == null)
                {
                    continue;
                }

                var adornment = CreateAdornment(decoded, match.Value);
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
            Utf8VisualizationState.StateChanged -= OnVisualizationStateChanged;
        }

        private void OnVisualizationStateChanged(object sender, EventArgs e)
        {
            var snapshot = _buffer.CurrentSnapshot;
            RaiseTagsChanged(new SnapshotSpan(snapshot, 0, snapshot.Length));
        }

        private void RaiseTagsChanged(SnapshotSpan span)
        {
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
        }

        private static string DecodeEscapeToDisplayText(string hexDigits)
        {
            if (hexDigits.Length < 4)
            {
                return null;
            }

            if (!int.TryParse(hexDigits, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var codePoint))
            {
                return null;
            }

            if (codePoint > 0x10FFFF)
            {
                return null;
            }

            if (codePoint > 0xFFFF)
            {
                return char.ConvertFromUtf32(codePoint);
            }

            return ((char)codePoint).ToString();
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

        private static Border CreateAdornment(string displayText, string originalEscape)
        {
            var cp = GetCodePoint(displayText);
            var cpLabel = cp.HasValue ? $"U+{cp.Value:X4}" : "?";

            var textBlock = new TextBlock
            {
                Text = displayText,
                Foreground = ForegroundBrush,
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                TextAlignment = TextAlignment.Center,
                ToolTip = $"Escape: {originalEscape} → {displayText} ({cpLabel})"
            };

            return new Border
            {
                Child = textBlock,
                Background = BackgroundBrush,
                CornerRadius = new CornerRadius(2),
                Padding = new Thickness(2, 0, 2, 0),
                BorderBrush = BorderBrush,
                BorderThickness = new Thickness(0.5)
            };
        }

        private static int? GetCodePoint(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            if (text.Length == 1)
            {
                return text[0];
            }

            if (text.Length == 2 && char.IsHighSurrogate(text[0]) && char.IsLowSurrogate(text[1]))
            {
                return char.ConvertToUtf32(text[0], text[1]);
            }

            return null;
        }
    }
}

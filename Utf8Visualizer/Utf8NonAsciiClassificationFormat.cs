using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Utf8Visualizer
{
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "Utf8Visualizer.NonAscii")]
    [Name("Utf8Visualizer.NonAscii")]
    [DisplayName("UTF-8 Visualizer Non-ASCII")]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class Utf8NonAsciiClassificationFormat : ClassificationFormatDefinition
    {
        public Utf8NonAsciiClassificationFormat()
        {
            ForegroundColor = Color.FromRgb(200, 90, 20);
            BackgroundColor = Color.FromArgb(35, 255, 140, 0);
            IsBold = true;
            TextDecorations = null;
        }
    }
}

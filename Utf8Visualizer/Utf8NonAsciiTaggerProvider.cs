using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace Utf8Visualizer
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(ClassificationTag))]
    [ContentType("text")]
    internal sealed class Utf8NonAsciiTaggerProvider : ITaggerProvider
    {
        [Import]
        internal IClassificationTypeRegistryService ClassificationRegistry { get; set; }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            if (buffer == null)
            {
                return null;
            }

            return new Utf8NonAsciiTagger(buffer, ClassificationRegistry) as ITagger<T>;
        }
    }
}

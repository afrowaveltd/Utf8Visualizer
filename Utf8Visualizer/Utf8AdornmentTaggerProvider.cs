using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace Utf8Visualizer
{
    [Export(typeof(IViewTaggerProvider))]
    [TagType(typeof(IntraTextAdornmentTag))]
    [ContentType("json")]
    [ContentType("javascript")]
    [ContentType("typescript")]
    [ContentType("code++.javascript")]
    [ContentType("code++.typescript")]
    [ContentType("Python")]
    [ContentType("CSharp")]
    [ContentType("Java")]
    [ContentType("Go")]
    [ContentType("Rust")]
    [ContentType("Ruby")]
    [ContentType("PHP")]
    [ContentType("Swift")]
    [ContentType("Kotlin")]
    [ContentType("Scala")]
    [ContentType("Perl")]
    [ContentType("Lua")]
    [ContentType("PowerShell")]
    [ContentType("plaintext")]
    [ContentType("XML")]
    [ContentType("yaml")]
    [ContentType("C/C++")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class Utf8AdornmentTaggerProvider : IViewTaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            if (textView == null || buffer == null)
            {
                return null;
            }

            if (buffer != textView.TextBuffer)
            {
                return null;
            }

            return new Utf8AdornmentTagger(textView, buffer) as ITagger<T>;
        }
    }
}

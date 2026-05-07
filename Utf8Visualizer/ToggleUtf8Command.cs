using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using Task = System.Threading.Tasks.Task;

namespace Utf8Visualizer
{
    internal sealed class ToggleUtf8Command
    {
        /// <summary>
        /// Identifikátor příkazu pro přepnutí režimu vizualizace.
        /// </summary>
        public const int ToggleVisualizationCommandId = 0x0100;

        /// <summary>
        /// Identifikátor příkazu pro převod vybraného textu nebo znaku pod kurzorem.
        /// </summary>
        public const int ConvertAtCursorOrSelectionCommandId = 0x0101;

        /// <summary>
        /// Identifikátor toolbar příkazu pro přepnutí režimu vizualizace.
        /// </summary>
        public const int ToggleVisualizationToolbarCommandId = 0x0102;

        /// <summary>
        /// Identifikátor příkazu v menu Tools pro přepnutí režimu vizualizace.
        /// </summary>
        public const int ToggleVisualizationToolsCommandId = 0x0103;

        /// <summary>
        /// Identifikátor příkazu v menu Tools pro převod vybraného textu nebo znaku pod kurzorem.
        /// </summary>
        public const int ConvertAtCursorOrSelectionToolsCommandId = 0x0104;

        public static readonly Guid CommandSet = new Guid("c3d4e5f6-a7b8-9012-3c4d-5e6f7a8b9012");

        private const string WindowTitle = "UTF-8 Visualizer";
        private readonly AsyncPackage _package;
        private static readonly Regex UnicodeEscapeRegex = new Regex(@"\\u[0-9a-fA-F]{4}", RegexOptions.Compiled);

        private ToggleUtf8Command(AsyncPackage package, OleMenuCommandService commandService)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var toggleCommand = new OleMenuCommand(ToggleVisualizationExecute, new CommandID(CommandSet, ToggleVisualizationCommandId));
            toggleCommand.BeforeQueryStatus += OnToggleVisualizationBeforeQueryStatus;
            commandService.AddCommand(toggleCommand);

            var toggleToolbarCommand = new OleMenuCommand(ToggleVisualizationExecute, new CommandID(CommandSet, ToggleVisualizationToolbarCommandId));
            toggleToolbarCommand.BeforeQueryStatus += OnToggleVisualizationBeforeQueryStatus;
            commandService.AddCommand(toggleToolbarCommand);

            var toggleToolsCommand = new OleMenuCommand(ToggleVisualizationExecute, new CommandID(CommandSet, ToggleVisualizationToolsCommandId));
            toggleToolsCommand.BeforeQueryStatus += OnToggleVisualizationBeforeQueryStatus;
            commandService.AddCommand(toggleToolsCommand);

            var convertCommand = new MenuCommand(ConvertAtCursorOrSelectionExecute, new CommandID(CommandSet, ConvertAtCursorOrSelectionCommandId));
            commandService.AddCommand(convertCommand);

            var convertToolsCommand = new MenuCommand(ConvertAtCursorOrSelectionExecute, new CommandID(CommandSet, ConvertAtCursorOrSelectionToolsCommandId));
            commandService.AddCommand(convertToolsCommand);
        }

        public static ToggleUtf8Command Instance { get; private set; }

        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider => _package;

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService == null)
            {
                throw new InvalidOperationException("Nepodařilo se získat službu nabídky příkazů Visual Studia.");
            }

            Instance = new ToggleUtf8Command(package, commandService);
        }

        private static void OnToggleVisualizationBeforeQueryStatus(object sender, EventArgs e)
        {
            if (sender is not OleMenuCommand command)
            {
                return;
            }

            command.Checked = Utf8VisualizationState.IsEnabled;
            command.Text = Utf8VisualizationState.IsEnabled
                ? "Vypnout UTF vizualizaci"
                : "Zapnout UTF vizualizaci";
        }

        private void ToggleVisualizationExecute(object sender, EventArgs e)
        {
            Utf8VisualizationState.IsEnabled = !Utf8VisualizationState.IsEnabled;
        }

        private void ConvertAtCursorOrSelectionExecute(object sender, EventArgs e)
        {
            _package.JoinableTaskFactory.RunAsync(async delegate
            {
                await ConvertAtCursorOrSelectionExecuteAsync();
            }).FileAndForget("Utf8Visualizer/ConvertAtCursorOrSelection");
        }

        private async Task ConvertAtCursorOrSelectionExecuteAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);

            try
            {
                var dte = await ServiceProvider.GetServiceAsync(typeof(DTE)) as DTE;
                if (dte?.ActiveDocument?.Selection is not TextSelection selection)
                {
                    return;
                }

                if (!selection.IsEmpty)
                {
                    var selectedText = selection.Text;
                    if (TryConvertText(selectedText, out var convertedText))
                    {
                        selection.Text = convertedText;
                        return;
                    }

                    ShowMessage("Ve výběru nebyl nalezen znak k převodu ani sekvence \\uXXXX.", MessageBoxImage.Information);
                    return;
                }

                if (dte.ActiveDocument.Object("TextDocument") is not TextDocument textDocument)
                {
                    return;
                }

                if (TryConvertAtCursor(selection, textDocument))
                {
                    return;
                }

                ShowMessage("Na pozici kurzoru nebyla nalezena sekvence \\uXXXX ani znak mimo ASCII.", MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowMessage($"Chyba: {ex.Message}", MessageBoxImage.Error);
            }
        }

        private static bool TryConvertAtCursor(TextSelection selection, TextDocument textDocument)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var editPoint = textDocument.StartPoint.CreateEditPoint();
            var line = selection.CurrentLine;
            editPoint.MoveToLineAndOffset(line, 1);
            var lineEnd = editPoint.CreateEditPoint();
            lineEnd.EndOfLine();
            var lineText = editPoint.GetText(lineEnd);

            var offsetInLine = Math.Max(0, selection.CurrentColumn - 1);
            var matchAtCursor = UnicodeEscapeRegex.Matches(lineText)
                .Cast<Match>()
                .FirstOrDefault(match => IsCursorInsideMatch(offsetInLine, match));

            if (matchAtCursor != null)
            {
                ReplaceSelection(selection, line, matchAtCursor.Index, matchAtCursor.Length, DecodeEscapeSequence(matchAtCursor.Value));
                return true;
            }

            if (offsetInLine < lineText.Length)
            {
                var characterAtCursor = lineText[offsetInLine];
                if (characterAtCursor > 127)
                {
                    ReplaceSelection(selection, line, offsetInLine, 1, EncodeAsUnicodeEscape(characterAtCursor));
                    return true;
                }
            }

            return false;
        }

        private static bool TryConvertText(string source, out string converted)
        {
            if (source == null)
            {
                converted = string.Empty;
                return false;
            }

            if (UnicodeEscapeRegex.IsMatch(source))
            {
                converted = UnicodeEscapeRegex.Replace(source, match => DecodeEscapeSequence(match.Value));
                return true;
            }

            if (source.Any(ch => ch > 127))
            {
                var builder = new StringBuilder(source.Length);
                foreach (var ch in source)
                {
                    builder.Append(ch > 127 ? EncodeAsUnicodeEscape(ch) : ch.ToString());
                }

                converted = builder.ToString();
                return true;
            }

            converted = source;
            return false;
        }

        private static bool IsCursorInsideMatch(int offsetInLine, Match match)
        {
            var absoluteStart = match.Index;
            var absoluteEnd = absoluteStart + match.Length;
            return offsetInLine >= absoluteStart && offsetInLine < absoluteEnd;
        }

        private static string DecodeEscapeSequence(string escapeSequence)
        {
            var hex = escapeSequence.Substring(2);
            var codePoint = int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return ((char)codePoint).ToString();
        }

        private static string EncodeAsUnicodeEscape(char character)
        {
            return $"\\u{((int)character).ToString("x4", CultureInfo.InvariantCulture)}";
        }

        private static void ReplaceSelection(TextSelection selection, int line, int startIndex, int length, string replacement)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            selection.MoveToLineAndOffset(line, startIndex + 1);
            selection.MoveToLineAndOffset(line, startIndex + length + 1, true);
            selection.Text = replacement;
        }

        private static void ShowMessage(string message, MessageBoxImage image)
        {
            MessageBox.Show(message, WindowTitle, MessageBoxButton.OK, image);
        }
    }
}

using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using Task = System.Threading.Tasks.Task;

namespace Utf8Visualizer
{
    internal sealed class ToggleUtf8Command
    {
        public const int CommandId = 0x0100;
        public static readonly Guid CommandSet = new Guid("c3d4e5f6-a7b8-9012-3c4d-5e6f7a8b9012");

        private const string WindowTitle = "UTF-8 Visualizer";
        private readonly AsyncPackage _package;
        private static readonly Regex UnicodeEscapeRegex = new Regex(@"\\u[0-9a-fA-F]{4}", RegexOptions.Compiled);

        private ToggleUtf8Command(AsyncPackage package, OleMenuCommandService commandService)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandId = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(Execute, menuCommandId);
            commandService.AddCommand(menuItem);
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

        private void Execute(object sender, EventArgs e)
        {
            _package.JoinableTaskFactory.RunAsync(async delegate
            {
                await ExecuteAsync(sender, e);
            }).FileAndForget("Utf8Visualizer/ToggleUtf8Command");
        }

        private async Task ExecuteAsync(object sender, EventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);

            try
            {
                var dte = await ServiceProvider.GetServiceAsync(typeof(DTE)) as DTE;
                if (dte?.ActiveDocument?.Selection is not TextSelection selection)
                {
                    return;
                }

                if (dte.ActiveDocument.Object("TextDocument") is not TextDocument textDocument)
                {
                    return;
                }

                var editPoint = textDocument.StartPoint.CreateEditPoint();
                var line = selection.CurrentLine;
                editPoint.MoveToLineAndOffset(line, 1);
                var lineEnd = editPoint.CreateEditPoint();
                lineEnd.EndOfLine();
                var lineText = editPoint.GetText(lineEnd);

                var offsetInLine = Math.Max(0, selection.CurrentColumn - 1);
                var searchStart = Math.Max(0, offsetInLine - 10);
                var searchEnd = Math.Min(lineText.Length, offsetInLine + 10);
                var searchText = lineText.Substring(searchStart, searchEnd - searchStart);

                var match = UnicodeEscapeRegex.Matches(searchText)
                    .Cast<Match>()
                    .FirstOrDefault(m => IsCursorInsideMatch(offsetInLine, searchStart, m));

                if (match != null)
                {
                    ReplaceSelection(selection, line, searchStart + match.Index, match.Length, DecodeEscapeSequence(match.Value));
                    return;
                }

                if (offsetInLine < lineText.Length)
                {
                    var ch = lineText[offsetInLine];
                    if (ch > 127)
                    {
                        ReplaceSelection(selection, line, offsetInLine, 1, $"\\u{((int)ch).ToString("X4").ToLowerInvariant()}");
                        return;
                    }
                }

                var anyMatch = UnicodeEscapeRegex.Match(lineText);
                if (anyMatch.Success)
                {
                    ReplaceSelection(selection, line, anyMatch.Index, anyMatch.Length, DecodeEscapeSequence(anyMatch.Value));
                    return;
                }

                ShowMessage("Na pozici kurzoru nebyla nalezena UTF-8 sekvence ani speciální znak.", MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowMessage($"Chyba: {ex.Message}", MessageBoxImage.Error);
            }
        }

        private static bool IsCursorInsideMatch(int offsetInLine, int searchStart, Match match)
        {
            var absoluteStart = searchStart + match.Index;
            var absoluteEnd = absoluteStart + match.Length;
            return offsetInLine >= absoluteStart && offsetInLine < absoluteEnd;
        }

        private static string DecodeEscapeSequence(string escapeSequence)
        {
            var hex = escapeSequence.Substring(2);
            var codePoint = Convert.ToInt32(hex, 16);
            return ((char)codePoint).ToString();
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

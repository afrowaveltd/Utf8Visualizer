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
      public const int ToggleVisualizationCommandId = 0x0100;
      public const int ConvertAtCursorOrSelectionCommandId = 0x0101;
      public const int ToggleVisualizationToolbarCommandId = 0x0102;
      public const int ToggleVisualizationToolsCommandId = 0x0103;
      public const int ConvertAtCursorOrSelectionToolsCommandId = 0x0104;
      public const int ConvertAllInDocumentCommandId = 0x0105;
      public const int ConvertAllInDocumentToolsCommandId = 0x0106;
      public const int TogglePerDocumentCommandId = 0x0107;
      public const int TogglePerDocumentToolsCommandId = 0x0108;
      public const int ConvertAllInDocumentToolbarCommandId = 0x0109;

      public static readonly Guid CommandSet = new Guid("c3d4e5f6-a7b8-9012-3c4d-5e6f7a8b9012");

      private const string WindowTitle = "UTF-8 Visualizer";
      private readonly AsyncPackage _package;
      private static readonly Regex EscapeRegex = new Regex(
         @"\\[uU]([0-9a-fA-F]{4,8})",
         RegexOptions.Compiled);

      private ToggleUtf8Command(AsyncPackage package, OleMenuCommandService commandService)
      {
         _package = package ?? throw new ArgumentNullException(nameof(package));
         commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

         RegisterToggleCommand(commandService, ToggleVisualizationCommandId);
         RegisterToggleCommand(commandService, ToggleVisualizationToolbarCommandId);
         RegisterToggleCommand(commandService, ToggleVisualizationToolsCommandId);

         RegisterCommand(commandService, ConvertAtCursorOrSelectionCommandId, ConvertAtCursorOrSelectionExecute, OnConvertQueryStatus);
         RegisterCommand(commandService, ConvertAtCursorOrSelectionToolsCommandId, ConvertAtCursorOrSelectionExecute, OnConvertQueryStatus);
         RegisterCommand(commandService, ConvertAllInDocumentCommandId, ConvertAllInDocumentExecute, OnConvertAllQueryStatus);
         RegisterCommand(commandService, ConvertAllInDocumentToolsCommandId, ConvertAllInDocumentExecute, OnConvertAllQueryStatus);
         RegisterCommand(commandService, ConvertAllInDocumentToolbarCommandId, ConvertAllInDocumentExecute, OnConvertAllQueryStatus);
         RegisterCommand(commandService, TogglePerDocumentCommandId, TogglePerDocumentExecute, OnTogglePerDocumentQueryStatus);
         RegisterCommand(commandService, TogglePerDocumentToolsCommandId, TogglePerDocumentExecute, OnTogglePerDocumentQueryStatus);
      }

      private void RegisterToggleCommand(OleMenuCommandService commandService, int commandId)
      {
         var command = new OleMenuCommand(ToggleVisualizationExecute, new CommandID(CommandSet, commandId));
         command.BeforeQueryStatus += OnToggleVisualizationBeforeQueryStatus;
         commandService.AddCommand(command);
      }

      private void RegisterCommand(OleMenuCommandService commandService, int commandId, EventHandler executeHandler, EventHandler queryStatusHandler)
      {
         var command = new OleMenuCommand(executeHandler, new CommandID(CommandSet, commandId));
         if (queryStatusHandler != null)
         {
            command.BeforeQueryStatus += queryStatusHandler;
         }

         commandService.AddCommand(command);
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

         command.Visible = true;
         command.Enabled = true;
         command.Checked = Utf8VisualizationState.IsGloballyEnabled;
         command.Text = Utf8VisualizationState.IsGloballyEnabled
             ? "Vypnout UTF vizualizaci"
             : "Zapnout UTF vizualizaci";
      }

      private static void OnConvertQueryStatus(object sender, EventArgs e)
      {
         if (sender is not OleMenuCommand command)
         {
            return;
         }

         command.Visible = true;
         command.Enabled = true;
      }

      private static void OnConvertAllQueryStatus(object sender, EventArgs e)
      {
         if (sender is not OleMenuCommand command)
         {
            return;
         }

         command.Visible = true;
         command.Enabled = true;
      }

      private static void OnTogglePerDocumentQueryStatus(object sender, EventArgs e)
      {
         if (sender is not OleMenuCommand command)
         {
            return;
         }

         command.Visible = true;
         command.Enabled = true;

         var filePath = GetActiveDocumentPath();
         var isDisabled = Utf8VisualizationState.IsDocumentExplicitlyDisabled(filePath);
         command.Checked = !isDisabled;
         command.Text = isDisabled
             ? "Zapnout vizualizaci pro tento dokument"
             : "Vypnout vizualizaci pro tento dokument";
      }

      private void ToggleVisualizationExecute(object sender, EventArgs e)
      {
         Utf8VisualizationState.IsGloballyEnabled = !Utf8VisualizationState.IsGloballyEnabled;
      }

      private void TogglePerDocumentExecute(object sender, EventArgs e)
      {
         var filePath = GetActiveDocumentPath();
         if (string.IsNullOrEmpty(filePath))
         {
            return;
         }

         var currentlyDisabled = Utf8VisualizationState.IsDocumentExplicitlyDisabled(filePath);
         Utf8VisualizationState.SetPerDocumentState(filePath, currentlyDisabled);
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
            var dte = await ServiceProvider.GetServiceAsync(typeof(DTE)) as DTE
               ?? throw new InvalidOperationException("Nepodařilo se získat službu DTE.");

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

               ShowMessage("Ve výběru nebyl nalezen znak k převodu ani escape sekvence.", MessageBoxImage.Information);
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

            ShowMessage("Na pozici kurzoru nebyla nalezena escape sekvence ani non-ASCII znak.", MessageBoxImage.Information);
         }
         catch (Exception ex)
         {
            ShowMessage($"Chyba: {ex.Message}", MessageBoxImage.Error);
         }
      }

      private void ConvertAllInDocumentExecute(object sender, EventArgs e)
      {
         _package.JoinableTaskFactory.RunAsync(async delegate
         {
            await ConvertAllInDocumentExecuteAsync();
         }).FileAndForget("Utf8Visualizer/ConvertAllInDocument");
      }

      private async Task ConvertAllInDocumentExecuteAsync()
      {
         await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);

         try
         {
            var dte = await ServiceProvider.GetServiceAsync(typeof(DTE)) as DTE
               ?? throw new InvalidOperationException("Nepodařilo se získat službu DTE.");

            if (dte?.ActiveDocument?.Selection is not TextSelection selection)
            {
               return;
            }

            var docText = dte.ActiveDocument.Object("TextDocument") is TextDocument textDocument
               ? textDocument.StartPoint.CreateEditPoint().GetText(textDocument.EndPoint)
               : null;

            if (string.IsNullOrEmpty(docText))
            {
               return;
            }

            var hasEscapes = EscapeRegex.IsMatch(docText);
            var hasNonAscii = docText.Any(ch => ch > 127);

            if (!hasEscapes && !hasNonAscii)
            {
               ShowMessage("V dokumentu nebyly nalezeny escape sekvence ani non-ASCII znaky.", MessageBoxImage.Information);
               return;
            }

            if (hasEscapes)
            {
               selection.SelectAll();
               var fullText = selection.Text;
               var converted = EscapeRegex.Replace(fullText, match =>
               {
                  var hex = match.Groups[1].Value;
                  if (int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var cp))
                  {
                     if (cp > 0xFFFF && cp <= 0x10FFFF)
                     {
                        return char.ConvertFromUtf32(cp);
                     }

                     if (cp <= 0xFFFF)
                     {
                        return ((char)cp).ToString();
                     }
                  }

                  return match.Value;
               });

               selection.Text = converted;
               ShowMessage("Převedeno: \\uXXXX → znaky v celém dokumentu.", MessageBoxImage.Information);
            }
            else if (hasNonAscii)
            {
               selection.SelectAll();
               var fullText = selection.Text;
               var builder = new StringBuilder(fullText.Length * 2);

               for (var i = 0; i < fullText.Length; i++)
               {
                  var ch = fullText[i];
                  if (ch <= 127)
                  {
                     builder.Append(ch);
                     continue;
                  }

                  if (char.IsHighSurrogate(ch) && i + 1 < fullText.Length && char.IsLowSurrogate(fullText[i + 1]))
                  {
                     var cp = char.ConvertToUtf32(ch, fullText[i + 1]);
                     builder.AppendFormat(CultureInfo.InvariantCulture, "\\U{0:X8}", cp);
                     i++;
                     continue;
                  }

                  if (char.IsLowSurrogate(ch))
                  {
                     builder.Append(ch);
                     continue;
                  }

                  builder.AppendFormat(CultureInfo.InvariantCulture, "\\u{0:x4}", (int)ch);
               }

               selection.Text = builder.ToString();
               ShowMessage("Převedeno: non-ASCII znaky → \\uXXXX v celém dokumentu.", MessageBoxImage.Information);
            }
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
         var matchAtCursor = EscapeRegex.Matches(lineText)
             .Cast<Match>()
             .FirstOrDefault(match => IsCursorInsideMatch(offsetInLine, match));

         if (matchAtCursor != null)
         {
            ReplaceSelection(selection, line, matchAtCursor.Index, matchAtCursor.Length,
               DecodeEscapeSequence(matchAtCursor));
            return true;
         }

         if (offsetInLine < lineText.Length)
         {
            var characterAtCursor = lineText[offsetInLine];
            if (characterAtCursor > 127)
            {
               if (char.IsHighSurrogate(characterAtCursor)
                   && offsetInLine + 1 < lineText.Length
                   && char.IsLowSurrogate(lineText[offsetInLine + 1]))
               {
                  var cp = char.ConvertToUtf32(characterAtCursor, lineText[offsetInLine + 1]);
                  ReplaceSelection(selection, line, offsetInLine, 2, string.Format(CultureInfo.InvariantCulture, "\\U{0:X8}", cp));
                  return true;
               }

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

         if (EscapeRegex.IsMatch(source))
         {
            converted = EscapeRegex.Replace(source, match =>
            {
               var hex = match.Groups[1].Value;
               if (int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var cp))
               {
                  if (cp > 0xFFFF && cp <= 0x10FFFF)
                  {
                     return char.ConvertFromUtf32(cp);
                  }

                  if (cp <= 0xFFFF)
                  {
                     return ((char)cp).ToString();
                  }
               }

               return match.Value;
            });

            return true;
         }

         if (source.Any(ch => ch > 127))
         {
            var builder = new StringBuilder(source.Length * 2);
            for (var i = 0; i < source.Length; i++)
            {
               var ch = source[i];
               if (ch <= 127)
               {
                  builder.Append(ch);
                  continue;
               }

               if (char.IsHighSurrogate(ch) && i + 1 < source.Length && char.IsLowSurrogate(source[i + 1]))
               {
                  var cp = char.ConvertToUtf32(ch, source[i + 1]);
                  builder.AppendFormat(CultureInfo.InvariantCulture, "\\U{0:X8}", cp);
                  i++;
                  continue;
               }

               if (char.IsLowSurrogate(ch))
               {
                  builder.Append(ch);
                  continue;
               }

               builder.Append(EncodeAsUnicodeEscape(ch));
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

      private static string DecodeEscapeSequence(Match match)
      {
         var hex = match.Groups[1].Value;
         if (!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var codePoint))
         {
            return match.Value;
         }

         if (codePoint > 0xFFFF && codePoint <= 0x10FFFF)
         {
            return char.ConvertFromUtf32(codePoint);
         }

         if (codePoint <= 0xFFFF)
         {
            return ((char)codePoint).ToString();
         }

         return match.Value;
      }

      private static string EncodeAsUnicodeEscape(char character)
      {
         return string.Format(CultureInfo.InvariantCulture, "\\u{0:x4}", (int)character);
      }

      private static void ReplaceSelection(TextSelection selection, int line, int startIndex, int length, string replacement)
      {
         ThreadHelper.ThrowIfNotOnUIThread();

         selection.MoveToLineAndOffset(line, startIndex + 1);
         selection.MoveToLineAndOffset(line, startIndex + length + 1, true);
         selection.Text = replacement;
      }

      private static string GetActiveDocumentPath()
      {
         ThreadHelper.ThrowIfNotOnUIThread();

         try
         {
            var dte = (DTE)Package.GetGlobalService(typeof(DTE));
            if (dte?.ActiveDocument?.FullName is string path)
            {
               return path;
            }
         }
         catch
         {
         }

         return null;
      }

      private static void ShowMessage(string message, MessageBoxImage image)
      {
         MessageBox.Show(message, WindowTitle, MessageBoxButton.OK, image);
      }
   }
}

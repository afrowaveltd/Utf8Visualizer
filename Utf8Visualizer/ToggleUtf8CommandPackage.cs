using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace Utf8Visualizer
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(ToggleUtf8CommandPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad("{F1536EF8-92EC-443C-9ED7-FDADF150DA82}", PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideBindingPath]
    public sealed class ToggleUtf8CommandPackage : AsyncPackage
    {
        public const string PackageGuidString = "a1b2c3d4-e5f6-7890-abcd-ef1234567890";

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            try
            {
                await ToggleUtf8Command.InitializeAsync(this);
                LogInfo("Package initialized successfully.");
            }
            catch (Exception ex)
            {
                LogError("InitializeAsync failed", ex);
                throw;
            }
        }

        private void LogInfo(string message)
        {
            try
            {
                if (GetService(typeof(SVsActivityLog)) is IVsActivityLog log)
                {
                    log.LogEntry((uint)__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION, "Utf8Visualizer", message);
                }
            }
            catch
            {
                // Fallback: nechceme aby logování samo způsobilo pád
            }
        }

        private void LogError(string message, Exception ex)
        {
            try
            {
                if (GetService(typeof(SVsActivityLog)) is IVsActivityLog log)
                {
                    log.LogEntry((uint)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR, "Utf8Visualizer", $"{message}: {ex}");
                }
            }
            catch
            {
                // Fallback
            }
        }
    }
}

using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace Utf8Visualizer
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(ToggleUtf8CommandPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class ToggleUtf8CommandPackage : AsyncPackage
    {
        public const string PackageGuidString = "a1b2c3d4-e5f6-7890-abcd-ef1234567890";

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await ToggleUtf8Command.InitializeAsync(this);
        }
    }
}

// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Reflection;
using osu.Desktop.Performance;
using osu.Framework.Platform;
using osu.Game;
using osu.Framework;
using osu.Desktop.MacOS;
using osu.Desktop.Windows;
using osu.Framework.Allocation;
using osu.Game.IPC;
using osu.Game.Performance;
using osu.Game.Utils;

namespace osu.Desktop
{
    internal partial class OsuGameDesktop : OsuGame
    {
        private ArchiveImportIPCChannel? archiveImportIPCChannel;

        [Cached(typeof(IHighPerformanceSessionManager))]
        private readonly HighPerformanceSessionManager highPerformanceSessionManager = new HighPerformanceSessionManager();

        public OsuGameDesktop(string[]? args = null)
            : base(args)
        {
        }

        public static bool IsPackageManaged => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OSU_EXTERNAL_UPDATE_PROVIDER"));

        public override bool RestartAppWhenExited()
        {
            string? exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath))
                return false;

            RestartOnExitAction = () => System.Diagnostics.Process.Start(exePath);
            return true;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (OperatingSystem.IsWindows())
            {
                LoadComponentAsync(new GameplayWinKeyBlocker(), Add);
                WindowsAssociationManager.UpdateAssociations();
            }
            else if (RuntimeInfo.OS == RuntimeInfo.Platform.macOS && !IsPackageManaged && IsDeployedBuild)
                LoadComponentAsync(new MacOSAppLocationChecker(), Add);

            archiveImportIPCChannel = new ArchiveImportIPCChannel(Host, this);
        }

        public override void SetHost(GameHost host)
        {
            base.SetHost(host);

            // Apple operating systems use a better icon provided via external assets.
            if (!RuntimeInfo.IsApple)
            {
                var iconStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(GetType(), "lazer.ico");
                if (iconStream != null)
                    host.Window.SetIconFromStream(iconStream);
            }

            host.Window.Title = Name;
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            archiveImportIPCChannel?.Dispose();
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using WUApiLib;

namespace Wu_change.Core
{
    public class UpdateInstallService
    {
        private readonly WuaSession _session;

        public event Action<string>? StatusChanged;

        public UpdateInstallService(WuaSession session)
        {
            _session = session;
        }

        /// <summary>
        /// Download then install a collection of updates.
        /// </summary>
        public async Task<bool> DownloadAndInstallAsync(
            IUpdateCollection wuaUpdates,
            CancellationToken ct = default)
        {
            return await Task.Run(() =>
            {
                // Download
                StatusChanged?.Invoke("Downloading updates...");
                var downloader = (UpdateDownloader)_session.CreateDownloader();
                downloader.Updates = (UpdateCollection)wuaUpdates;
                IDownloadResult dlResult = downloader.Download();

                if (dlResult.ResultCode == OperationResultCode.orcFailed)
                {
                    StatusChanged?.Invoke("Download failed.");
                    return false;
                }

                ct.ThrowIfCancellationRequested();

                // Install
                StatusChanged?.Invoke("Installing updates...");
                var installer = (UpdateInstaller)_session.CreateInstaller();
                installer.Updates = (UpdateCollection)wuaUpdates;
                installer.AllowSourcePrompts = false;
                installer.ForceQuiet = true;

                IInstallationResult installResult = installer.Install();

                bool rebootRequired = installResult.RebootRequired;
                StatusChanged?.Invoke(rebootRequired
                    ? "Updates installed. Restart required."
                    : "Updates installed successfully.");

                return installResult.ResultCode == OperationResultCode.orcSucceeded;
            }, ct);
        }

}
}

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

                IInstallationResult installResult = installer.Install();

                bool rebootRequired = installResult.RebootRequired;
                bool success = installResult.ResultCode == OperationResultCode.orcSucceeded
                            || installResult.ResultCode == OperationResultCode.orcSucceededWithErrors;

                if (!success)
                {
                    // Log per-update result codes to help diagnose what failed
                    for (int i = 0; i < wuaUpdates.Count; i++)
                    {
                        var r = installResult.GetUpdateResult(i);
                        if (r.ResultCode != OperationResultCode.orcSucceeded)
                            StatusChanged?.Invoke(
                                $"Failed: {wuaUpdates[i].Title} (HRESULT 0x{r.HResult:X8})");
                    }
                    StatusChanged?.Invoke(
                        $"Install failed (code: {installResult.ResultCode}). Try running Windows Update directly.");
                    return false;
                }

                StatusChanged?.Invoke(rebootRequired
                    ? "Updates installed. A restart is required to finish applying them."
                    : "Updates installed successfully.");

                return true;
            }, ct);
        }

}
}

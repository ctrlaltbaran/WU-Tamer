using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WUApiLib;
using WuTamer.Core.Models;

namespace WuTamer.Core
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
        /// Download then install the given updates. Items without a Windows Update handle are skipped.
        /// </summary>
        public async Task<bool> DownloadAndInstallAsync(
            IEnumerable<UpdateItem> items,
            CancellationToken ct = default)
        {
            return await Task.Run(() =>
            {
                var wuaUpdates = new UpdateCollection();
                foreach (var item in items.Where(i => i.RawUpdate != null))
                    wuaUpdates.Add(item.RawUpdate);

                // Download
                StatusChanged?.Invoke("Downloading updates...");
                var downloader = (UpdateDownloader)_session.CreateDownloader();
                downloader.Updates = wuaUpdates;
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
                installer.Updates = wuaUpdates;
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

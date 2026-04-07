using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WUApiLib;
using Wu_change.Models;

namespace Wu_change.Core
{
    public class UpdateSearchService
    {
        private readonly WuaSession _session;
        private const string MicrosoftUpdateServiceId = "7971f918-a847-4430-9279-4a52d1efe18d";

        public event Action<string>? StatusChanged;

        public UpdateSearchService(WuaSession session)
        {
            _session = session;
        }

        private void EnsureMicrosoftUpdateRegistered()
        {
            try
            {
                var svcManager = new UpdateServiceManager();
                svcManager.AddService2(MicrosoftUpdateServiceId,
                    (int)(tagAddServiceFlag.asfAllowOnlineRegistration |
                          tagAddServiceFlag.asfAllowPendingRegistration |
                          tagAddServiceFlag.asfRegisterServiceWithAU), "");
            }
            catch { }
        }

        private UpdateSearcher CreateMicrosoftUpdateSearcher()
        {
            var searcher = (UpdateSearcher)_session.CreateSearcher();
            searcher.IncludePotentiallySupersededUpdates = true;
            try
            {
                searcher.ServerSelection = ServerSelection.ssDefault;
                searcher.ServiceID = MicrosoftUpdateServiceId;
            }
            catch
            {
                searcher.ServerSelection = ServerSelection.ssDefault;
            }
            return searcher;
        }

        public async Task<List<UpdateItem>> SearchOnlineAsync(
            bool includeSoftware = true,
            bool includeDrivers = true,
            CancellationToken ct = default)
        {
            return await Task.Run(() =>
            {
                EnsureMicrosoftUpdateRegistered();

                StatusChanged?.Invoke("Connecting to Windows Update...");
                var searcher = CreateMicrosoftUpdateSearcher();

                // DeploymentAction=* captures all types including optional/preview KBs (same as WuMgr)
                // Include hidden updates too so user can unhide them
                string query = "(IsInstalled=0 and IsHidden=0 and DeploymentAction=*) or (IsHidden=1 and DeploymentAction=*)";

                StatusChanged?.Invoke("Searching for updates...");
                ISearchResult result = searcher.Search(query);
                System.Diagnostics.Debug.WriteLine($"Found: {result.Updates.Count} updates");

                var items = MapResults(result.Updates);
                StatusChanged?.Invoke("Search complete.");
                return items;

            }, ct);
        }

        public async Task<List<UpdateItem>> SearchOfflineAsync(string cabPath, CancellationToken ct = default)
        {
            return await Task.Run(() =>
            {
                StatusChanged?.Invoke("Loading offline catalog...");
                var searcher = (UpdateSearcher)_session.CreateSearcher();
                searcher.ServerSelection = ServerSelection.ssOthers;
                var manager = new UpdateServiceManager();
                var service = manager.AddScanPackageService("Offline Sync Service", cabPath);
                searcher.ServiceID = service.ServiceID;

                StatusChanged?.Invoke("Scanning against offline catalog...");
                ISearchResult result = searcher.Search("IsInstalled=0");
                return MapResults(result.Updates);
            }, ct);
        }

        public async Task<List<UpdateItem>> GetHistoryAsync(int maxCount = 100, CancellationToken ct = default)
        {
            return await Task.Run(() =>
            {
                var searcher = (UpdateSearcher)_session.CreateSearcher();
                int total = searcher.GetTotalHistoryCount();
                int count = Math.Min(total, maxCount);
                var history = searcher.QueryHistory(0, count);
                var items = new List<UpdateItem>();

                for (int i = 0; i < history.Count; i++)
                {
                    var entry = history[i];
                    var item = new UpdateItem
                    {
                        Id = entry.UpdateIdentity.UpdateID,
                        Title = entry.Title,
                        Description = entry.Description,
                        Status = UpdateStatus.Installed,
                        LastDeploymentChangeTime = entry.Date,
                        GroupName = ExtractGroupNameFromTitle(entry.Title),
                        IsDriver = entry.Title.Contains("Driver", StringComparison.OrdinalIgnoreCase)
                    };

                    // Extract KB number from title (e.g. "... (KB5048685)")
                    var kbMatch = Regex.Match(entry.Title, @"KB(\d+)", RegexOptions.IgnoreCase);
                    if (kbMatch.Success)
                        item.KBArticleIds.Add(kbMatch.Groups[1].Value);

                    items.Add(item);
                }
                return items;
            }, ct);
        }

        /// <summary>
        /// Hide or unhide an update. update.IsHidden = true prevents it from being installed.
        /// </summary>
        public async Task HideUpdateAsync(UpdateItem item, bool hide)
        {
            await Task.Run(() =>
            {
                if (item.RawUpdate == null) return;
                item.RawUpdate.IsHidden = hide;
                item.IsHidden = hide;
                item.Status = hide ? UpdateStatus.Hidden : UpdateStatus.Available;
            });
        }

        private List<UpdateItem> MapResults(IUpdateCollection updates)
        {
            var items = new List<UpdateItem>();
            for (int i = 0; i < updates.Count; i++)
            {
                var u = updates[i];
                var item = new UpdateItem
                {
                    RawUpdate = u,
                    Id = u.Identity.UpdateID,
                    Title = u.Title,
                    Description = u.Description,
                    Category = GetCategory(u.Categories),
                    GroupName = GetGroupName(u.Categories),
                    RequiresReboot = u.InstallationBehavior.RebootBehavior != InstallationRebootBehavior.irbNeverReboots,
                    IsHidden = u.IsHidden,
                    Status = u.IsHidden ? UpdateStatus.Hidden : UpdateStatus.Available,
                    LastDeploymentChangeTime = u.LastDeploymentChangeTime
                };

                item.Severity = u.MsrcSeverity?.ToLower() switch
                {
                    "critical"  => UpdateSeverity.Critical,
                    "important" => UpdateSeverity.Important,
                    "moderate"  => UpdateSeverity.Moderate,
                    "low"       => UpdateSeverity.Low,
                    _           => UpdateSeverity.Unknown
                };

                for (int c = 0; c < u.Categories.Count; c++)
                {
                    var cat = u.Categories[c];
                    if (cat.Name.Contains("Driver", StringComparison.OrdinalIgnoreCase))
                        item.IsDriver = true;
                    if (cat.Name.Contains("Firmware", StringComparison.OrdinalIgnoreCase))
                        item.IsFirmware = true;
                }

                for (int k = 0; k < u.KBArticleIDs.Count; k++)
                    item.KBArticleIds.Add(u.KBArticleIDs[k]);

                if (u.MaxDownloadSize > 0)
                    item.SizeMB = (double)u.MaxDownloadSize / (1024.0 * 1024.0);

                items.Add(item);
            }
            return items;
        }

        /// <summary>
        /// Returns "Product; Classification" string — same logic as WuMgr's GetCategory.
        /// </summary>
        private static string GetCategory(ICategoryCollection cats)
        {
            string classification = "";
            string product = "";
            foreach (ICategory cat in cats)
            {
                if (cat.Type.Equals("UpdateClassification"))
                    classification = cat.Name;
                else if (cat.Type.Equals("Product"))
                    product = cat.Name;
            }
            return product.Length == 0 ? classification : $"{product}; {classification}";
        }

        /// <summary>
        /// Returns the top-level group name for UI grouping (e.g. "Drivers", "Definition Updates", "Updates").
        /// </summary>
        private static string GetGroupName(ICategoryCollection cats)
        {
            foreach (ICategory cat in cats)
            {
                if (cat.Type.Equals("UpdateClassification"))
                    return cat.Name;
            }
            return "Updates";
        }

        /// <summary>
        /// Extract a group name from a history entry's title by pattern matching.
        /// Since history entries don't have WUA category data, we infer from the title.
        /// </summary>
        private static string ExtractGroupNameFromTitle(string title)
        {
            // Microsoft Store app updates: title starts with a package family name ID
            // (uppercase alphanumeric block with no spaces, e.g. "9MWPM2CQNLHN-Microsoft.GamingServices")
            if (Regex.IsMatch(title, @"^[A-Z0-9]{5,}-"))
                return "Store Apps";

            // Hardware driver updates: explicit keyword OR the common WU driver naming convention
            // "Manufacturer - ComponentType - Version" (e.g. "Logitech - HIDClass - 1.10.92.0")
            if (title.Contains("Driver", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Firmware", StringComparison.OrdinalIgnoreCase) ||
                Regex.IsMatch(title, @"^.+\s-\s\w+\s-\s\d+\.\d+"))
                return "Drivers";

            if (title.Contains("Definition", StringComparison.OrdinalIgnoreCase))
                return "Definition Updates";

            if (title.Contains("Critical", StringComparison.OrdinalIgnoreCase))
                return "Critical Updates";

            if (title.Contains("Security", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Defender", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Antimalware", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Malicious Software", StringComparison.OrdinalIgnoreCase))
                return "Security Updates";

            return "Updates";
        }
    }
}

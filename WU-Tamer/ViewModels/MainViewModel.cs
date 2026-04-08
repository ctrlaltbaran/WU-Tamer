using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Wu_change.Views;
using Wu_change.Core;
using Wu_change.Models;
using Wu_change.Services;

namespace Wu_change.ViewModels
{
    public class UpdateGroup
    {
        public string GroupName { get; set; } = string.Empty;
        public ObservableCollection<UpdateItem> Items { get; set; } = new();
    }

    public class HistoryKbGroup : INotifyPropertyChanged
    {
        public UpdateItem Latest { get; }
        public List<UpdateItem> OlderAttempts { get; }
        public bool HasMultiple => OlderAttempts.Count > 0;

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(); }
        }

        public ICommand ToggleCommand { get; }

        public HistoryKbGroup(UpdateItem latest, IEnumerable<UpdateItem> older)
        {
            Latest = latest;
            OlderAttempts = older.ToList();
            ToggleCommand = new RelayCommand(_ => IsExpanded = !IsExpanded);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class HistoryUpdateGroup
    {
        public string GroupName { get; set; } = string.Empty;
        public ObservableCollection<HistoryKbGroup> Items { get; set; } = new();
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly WuaSession _session;
        private readonly UpdateSearchService _searcher;
        private readonly UpdateInstallService _installer;
        private readonly RestorePointService _restorePoint;
        private CancellationTokenSource? _cts;
        private List<UpdateItem> _allResults = new();

        public ObservableCollection<UpdateGroup> UpdateGroups { get; } = new();
        public ObservableCollection<UpdateItem> History { get; } = new();
        public ObservableCollection<HistoryUpdateGroup> HistoryGroups { get; } = new();
        public ObservableCollection<HistoryKbGroup> HistoryFlat { get; } = new();

        private List<UpdateItem> _allHistory = new();

        private bool _historyIsGrouped = true;
        public bool HistoryIsGrouped
        {
            get => _historyIsGrouped;
            set { _historyIsGrouped = value; OnPropertyChanged(); ApplyHistoryFilter(); }
        }

        private Dictionary<string, bool> _historyGroupFilter = new();

        public ICommand ScanCommand { get; }
        public ICommand InstallSelectedCommand { get; }
        public ICommand HideSelectedCommand { get; }
        public ICommand UnhideSelectedCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand LoadHistoryCommand { get; }
        public ICommand ToggleDarkModeCommand { get; }

        private string _statusText = "Ready — click Scan to check for updates.";
        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNotBusy));
                (ScanCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (InstallSelectedCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (HideSelectedCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (UnhideSelectedCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (CancelCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (LoadHistoryCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
        public bool IsNotBusy => !_isBusy;

        private bool _hasNoUpdates = true;
        public bool HasNoUpdates
        {
            get => _hasNoUpdates;
            set { _hasNoUpdates = value; OnPropertyChanged(); }
        }

        private bool _isDarkMode;
        public bool IsDarkMode
        {
            get => _isDarkMode;
            set { _isDarkMode = value; OnPropertyChanged(); }
        }

        private bool _includeDrivers = true;
        public bool IncludeDrivers
        {
            get => _includeDrivers;
            set { _includeDrivers = value; OnPropertyChanged(); ApplyFilter(); }
        }

        private bool _includeSoftware = true;
        public bool IncludeSoftware
        {
            get => _includeSoftware;
            set { _includeSoftware = value; OnPropertyChanged(); ApplyFilter(); }
        }

        public void ToggleHistoryGroupFilter(string groupName)
        {
            if (!_historyGroupFilter.ContainsKey(groupName))
                _historyGroupFilter[groupName] = true;
            else
                _historyGroupFilter[groupName] = !_historyGroupFilter[groupName];

            ApplyHistoryFilter();
        }

        public Dictionary<string, bool> HistoryGroupFilter => _historyGroupFilter;

        private static bool DetectSystemDarkMode()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                // AppsUseLightTheme = 0 means dark mode
                return key?.GetValue("AppsUseLightTheme") is int v && v == 0;
            }
            catch { return false; }
        }

        public MainViewModel()
        {
            _isDarkMode = DetectSystemDarkMode();

            _session = new WuaSession();
            _searcher = new UpdateSearchService(_session);
            _installer = new UpdateInstallService(_session);
            _restorePoint = new RestorePointService();

            _searcher.StatusChanged += msg => Application.Current.Dispatcher.Invoke(() => StatusText = msg);
            _installer.StatusChanged += msg => Application.Current.Dispatcher.Invoke(() => StatusText = msg);
            _restorePoint.StatusChanged += msg => Application.Current.Dispatcher.Invoke(() => StatusText = msg);

            ScanCommand = new RelayCommand(async _ => await ScanAsync(), _ => IsNotBusy);
            InstallSelectedCommand = new RelayCommand(async _ => await InstallSelectedAsync(), _ => IsNotBusy);
            HideSelectedCommand = new RelayCommand(async _ => await HideSelectedAsync(true), _ => IsNotBusy);
            UnhideSelectedCommand = new RelayCommand(async _ => await HideSelectedAsync(false), _ => IsNotBusy);
            CancelCommand = new RelayCommand(_ => Cancel(), _ => IsBusy);
            LoadHistoryCommand = new RelayCommand(async _ => await LoadHistoryAsync(), _ => IsNotBusy);
            ToggleDarkModeCommand   = new RelayCommand(_ => IsDarkMode = !IsDarkMode);
        }

        private void ApplyFilter()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateGroups.Clear();

                var filtered = _allResults.Where(u =>
                    (u.IsDriver || u.IsFirmware) ? _includeDrivers : _includeSoftware);

                foreach (var group in filtered.GroupBy(u => u.GroupName).OrderBy(g => g.Key))
                {
                    var ug = new UpdateGroup { GroupName = group.Key };
                    foreach (var item in group.OrderBy(u => u.Title))
                        ug.Items.Add(item);
                    UpdateGroups.Add(ug);
                }

                HasNoUpdates = UpdateGroups.Count == 0;

                int visible = UpdateGroups.Sum(g => g.Items.Count);
                int hidden = _allResults.Count(u => u.IsHidden);

                if (_allResults.Count > 0)
                    StatusText = $"Showing {visible} of {_allResults.Count} update(s)" +
                                 (hidden > 0 ? $" · {hidden} hidden" : "");
            });
        }

        public async Task ScanAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            _allResults.Clear();
            Application.Current.Dispatcher.Invoke(() => { UpdateGroups.Clear(); HasNoUpdates = true; });
            _cts = new CancellationTokenSource();

            try
            {
                var results = await _searcher.SearchOnlineAsync(true, true, _cts.Token);
                _allResults = results;
                ApplyFilter();

                int total = results.Count;
                int hidden = results.Count(u => u.IsHidden);
                StatusText = total == 0
                    ? "✓ Your system is up to date."
                    : $"Found {total} update(s)" + (hidden > 0 ? $" · {hidden} hidden" : "");
            }
            catch (OperationCanceledException) { StatusText = "Scan cancelled."; }
            catch (Exception ex) { StatusText = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        private async Task HideSelectedAsync(bool hide)
        {
            IsBusy = true;
            try
            {
                var selected = UpdateGroups.SelectMany(g => g.Items)
                                           .Where(u => u.IsSelected).ToList();
                if (selected.Count == 0) { StatusText = "No updates selected."; return; }

                foreach (var item in selected)
                {
                    await _searcher.HideUpdateAsync(item, hide);
                    item.IsSelected = false;
                }

                StatusText = hide
                    ? $"Hidden {selected.Count} update(s)."
                    : $"Unhidden {selected.Count} update(s).";
            }
            catch (Exception ex) { StatusText = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        private async Task InstallSelectedAsync()
        {
            IsBusy = true;
            _cts = new CancellationTokenSource();
            try
            {
                var selected = UpdateGroups.SelectMany(g => g.Items)
                                           .Where(u => u.IsSelected && !u.IsHidden && u.RawUpdate != null)
                                           .ToList();
                if (selected.Count == 0) { StatusText = "No updates selected."; return; }

                var installAnswer = ThemedMessageBox.Show(
                    "Create a System Restore point before installing?",
                    "Restore Point",
                    IsDarkMode);
                if (installAnswer == MessageBoxResult.Cancel) return;
                if (installAnswer == MessageBoxResult.Yes)
                    await _restorePoint.CreateAsync("WU-Tamer — before installing updates", _cts.Token);

                var collection = new WUApiLib.UpdateCollection();
                foreach (var item in selected)
                    collection.Add(item.RawUpdate);

                bool success = await _installer.DownloadAndInstallAsync(collection, _cts.Token);
                if (success)
                {
                    foreach (var item in selected)
                    {
                        item.Status = UpdateStatus.Installed;
                        item.IsSelected = false;
                    }
                }
            }
            catch (OperationCanceledException) { StatusText = "Cancelled."; }
            catch (Exception ex) { StatusText = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        public async Task LoadHistoryAsync()
        {
            IsBusy = true;
            Application.Current.Dispatcher.Invoke(() => History.Clear());
            try
            {
                var results = await _searcher.GetHistoryAsync();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var item in results)
                        History.Add(item);

                    _allHistory = results;

                    // Build filter dictionary with all group names found in history
                    _historyGroupFilter.Clear();
                    foreach (var groupName in results.Select(u => u.GroupName).Distinct().OrderBy(g => g))
                        _historyGroupFilter[groupName] = true;  // All groups visible by default

                    // Apply filtering, sorting, and grouping/flat view
                    ApplyHistoryFilter();
                });
                StatusText = $"Loaded {results.Count} history entries.";
            }
            catch (Exception ex) { StatusText = $"Error loading history: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        public void Cancel() => _cts?.Cancel();

        private void ApplyHistoryFilter()
        {
            // Filter by group visibility, then sort newest-first
            var filtered = _allHistory
                .Where(u => _historyGroupFilter.ContainsKey(u.GroupName) && _historyGroupFilter[u.GroupName])
                .OrderByDescending(u => u.LastDeploymentChangeTime)
                .ToList();

            var kbGroups = BuildKbGroups(filtered);

            Application.Current.Dispatcher.Invoke(() =>
            {
                HistoryFlat.Clear();
                foreach (var kg in kbGroups)
                    HistoryFlat.Add(kg);

                if (HistoryIsGrouped)
                    GroupHistoryItems(filtered);
            });
        }

        // Group a flat list of UpdateItems by KB (or title), newest attempt first per group.
        private static List<HistoryKbGroup> BuildKbGroups(IEnumerable<UpdateItem> items)
        {
            return items
                .GroupBy(u => u.KBArticleIds.Count > 0 ? u.KBArticleIds[0] : u.Title)
                .Select(g =>
                {
                    var sorted = g.OrderByDescending(u => u.LastDeploymentChangeTime).ToList();
                    return new HistoryKbGroup(sorted[0], sorted.Skip(1));
                })
                .OrderByDescending(g => g.Latest.LastDeploymentChangeTime)
                .ToList();
        }

        private static int GroupSortPriority(string groupName) => groupName switch
        {
            "Updates"            => 0,
            "Security Updates"   => 1,
            "Critical Updates"   => 2,
            "Definition Updates" => 3,
            "Store Apps"         => 4,
            "Drivers"            => 5,
            _                    => 6
        };

        private void GroupHistoryItems(List<UpdateItem> items)
        {
            HistoryGroups.Clear();

            // Items are already sorted newest-first; group by category with Windows updates first
            foreach (var group in items.GroupBy(u => u.GroupName).OrderBy(g => GroupSortPriority(g.Key)))
            {
                var ug = new HistoryUpdateGroup { GroupName = group.Key };
                foreach (var kg in BuildKbGroups(group))
                    ug.Items.Add(kg);
                HistoryGroups.Add(ug);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object? parameter) => _execute(parameter);
    }
}

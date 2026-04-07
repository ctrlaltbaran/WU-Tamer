using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WUApiLib;

namespace Wu_change.Models
{
    public enum UpdateSeverity { Unknown, Low, Moderate, Important, Critical }
    public enum UpdateStatus { Available, Downloading, Downloaded, Installing, Installed, Failed, Hidden }

    public class UpdateItem : INotifyPropertyChanged
    {
        // Raw COM reference — needed for hide/unhide
        internal IUpdate? RawUpdate { get; set; }

        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public UpdateSeverity Severity { get; set; }
        public bool RequiresReboot { get; set; }
        public bool IsDriver { get; set; }
        public bool IsFirmware { get; set; }
        public bool IsOptional { get; set; }
        public double SizeMB { get; set; }
        public DateTime? LastDeploymentChangeTime { get; set; }
        public List<string> KBArticleIds { get; set; } = new();

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        private bool _isHidden;
        public bool IsHidden
        {
            get => _isHidden;
            set
            {
                _isHidden = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusString));
            }
        }

        private UpdateStatus _status;
        public UpdateStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusString));
            }
        }

        private int _downloadProgress;
        public int DownloadProgress
        {
            get => _downloadProgress;
            set { _downloadProgress = value; OnPropertyChanged(); }
        }

        public string KBString => KBArticleIds.Count > 0 ? $"KB{KBArticleIds[0]}" : "KBUnknown";
        public string SizeString => SizeMB < 1 ? $"{SizeMB * 1024:F0} KB" : $"{SizeMB:F1} MB";

        public string SeverityString => Severity switch
        {
            UpdateSeverity.Critical => "Critical",
            UpdateSeverity.Important => "Important",
            UpdateSeverity.Moderate => "Moderate",
            UpdateSeverity.Low => "Low",
            _ => ""
        };

        public string StatusString => IsHidden ? "Hidden" : Status switch
        {
            UpdateStatus.Installed => "Installed",
            UpdateStatus.Failed => "Failed",
            _ => IsOptional ? "Pending (!)" : "Pending"
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

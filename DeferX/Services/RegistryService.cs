using System;
using Microsoft.Win32;

namespace Wu_change.Services
{
    /// <summary>
    /// Read and write Windows Update policy settings via the registry.
    /// Requires administrator privileges for write operations.
    /// </summary>
    public static class RegistryService
    {
        private const string WuPolicyKey = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate";
        private const string WuAuPolicyKey = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU";

        public static void SetFeatureUpdateDeferralDays(int days)
        {
            using var key = Registry.LocalMachine.CreateSubKey(WuPolicyKey);
            key.SetValue("DeferFeatureUpdatesPeriodInDays", days, RegistryValueKind.DWord);
            key.SetValue("DeferFeatureUpdates", 1, RegistryValueKind.DWord);
        }

        public static void SetQualityUpdateDeferralDays(int days)
        {
            using var key = Registry.LocalMachine.CreateSubKey(WuPolicyKey);
            key.SetValue("DeferQualityUpdatesPeriodInDays", days, RegistryValueKind.DWord);
            key.SetValue("DeferQualityUpdates", 1, RegistryValueKind.DWord);
        }

        public static int GetFeatureUpdateDeferralDays()
        {
            using var key = Registry.LocalMachine.OpenSubKey(WuPolicyKey);
            return (int)(key?.GetValue("DeferFeatureUpdatesPeriodInDays") ?? 0);
        }

        public static int GetQualityUpdateDeferralDays()
        {
            using var key = Registry.LocalMachine.OpenSubKey(WuPolicyKey);
            return (int)(key?.GetValue("DeferQualityUpdatesPeriodInDays") ?? 0);
        }

        public static void SetMsUpdateServerBlocked(bool blocked)
        {
            using var key = Registry.LocalMachine.CreateSubKey(WuPolicyKey);
            if (blocked)
            {
                key.SetValue("DisableWindowsUpdateAccess", 1, RegistryValueKind.DWord);
                key.SetValue("WUServer", "http://localhost", RegistryValueKind.String);
                key.SetValue("WUStatusServer", "http://localhost", RegistryValueKind.String);
            }
            else
            {
                key.DeleteValue("DisableWindowsUpdateAccess", throwOnMissingValue: false);
                key.DeleteValue("WUServer", throwOnMissingValue: false);
                key.DeleteValue("WUStatusServer", throwOnMissingValue: false);
            }
        }

        public static bool IsMsUpdateServerBlocked()
        {
            using var key = Registry.LocalMachine.OpenSubKey(WuPolicyKey);
            return (int)(key?.GetValue("DisableWindowsUpdateAccess") ?? 0) == 1;
        }

        public enum AuOption
        {
            NotConfigured = 0,
            Disabled = 1,
            NotifyBeforeDownload = 2,
            NotifyBeforeInstall = 3,
            ScheduledInstall = 4
        }

        public static void SetAutoUpdateOption(AuOption option)
        {
            using var key = Registry.LocalMachine.CreateSubKey(WuAuPolicyKey);
            key.SetValue("AUOptions", (int)option, RegistryValueKind.DWord);
            key.SetValue("NoAutoUpdate", option == AuOption.Disabled ? 1 : 0, RegistryValueKind.DWord);
        }

        public static AuOption GetAutoUpdateOption()
        {
            using var key = Registry.LocalMachine.OpenSubKey(WuAuPolicyKey);
            int val = (int)(key?.GetValue("AUOptions") ?? 0);
            return Enum.IsDefined(typeof(AuOption), val) ? (AuOption)val : AuOption.NotConfigured;
        }

        private const string ActiveHoursKey = @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings";

        public static void SetActiveHours(int startHour, int endHour)
        {
            using var key = Registry.LocalMachine.CreateSubKey(ActiveHoursKey);
            key.SetValue("ActiveHoursStart", startHour, RegistryValueKind.DWord);
            key.SetValue("ActiveHoursEnd", endHour, RegistryValueKind.DWord);
        }

        public static (int start, int end) GetActiveHours()
        {
            using var key = Registry.LocalMachine.OpenSubKey(ActiveHoursKey);
            int start = (int)(key?.GetValue("ActiveHoursStart") ?? 8);
            int end = (int)(key?.GetValue("ActiveHoursEnd") ?? 17);
            return (start, end);
        }
    }
}

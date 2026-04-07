using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Wu_change.Services
{
    public class RestorePointService
    {
        public event Action<string>? StatusChanged;

        /// <summary>
        /// Creates a Windows System Restore point via PowerShell's Checkpoint-Computer.
        /// Requires System Restore to be enabled on the system drive.
        /// Windows throttles creation to once per 24 hours by default; a throttle failure
        /// is treated as a soft warning rather than a hard error.
        /// </summary>
        public async Task<bool> CreateAsync(string description, CancellationToken ct = default)
        {
            return await Task.Run(() =>
            {
                StatusChanged?.Invoke("Creating restore point...");

                string command = $"Checkpoint-Computer -Description '{description}' -RestorePointType MODIFY_SETTINGS";
                var psi = new ProcessStartInfo("powershell.exe",
                    $"-NonInteractive -NoProfile -Command \"{command}\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true
                };

                using var process = Process.Start(psi)
                    ?? throw new InvalidOperationException("Failed to start powershell.exe");

                string stderr = process.StandardError.ReadToEnd();

                while (!process.WaitForExit(500))
                    ct.ThrowIfCancellationRequested();

                if (process.ExitCode == 0)
                {
                    StatusChanged?.Invoke("Restore point created.");
                    return true;
                }

                // Windows throttles restore point creation to once per 24 hours by default.
                // Treat this as a non-fatal warning so the operation can continue.
                if (stderr.Contains("0x80042306", StringComparison.OrdinalIgnoreCase) ||
                    stderr.Contains("too frequently", StringComparison.OrdinalIgnoreCase))
                {
                    StatusChanged?.Invoke("Restore point skipped (one already exists today).");
                    return true;
                }

                StatusChanged?.Invoke($"Restore point failed (exit {process.ExitCode}). Continuing anyway.");
                return false;
            }, ct);
        }
    }
}

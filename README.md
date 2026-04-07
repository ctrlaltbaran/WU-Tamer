# DeferX — Windows Update Manager

A lightweight WPF application for managing Windows updates on your PC with fine-grained control. Scan for updates, install, hide, unhide, and roll back updates all from one interface.

## Features

### 📋 Update Management
- **Scan for Updates** — Search Windows Update and Microsoft Update for available updates (software, drivers, and firmware)
- **Install Selected** — Download and install chosen updates with a single click. Optionally create a System Restore point before installing
- **Hide/Unhide** — Hide unwanted updates to prevent automatic installation, or unhide them later

### 🔄 System Restore Points
Before installing updates, you'll be prompted with a three-option dialog:
- **Yes** — Create a System Restore point, then proceed
- **No** — Skip the restore point and proceed immediately
- **Cancel** — Abort the operation entirely

Windows throttles restore point creation to once per 24 hours by default; if one already exists today, the app treats this as a soft pass and continues.

### 📜 Update History
- **Load History** — View the last 100 installed updates with their KB article numbers and installation dates
- **Grouped View** — Organize history by update category (Drivers, Security Updates, Definition Updates, etc.)
- **Flat List View** — See all history items in a single sortable list
- **Sort & Filter** — Sort by title or date, and filter by update type

### 🎨 Dark Mode
Click the 💡 lightbulb icon in the top-right toolbar to toggle between light and dark themes. The bulb is full-brightness in light mode and dimmed in dark mode.

### 🏷️ Filtering
- **Software** toggle — Show/hide software updates
- **Drivers** toggle — Show/hide driver and firmware updates

Updates are organized by category (Definition Updates, Critical Updates, Security Updates, etc.) and displayed in a grouped list view.

---

## Usage

### Installation & Running
1. Build the solution: `dotnet build`
2. Run the executable (requires **Administrator privileges** — see `app.manifest`)
3. The app will initialize WUA (Windows Update Agent) on first launch

### Typical Workflow

#### Installing Updates
1. Click **Scan for Updates**
2. Wait for the search to complete (may take 30–60 seconds)
3. Review the list of available updates
4. Check the boxes next to updates you want to install
5. Click **Install Selected**
6. When prompted, choose whether to create a System Restore point
7. The app will download and install selected updates
8. If a restart is required, you'll be notified in the status bar

#### Hiding Updates
1. In the **Available Updates** tab, find the update(s) you want to hide
2. Check their boxes
3. Click **Hide**
4. Hidden updates appear dimmed and won't be installed automatically
5. To unhide: select the update and click **Unhide**

---

## Architecture

### Key Components

**`Core/`**
- `WuaSession.cs` — Wraps the WUA COM API (`WUApiLib.dll`)
- `UpdateSearchService.cs` — Searches for available/hidden updates and manages hide/unhide state
- `UpdateInstallService.cs` — Downloads and installs updates via the WUA API

**`Services/`**
- `RestorePointService.cs` — Creates Windows System Restore points via PowerShell's `Checkpoint-Computer` cmdlet
- `RegistryService.cs` — (Included for future use) Can read/write Windows Update policy registry keys

**`Models/`**
- `UpdateItem.cs` — Data model representing a single update (title, KB number, severity, size, installation status, etc.)

**`ViewModels/`**
- `MainViewModel.cs` — MVVM view model managing UI state, commands, and business logic

**`UI/`**
- `MainWindow.xaml / .xaml.cs` — Main WPF window with grouped update list view, history tab, and toolbar
- `Themes/Dark.xaml`, `Light.xaml` — Theme resource dictionaries for light and dark modes

### Technology Stack
- **Language:** C# (.NET 10.0 for Windows)
- **UI Framework:** WPF (Windows Presentation Foundation)
- **Update Integration:** Windows Update Agent (WUA) COM API
- **System Restore:** PowerShell `Checkpoint-Computer` cmdlet
- **Privileges:** Requires Administrator rights (enforced via `app.manifest`)

---

## System Requirements

- **OS:** Windows 10 or Windows 11
- **.NET:** .NET 10.0 Runtime for Windows
- **Privileges:** Administrator rights (required for install, hide, and unhide operations)
- **System Restore:** Must be enabled on the system drive to create restore points

---

## Known Limitations

- **Restore Point Throttling:** Windows limits restore point creation to once per 24 hours by default. If a restore point already exists on the current day, the app will note this and continue without creating a new one.
- **History KB Extraction:** KB article numbers are extracted from update titles using regex pattern matching (`KB\d+`). Some updates may not have KB numbers if they're not embedded in the title.
- **Offline Catalogs:** The app supports offline update catalogs (`.cab` files) via `SearchOfflineAsync`, but this feature is not exposed in the UI yet.

---

## Troubleshooting

### "Access Denied" or Permission Errors
- Run the app as Administrator. Right-click the executable and select "Run as Administrator"

### Updates Keep Reinstalling
- After rolling back an update, you **must hide it** to prevent Windows from automatically reinstalling it on the next scan
- Use the **Hide** button in the Available Updates tab

### Restore Point Creation Fails
- Ensure System Restore is enabled: Settings → System → System protection
- Check that you haven't created a restore point in the last 24 hours (Windows throttling)
- Ensure the system drive has sufficient free space (at least 3% of disk size recommended)

### Status Bar Shows "Failed to Remove KB..."
- The update may not be currently installed
- The update may require a restart to uninstall (try restarting and running the app again)
- Some system updates cannot be uninstalled via `wusa.exe`

---

## Development Notes

### Building from Source
```bash
cd DeferX
dotnet build -c Release
dotnet publish -c Release -o ./bin/publish
```

The published executable is stand-alone and includes the .NET runtime.

### Code Style
- Uses MVVM pattern with `INotifyPropertyChanged` for data binding
- Async/await for all long-running operations (search, download, install)
- Theme-aware colors via `DynamicResource` bindings (no hardcoded colors in XAML except accent colors)

### Adding New Features
- New service classes should implement an `event Action<string>? StatusChanged` for UI status updates
- Async methods should accept `CancellationToken` for cancellation support
- All ViewModels should inherit from `INotifyPropertyChanged` and use `OnPropertyChanged()` for property change notifications

---

## License

[MIT License](LICENSE) — © 2026 ctrlaltbaran

---

## Credits

Built with C#, WPF, and the Windows Update Agent API with the help of Claude AI.

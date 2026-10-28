# WU-Tamer: instructions for Claude

Windows Update manager (WPF, .NET 10). Windows-only. Part of a planned Windows repair-app bundle with DrBenchy and future repair tools, so keep logic out of the UI.
House rules: `~/Projects/CLAUDE.md` (registry row in `~/Projects/_house/PROJECTS.md`). No GitHub CI or workflows: build releases by hand on Windows.

## Layout
- `WU-Tamer.Core/`: Windows Update logic, no UI code (namespace `WuTamer.Core`). Windows Update COM types (`WUApiLib`) must not appear in its public API.
- `WU-Tamer/`: the WPF app (namespace `WU_Tamer`): view model, views, themes, admin manifest, publish settings.

## Commands
- Build: `dotnet build` (from the repo root).
- Linux compile check only (can't run the app): `dotnet build -p:EnableWindowsTargeting=true`.
- Release: `dotnet publish WU-Tamer/WU-Tamer.csproj -c Release -o ./publish` on a Windows machine.
- There are no automated tests. The app needs administrator rights and a real Windows machine to try.

## Rules
- Anything that needs trying by hand goes in `docs/MANUAL_TESTS.md`.
- New services expose `event Action<string>? StatusChanged` and take a `CancellationToken`.
- The app makes no network connections of its own; only Windows Update does, when the user asks.

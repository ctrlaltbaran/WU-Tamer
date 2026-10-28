# Manual tests (on Windows, as administrator)

## After the Core/UI split (branch `core-split`)
- [ ] App starts; light/dark toggle works
- [ ] Scan finds updates; Software and Drivers toggles filter them
- [ ] Install selected: restore-point prompt Yes / No / Cancel each behave (Cancel stops everything)
- [ ] Install runs to the end; status line reports success or the failure reason
- [ ] Hide and Unhide selected updates; hidden ones show as Hidden after a rescan
- [ ] Load History: grouped and flat views, sort, filter
- [ ] `dotnet publish` still produces one self-contained .exe that runs

## Offline scan (branch `core-split`)
- [ ] Download `wsusscn2.cab` from Microsoft; **Scan offline…** opens a file picker filtered to .cab
- [ ] Cancel in the picker does nothing
- [ ] Results list shows missing updates; status says it is scan only
- [ ] Install, Hide and Unhide are disabled while offline results are shown; a normal Scan re-enables them
- [ ] Cancel during an offline scan stops it and leaves the list empty
- [ ] No "Offline Sync Service" left registered afterwards (run `(New-Object -ComObject Microsoft.Update.ServiceManager).Services | select Name` in PowerShell)
- [ ] A non-catalog .cab or missing file shows a plain error in the status line
- [ ] Note what it reports vs a normal scan (does it list drivers or optional updates?)

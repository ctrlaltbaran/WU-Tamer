# Manual tests (on Windows, as administrator)

## After the Core/UI split (branch `core-split`)
- [ ] App starts; light/dark toggle works
- [ ] Scan finds updates; Software and Drivers toggles filter them
- [ ] Install selected: restore-point prompt Yes / No / Cancel each behave (Cancel stops everything)
- [ ] Install runs to the end; status line reports success or the failure reason
- [ ] Hide and Unhide selected updates; hidden ones show as Hidden after a rescan
- [ ] Load History: grouped and flat views, sort, filter
- [ ] `dotnet publish` still produces one self-contained .exe that runs

# FE2.IO Native

No browser / webview anywhere. A native client that speaks the same
websocket protocol as mini-fe2io, with its own custom WinForms UI.

- Connects to `ws://client.fe2.io:8081`
- Sends your Roblox username
- On `bgm` messages: downloads (caches to `%USERPROFILE%\fe2io-cache`) and
  plays the track locally with NAudio
- On `gameStatus: died`: stops the current track and halves the volume
- On `gameStatus: left`: stops audio and disables the volume controls
  until the next round starts (next `bgm` message)

## UI

- Username box + Connect/Disconnect button
- Connection status + now-playing track
- Volume slider + Mute checkbox (disabled outside an active game session)
- Log panel

## Build

Double-click `build.bat`, or manually:

```
dotnet restore
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o dist
```

Output: `dist\FE2IO Native.exe`

## Notes / ideas for further work

- To restyle the UI, edit the `Controls.AddRange(...)` block and the
  control definitions in `MainForm.cs` — everything is in code, no
  separate designer file.
- To shrink RAM/size further you can try `PublishTrimmed`/`ReadyToRun`,
  but test carefully since WinForms + NAudio sometimes don't trim cleanly.
- mini-fe2io's `keybind.rs` (global hotkeys that work even while the app
  isn't focused) isn't implemented here. Say the word if you want that
  added (e.g. via `SharpHook` or the Windows hook API).

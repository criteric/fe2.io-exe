# FE2.IO Native — Rust port

Native desktop client, without a browser or WebView. Based on the supplied C# WinForms source. Uses egui/eframe, Rodio, Tokio and tungstenite. The server address and raw username handshake are unchanged. Death and leave options follow MainForm.cs (rather than the contradictory original README).

## Build

Install Rust (rustup, stable) and a platform C/C++ linker. On Windows use the MSVC toolchain and Visual Studio Build Tools with Desktop development with C++. On Fedora/Bazzite, use a Fedora development environment with the following packages (Bazzite: run inside a Fedora Distrobox, not on the immutable host):

```sh
sudo dnf install gcc gcc-c++ pkgconf-pkg-config alsa-lib-devel libX11-devel libXcursor-devel libXi-devel libXrandr-devel libxkbcommon-devel mesa-libGL-devel wayland-devel
cargo build --release
cargo run --release
```

On Debian/Ubuntu:

```sh
sudo apt install build-essential pkg-config libasound2-dev libx11-dev libxcursor-dev libxi-dev libxrandr-dev libxkbcommon-dev libgl1-mesa-dev libwayland-dev
cargo build --release
```

Windows (PowerShell): `cargo build --release`, then `./target/release/fe2io-native.exe`.
Linux: `./target/release/fe2io-native`.

## Features

- Connect / disconnect, Roblox username, same `ws://client.fe2.io:8081` protocol.
- BGM download, local playback, volume/mute, death/leave actions and log.
- Cache in `~/fe2io-cache`, keyed by SHA-256 URL (not compatible with old cache names).
- Audio downloads are cancelable and bounded to 100 MiB. Mute keeps the previous volume, and changing the slider unmutes.
- No global hotkeys (also absent from the supplied C# implementation).
- The original server uses unencrypted WebSocket; the username is sent as plain text. No passwords are required.

## Verification

Source port only. Compilation and live server/audio testing were not performed in the generation environment. Run `cargo check`, `cargo test`, and `cargo build --release` locally. Dependencies are version ranges; Cargo generates Cargo.lock on first build.

## Cross-platform build helpers

Linux: `./build.sh` checks ALSA development dependencies before building. Install packages inside the same container where Cargo runs. Windows: run `build.bat` from a terminal with Rust and MSVC Build Tools installed. No ALSA package is required on Windows; Rodio/CPAL uses WASAPI there. Linux uses the ALSA interface (which may route through PipeWire). No sound server replacement is required.

The included GitHub Actions workflow builds both targets separately and uploads their binaries when run in a GitHub repository. The workflow has not been executed here. A Linux binary and a Windows EXE are distinct artifacts.

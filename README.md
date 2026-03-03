# NSMB Editor (NSMBe5)

Forked from [MammaMiaTeam](https://github.com/MammaMiaTeam/NSMB-Editor), originally by [Dirbaio](https://github.com/Dirbaio/NSMB-Editor).

A feature-rich editor for **New Super Mario Bros. (Nintendo DS)** ROM hacking.

## Download
- Currently this fork does not have builds, you'll need to build it yourself.
- Official *MammaMiaTeam* builds: [GitHub Releases](https://github.com/MammaMiaTeam/NSMB-Editor/releases/latest/)
- Legacy builds: [NSMBHD Downloads](https://nsmbhd.net/download/all/)

## Community
- Discord: [NSMB Central](https://discord.gg/x7gr3M9)
- Forums: [NSMBHD](http://nsmbhd.net/)

## Requirements
- Windows: [.NET Framework 4.8](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48)
- Linux/macOS/other platforms: [Mono](https://www.mono-project.com/download/stable/)

## New Additions In This Fork
- ROM File Browser improvements:
  - Search field for faster file lookup.
- Zoom and preview quality improvements:
  - Map16 editor zoom controls (with grid/preview scaling improvements).
  - Object editor zoom for editable canvas and Map16 tile picker.
  - Tile behavior preview zoom controls in the Tileset Editor.
  - Pixel-art rendering fixes (nearest-neighbor scaling where applicable).
- Graphics tab quality-of-life:
  - `Remove Selected Bitmap` button.
  - Right-clicking a bitmap focuses that file in the ROM File Browser.
- New direct file import workflows:
  - Direct **Tileset** file importing from the Graphics editor.
  - Direct **Background** file importing from the Tilemap editor.
- Ongoing language updates (including French strings for newer UI options).

## Building
### Linux (Mono)
1. Install Mono with `msbuild` support.
2. Build from the repository root:
   ```bash
   msbuild NSMBe5.sln /t:Build /p:Configuration=Release
   ```
3. Run:
   ```bash
   mono NSMBe5/bin/Release/NSMBe5.exe
   ```

### Windows
1. Install Visual Studio 2022 (or newer) with `.NET desktop development`.
2. Open `NSMBe5.sln`.
3. Select `Release | Any CPU`.
4. Build (`Build > Build Solution`).
5. Run `NSMBe5/bin/Release/NSMBe5.exe`.

### Build Notes
- First build may take longer due to NuGet restore.
- If Mono prints `Gtk not found ... using built-in colorscheme`, the editor still works and falls back to built-in theming.

## Easy Sharing Packages
Use the scripts in `scripts/` to generate distributable artifacts in `dist/`.

### 1) Windows ZIP Package
Build and package:

```bash
./scripts/build-windows-zip.sh
```

Output:
- `dist/NSMBe5-windows-anycpu.zip`

Recommended release note for users:
- Extract the ZIP and run `NSMBe5.exe` on Windows.

### 2) Linux AppImage Package
Build and package:

```bash
./scripts/build-linux-appimage.sh
```

Output:
- `dist/NSMBe5-linux-x86_64.AppImage`

Requirements for packaging:
- `msbuild` (Mono build tools)
- `appimagetool`

Runtime note:
- The generated AppImage currently uses system `mono` by default.
- If `mono` is not installed on the target machine, the AppImage will show an error and exit.

To bundle Mono in the AppImage (easier sharing):

```bash
./scripts/build-linux-appimage.sh --bundle-mono
```

Output:
- `dist/NSMBe5-linux-x86_64-bundled-mono.AppImage`

Bundled mode note:
- This file is larger, but users usually do not need system Mono installed.

### 3) Build Everything At Once
Generate both Linux AppImages and the Windows AnyCPU ZIP:

```bash
./scripts/build-all-packages.sh
```

Run:

```bash
chmod +x dist/NSMBe5-linux-x86_64.AppImage
./dist/NSMBe5-linux-x86_64.AppImage
```

## Screenshots
<p align="left">
  <img src="https://raw.githubusercontent.com/MammaMiaTeam/NSMB-Editor/master/screenshots/filebrowser.png" width="385" title="File Browser">
  <img src="https://raw.githubusercontent.com/MammaMiaTeam/NSMB-Editor/master/screenshots/leveleditor.png" width="400" title="Level Editor">
</p>

## Credits
- Treeki - Original developer
- Dirbaio - Second developer
- Piranhaplant - Developer
- MeroMero - Developer
- RicBent - Developer
- Mamma Mia Team - Developers and current maintainers
- Szymbar - Developer, adapted structure to work with MKDS assembly
- All other contributors

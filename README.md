# NSMB Editor
##### Forked from [MeroMero](https://github.com/Mero-Mero/NSMB-Editor) originally by [Dirbaio](https://github.com/Dirbaio/NSMB-Editor)

## Download
To download the latest NSMBe build check the [GitHub Release page](https://github.com/MammaMiaTeam/NSMB-Editor/releases/latest/).

For legacy builds, check the [NSMBHD Downloads page](https://nsmbhd.net/download/all/).

## Community
Join the [NSMB Central Discord](https://discord.gg/x7gr3M9)!

If you want to, you can also join the forums at [NSMBHD](http://nsmbhd.net/).

## Requirements
- Windows: [.NET Framework 4.7.2](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net472)
- Linux, MacOSX, Others: [Mono](https://www.mono-project.com/download/stable/)

## What's New (This Fork)
- ROM File Browser search field for faster file lookup.
- ROM File Browser search filter mode to show only matching entries.
- Map16 editor zoom controls (with grid/preview scaling improvements).
- Object editor zoom options:
  - Editable object canvas zoom.
  - Map16 tile picker zoom.
- Tile behavior preview zoom controls in the Tileset Editor.
- Graphics tab QoL:
  - `Remove Selected Bitmap` button.
  - Right-clicking a bitmap now focuses that file in the ROM File Browser.
- Pixel-art rendering fixes in editor previews (nearest-neighbor scaling where applicable).

## Building
### Linux (Mono)
1. Install Mono with `msbuild` support.
2. From the repository root, build:
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
4. Build solution (`Build > Build Solution`).
5. Run `NSMBe5/bin/Release/NSMBe5.exe`.

### Notes
- If Mono prints `Gtk not found ... using built-in colorscheme`, the editor will still run; it just falls back to built-in theming.
- First build may take longer due to NuGet package restore.

## Credits
- Treeki - Original Developer
- Dirbaio - Second Developer
- Piranhaplant - Developer
- MeroMero - Developer
- RicBent - Developer
- Mamma Mia Team - Developers and current maintainers
- Szymbar - Developer, adopted the structure to work with MKDS assembly
- And all other contributors!

## Previews
<p align="left">
<img src="https://raw.githubusercontent.com/MammaMiaTeam/NSMB-Editor/master/screenshots/filebrowser.png" width="385" title="File Browser">
<img src="https://raw.githubusercontent.com/MammaMiaTeam/NSMB-Editor/master/screenshots/leveleditor.png" width="400" title="Level Editor">
</p>

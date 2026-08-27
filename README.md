# Smithton Livestream Guide

A small Windows desktop app that shows the Smithton Free Church livestream
setup guide (power on, cameras, lights, Restream, RTMP Web Player, the T-15
go-live sequence, etc.) as a clean, always-available reference window — so
whoever is running the desk doesn't need to dig up a printed sheet or a
shared doc.

It's a static reference viewer: nothing to click through or check off, just
the guide laid out clearly, with an "Always on top" option so it can sit
over EasyWorship / the browser / the ATEM software while you work.

## Project layout

- `src/SmithtonLivestreamGuide/` — the WPF (.NET 8) app.
- `installer/setup.iss` — Inno Setup script that packages the published app
  into a proper Windows installer (Start Menu shortcut, optional desktop
  icon, uninstaller).
- `.github/workflows/build-and-release.yml` — manually-triggered GitHub
  Actions workflow that builds the app, builds the installer, and publishes
  it to a GitHub Release.

## Getting a release

1. Go to the repo's **Actions** tab.
2. Select **Build and Release Windows Installer** in the left sidebar.
3. Click **Run workflow** (no inputs needed).
4. When it finishes, check the **Releases** page — the installer
   (`SmithtonLivestreamGuide-Setup-<version>.exe`) is attached there.
   Versions are date-based (e.g. `2026.08.27-3`), so every run produces a
   new, uniquely tagged release automatically.

Hand that installer `.exe` to whoever needs it on the church laptop; running
it installs the app properly (Start Menu entry, optional desktop icon,
clean uninstall via "Add or Remove Programs") rather than just leaving a
loose `.exe` to double-click.

> **Note on SmartScreen:** the installer isn't code-signed (that requires a
> paid certificate), so Windows SmartScreen may show an "unrecognized app"
> warning on first run. Clicking **More info → Run anyway** proceeds. If
> this is a problem, a code-signing certificate can be added to the
> workflow later.

## Building locally (optional)

Requires the .NET 8 SDK and Inno Setup 6 installed on Windows.

```powershell
dotnet publish src/SmithtonLivestreamGuide/SmithtonLivestreamGuide.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" `
  /DMyAppVersion="1.0.0" `
  /DPublishDir="$PWD\src\SmithtonLivestreamGuide\bin\Release\net8.0-windows\win-x64\publish" `
  installer\setup.iss
```

The installer is written to `dist\`.

## Updating the guide content

The guide text lives directly in
`src/SmithtonLivestreamGuide/MainWindow.xaml` as a series of `TextBlock`
entries grouped under section headers. Edit it there, commit, and run the
Actions workflow again to publish a new installer.

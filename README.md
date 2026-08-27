# Smithton Livestream Guide

A small Windows desktop app that shows the Smithton Free Church livestream
setup guide (power on, cameras, lights, Restream, RTMP Web Player, the T-15
go-live sequence, etc.) as a clean, always-available reference window — so
whoever is running the desk doesn't need to dig up a printed sheet or a
shared doc.

It's a static reference viewer: nothing to click through or check off, just
the guide laid out clearly, with an "Always on top" option so it can sit
over EasyWorship / the browser / the ATEM software while you work, and a
"Start with Windows" option so it's already open when the desk laptop is
switched on. Light theme, set in Poppins (embedded in the app, no need to
have it installed on the church laptop).

It also auto-updates itself: on launch it checks this repo's GitHub
Releases, and if a newer version has been published it shows a mandatory
"Update available" popup (no way to dismiss it — it must be applied before
the guide is usable again). Clicking **Update** downloads the new
installer, shows a small progress bar while it does, then runs the
installer fully silently, which closes the app, replaces it, and reopens
it automatically. If GitHub can't be reached (offline, no internet yet)
the check just fails silently and the guide opens normally — it never
blocks you from using the current version because of a failed check.

## Project layout

- `src/SmithtonLivestreamGuide/` — the WPF (.NET 8) app.
- `src/SmithtonLivestreamGuide/UpdateChecker.cs` — polls the GitHub
  Releases API and compares the published tag against the running app's
  version.
- `src/SmithtonLivestreamGuide/UpdateInstaller.cs` — downloads the new
  installer and launches it silently.
- `src/SmithtonLivestreamGuide/UpdateWindow.xaml` — the mandatory update
  popup (prompt → progress → error/retry states).
- `src/SmithtonLivestreamGuide/Fonts/` — the Poppins `.ttf` files, embedded
  into the app as WPF resources (SIL Open Font License, see `OFL.txt`).
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

> **Note on repo visibility:** the auto-update check calls GitHub's public
> Releases API (`api.github.com/repos/bobchomp/livestreamguide/releases/latest`)
> with no authentication, so this repository needs to stay **public** for
> update checks to work on the church laptop.

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

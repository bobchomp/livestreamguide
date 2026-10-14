# Update System

How Smithton Livestream Guide keeps itself up to date, written so another
developer (or AI) can maintain it, or rebuild it in another project, to the
same standard.

## Overview

The app updates itself from GitHub Releases with no server of its own. A
manually triggered GitHub Actions workflow
(`.github/workflows/build-and-release.yml`) builds the app, packages it with
Inno Setup, and publishes the installer `.exe` as a release asset. On launch,
the installed app asks GitHub's public API for the latest release, compares
that release's tag with its own embedded version, and if the release is newer
it downloads and silently runs the new installer. The installer replaces the
app and relaunches it.

Five parts have to agree for this to work:

1. The version scheme
2. The version baked into the binary
3. The comparison logic
4. The download-and-handoff step
5. The installer's close/relaunch settings

Most bugs in systems like this come from those parts drifting apart.

## Versioning

The workflow creates versions in the form `yyyy.MM.dd-N`, where `N` is
`GITHUB_RUN_NUMBER`. That number increases on every run, so two releases on
the same day still get distinct versions. The same string becomes the release
tag (`v` + version) and is passed to `dotnet publish` as `-p:Version=...`.

The app reads its own version from `AssemblyInformationalVersionAttribute` at
runtime. By default the .NET SDK appends `+<git-sha>` to that attribute, which
would break parsing, so the csproj sets
`IncludeSourceRevisionInInformationalVersion=false`. With that set, the string
in the binary matches the release tag exactly. **Do not remove this setting.**

Versions are compared as parsed values, never as strings: each one is split
into a `(DateOnly, int)` pair and compared by date first, then build number.
String comparison would sort `-10` before `-9` and miss a release.

If the app's own version doesn't parse (a local dev build has the csproj
default `1.0.0.0`), there is nothing to compare, so no check runs.

## Checking for updates

File: `src/SmithtonLivestreamGuide/UpdateChecker.cs`

`UpdateChecker` makes one unauthenticated `GET` to
`https://api.github.com/repos/bobchomp/livestreamguide/releases/latest`. It
sends a `User-Agent` header, because GitHub rejects requests without one, and
uses a 10-second timeout. **The repo must stay public**, because no token is
shipped inside the app.

A single core method (`CheckCoreAsync`) returns
`UpdateCheckResult(Succeeded, UpdateInfo?)`, and two entry points use it
differently:

- **`CheckForUpdateAsync`**: the silent startup check, called from
  `MainWindow_Loaded`. It treats every failure like "up to date": being
  offline, rate-limited, or getting bad JSON never shows the user anything or
  blocks the guide. **Fail open** is the most important rule here. The guide
  is used during live services, and a broken update check must never stop
  someone using the current version.
- **`CheckForUpdateInteractiveAsync`**: Help → Check for Updates. This path
  does distinguish the cases, because the user is waiting for an answer. It
  says either "you're up to date" or "couldn't check."

An update is only reported when the release tag is strictly newer and the
release has an `.exe` asset with a `browser_download_url`.

## The mandatory prompt

Files: `src/SmithtonLivestreamGuide/UpdateWindow.xaml` / `.xaml.cs`

When an update is found, `UpdateWindow` opens as a modal dialog
(`ShowDialog()`, owned by the main window), so the guide underneath can't be
used. The user can't dismiss it:

- `WindowStyle="None"` removes the close button.
- The `Closing` event is cancelled unless a private `_allowClose` flag is set.

Only the successful handoff path sets that flag, immediately before calling
`Application.Current.Shutdown()`. If the flag isn't set first, the shutdown
cancels itself.

The window moves between three states by toggling visibility: a prompt with an
Update button, a progress view, and an error view with a Retry button. There
is deliberately no skip option.

## Download and handoff

File: `src/SmithtonLivestreamGuide/UpdateInstaller.cs`

`UpdateInstaller` downloads the asset with `HttpClient` using
`HttpCompletionOption.ResponseHeadersRead`. It streams the file to a uniquely
named temp file in a buffered loop and reports progress through
`IProgress<double>` as bytes read ÷ `Content-Length`, so the progress bar
moves smoothly.

It then starts the installer with `UseShellExecute = true` and:

```
/VERYSILENT /SUPPRESSMSGBOXES /NORESTART
```

so the user sees no Inno wizard, only the app's own progress window. It does
not wait for the installer to finish.

## The self-replacement problem

The running process is the exact `.exe` the installer has to overwrite. Three
mechanisms handle this:

1. **Primary:** the app shuts itself down immediately after starting the
   installer (`UpdateWindow.xaml.cs`), which releases the file lock long
   before Inno reaches its file-copy step.
2. **Safety net:** `installer/setup.iss` sets `CloseApplications=force`, so
   Windows Restart Manager kills any process still holding the file, without
   prompting.
3. **Relaunch:** handled by exactly one mechanism. The `[Run]` entry that
   launches the app does not have the `skipifsilent` flag, so it runs after
   silent installs too. `RestartApplications=no` stops Restart Manager from
   also relaunching the app it closed, which would open two windows.

These settings are split between `UpdateWindow.xaml.cs` and `setup.iss` but
work as one mechanism. **Change the silent-install arguments, the `[Run]`
flags, or the two `*Applications` settings together, never one at a time.**

## File map

| File | Responsibility |
|---|---|
| `.github/workflows/build-and-release.yml` | Mints the `yyyy.MM.dd-N` version, publishes, builds the installer, creates the release |
| `src/SmithtonLivestreamGuide/SmithtonLivestreamGuide.csproj` | Pins `InformationalVersion` to exactly `$(Version)` |
| `src/SmithtonLivestreamGuide/UpdateChecker.cs` | GitHub API call, version parsing/comparison, silent vs interactive results |
| `src/SmithtonLivestreamGuide/UpdateInstaller.cs` | Streamed download with progress, silent installer launch |
| `src/SmithtonLivestreamGuide/UpdateWindow.xaml[.cs]` | Mandatory modal UI, state switching, the one legitimate close |
| `src/SmithtonLivestreamGuide/MainWindow.xaml.cs` | Startup check (`MainWindow_Loaded`) and Help → Check for Updates |
| `installer/setup.iss` | `CloseApplications=force`, `RestartApplications=no`, `[Run]` without `skipifsilent` |

## Principles

- Fail open on checks, but never let the user skip a confirmed update.
- Compare versions semantically, not as strings.
- Keep the version in the binary byte-identical to the release tag.
- Use exactly one relaunch path.
- Have the app release its own file lock rather than relying on being killed.
- Show explicit progress and a retry path instead of hanging silently.
- Keep the `DispatcherUnhandledException` handler in `App.xaml.cs` so
  failures show an error dialog with the stack trace instead of the process
  vanishing.
- Every release must come from the workflow. A hand-built binary with a
  non-conforming version silently opts out of updates.

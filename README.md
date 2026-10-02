# FNF Asset Prep

FNF Asset Prep is a native Windows desktop toolbox for Friday Night Funkin' assets and audio.

This branch contains the development rewrite. The public v0.1.0 release is unchanged.

## Native app

The development build uses C#, .NET 8, WPF, native Windows dialogs, ImageSharp, FFmpeg, and yt-dlp.

There is no HTML, CSS, Electron, or browser-based UI in this branch.

## Pages

### Asset Prep

Prepare selected files for FNF. Images become PNG and audio becomes OGG. Source files are never edited in place.

### YouTube Audio

This page is for videos you own or have permission to download.

Single-video output formats:

- OGG
- MP3
- WAV
- FLAC
- M4A
- Opus
- AAC
- ALAC

The app checks a link first and shows its title, channel, and duration. Playlists are skipped in this test build.

### Settings

Settings are stored locally in:

```text
%LOCALAPPDATA%\FNF Asset Prep\settings.json
```

Saved options:

- Default Asset Prep output folder
- Default YouTube Audio output folder
- Default OGG quality
- Default download format
- Open output folder after a successful task
- Confirm before clearing the Asset Prep list

## Test build

Pushes to `native-rewrite` create a GitHub Actions artifact only. No GitHub Release is created.

The artifact contains:

```text
FNF-Asset-Prep-Native-Test.exe
tools/
  ffmpeg.exe
  yt-dlp.exe
```

Keep the tools folder next to the EXE.

## Build locally

Install the .NET 8 SDK.

```powershell
dotnet restore FNFAssetPrep.sln
dotnet run --project src/FNFAssetPrep/FNFAssetPrep.csproj
```

## Development status

This version is for testing and is not the public release.

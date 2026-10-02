# FNF Asset Prep

FNF Asset Prep prepares source images and audio for Friday Night Funkin' mods.

This branch contains the native Windows rewrite.

## Native rewrite

The old public v0.1.0 app uses Electron. The new development build does not.

The native rewrite uses:

- C#
- .NET 8
- WPF
- Windows file dialogs
- A normal Windows menu, toolbar, file table, and status bar
- ImageSharp for image decoding and PNG output
- FFmpeg for OGG Vorbis audio output

There is no HTML, CSS, browser view, or JavaScript UI in this version.

## What it does

Images:

- PNG stays PNG
- JPG to PNG
- JPEG to PNG
- WebP to PNG
- BMP to PNG
- TIFF to PNG

Audio:

- OGG stays OGG
- WAV to OGG
- MP3 to OGG
- FLAC to OGG
- M4A to OGG
- AAC to OGG

Source files are never edited in place.

If an output name already exists, the app creates a new name such as:

\`\`\`text
Inst.ogg
Inst_2.ogg
Inst_3.ogg
\`\`\`

## Desktop controls

- Ctrl+O: Add files
- Ctrl+Shift+O: Choose output folder
- Delete: Remove selected files
- Ctrl+L: Clear the list
- Drag and drop: Add supported files

## Test build

Pushes to the \`native-rewrite\` branch create a Windows test artifact through GitHub Actions.

The test build is not published to GitHub Releases.

The artifact contains:

\`\`\`text
FNF-Asset-Prep-Native-Test.exe
tools/
  ffmpeg.exe
\`\`\`

Keep the \`tools\` folder next to the EXE so audio conversion works.

## Build locally

You need the .NET 8 SDK.

\`\`\`powershell
dotnet restore FNFAssetPrep.sln
dotnet run --project src/FNFAssetPrep/FNFAssetPrep.csproj
\`\`\`

For audio conversion, place \`ffmpeg.exe\` here:

\`\`\`text
src/FNFAssetPrep/bin/Debug/net8.0-windows/tools/ffmpeg.exe
\`\`\`

## Status

This is a development rewrite for testing. The public v0.1.0 release stays unchanged until the native version is tested and approved.

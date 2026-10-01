# Getting started with FNF Asset Prep

This guide explains the normal workflow.

## Install the Windows build

1. Open the GitHub Releases page.
2. Download the installer or portable `.exe`.
3. Open FNF Asset Prep.
4. If Windows SmartScreen appears, check that the file came from the official repository before you continue.

## Run from source

Install Node.js 20 or newer, then run:

```bash
git clone https://github.com/xm5o/FNF-Asset-Prep.git
cd FNF-Asset-Prep
npm install
npm start
```

## Prepare an image

Example source file:

```text
character.webp
```

Steps:

1. Add `character.webp`.
2. Choose an output folder.
3. Press `Prep files`.
4. The output becomes `character.png`.

The source WebP file stays unchanged.

## Prepare audio

Example source file:

```text
Inst.wav
```

Steps:

1. Add `Inst.wav`.
2. Choose an output folder.
3. Leave OGG quality at 6, or change it if needed.
4. Press `Prep files`.
5. The output becomes `Inst.ogg`.

The app uses OGG Vorbis for converted audio.

## OGG quality

The quality slider uses the normal Vorbis quality scale from 0 to 10.

A higher number normally means better quality and a larger file.

For most FNF music and voices, 6 is a useful starting point.

## Existing PNG and OGG files

PNG and OGG are already the target formats. The app copies them to the output folder instead of changing their format.

## Duplicate names

The app does not overwrite an existing output file.

If `voices.ogg` already exists, the next output becomes:

```text
voices_2.ogg
```

## Drag and drop

You can drag files from File Explorer into the drop area.

Folders are not scanned. Add the files you want to prepare.

## Troubleshooting

### An image fails

Try opening the source image in another image viewer first. A damaged or unusual image file might not decode correctly.

### Audio conversion fails

Check that the source audio file plays normally. FNF Asset Prep uses a bundled FFmpeg build for audio conversion.

### The output sounds different

Try a higher OGG quality value and convert the original source file again.

### Windows shows SmartScreen

The first builds are unsigned. Check that you downloaded the file from the official repository release page.

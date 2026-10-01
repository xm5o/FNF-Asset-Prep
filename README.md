# FNF Asset Prep

FNF Asset Prep is a small desktop utility for preparing image and audio files before you place them in a Friday Night Funkin' mod.

The goal is simple:

- Images become `.png`
- Audio becomes `.ogg`

It does not scan or rewrite your full mod. You choose the source files you want to prepare.

## Why this exists

FNF engines normally expect common game assets in formats such as PNG for images and OGG for audio. Source art and audio often start as JPG, WebP, WAV, MP3, FLAC, or another format.

FNF Asset Prep gives you one small place to convert those files without opening a full editor.

## Main features

- Add several files at once
- Drag files into the window
- Convert JPG, JPEG, WebP, BMP, and TIFF images to PNG
- Convert WAV, MP3, FLAC, M4A, and AAC audio to OGG Vorbis
- Copy files that are already PNG or OGG
- Keep transparent image data when the source supports it
- Choose OGG quality from 0 to 10
- Keep the original source files unchanged
- Avoid overwriting files with the same name
- Open the output folder from the app

## Download

The easiest way to use the app is the Windows build from the GitHub Releases page.

Each tagged release builds:

- A normal Windows installer
- A portable Windows `.exe`

The release workflow is included in `.github/workflows/release.yml`.

For release steps, read [docs/PUBLISHING.md](docs/PUBLISHING.md).

## Run from source

You need Node.js 20 or newer.

```bash
git clone https://github.com/xm5o/FNF-Asset-Prep.git
cd FNF-Asset-Prep
npm install
npm start
```

## Build the Windows app

```bash
npm install
npm run dist
```

The Windows files will be placed in `dist/`.

## How to use it

1. Open FNF Asset Prep.
2. Press `Add files` or drop source files into the window.
3. Choose an output folder.
4. Set the OGG quality if you want to change it. Quality 6 is a good general default.
5. Press `Prep files`.
6. Open the output folder and move the prepared assets into your mod.

Read [docs/GETTING_STARTED.md](docs/GETTING_STARTED.md) for more detail.

## Supported input formats

### Images

```text
.png
.jpg
.jpeg
.webp
.bmp
.tif
.tiff
```

Output: `.png`

### Audio

```text
.ogg
.wav
.mp3
.flac
.m4a
.aac
```

Output: `.ogg`

## What happens to PNG and OGG files

If a file is already PNG or OGG, the app copies it into the output folder. It does not convert it to another format.

## File safety

The app never edits the source file in place.

If the output folder already contains a file with the same name, a number is added to the new file name instead of overwriting the old one.

Example:

```text
Inst.ogg
Inst_2.ogg
Inst_3.ogg
```

## Windows warning

The first public builds are unsigned. Windows SmartScreen might show a warning because the executable does not have a paid code signing certificate.

Always download the app from the official repository release page.

## License

MIT. See [LICENSE](LICENSE).

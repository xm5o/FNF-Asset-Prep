const { app, BrowserWindow, dialog, ipcMain, shell } = require('electron');
const { spawn } = require('node:child_process');
const fs = require('node:fs');
const path = require('node:path');
const sharp = require('sharp');
const ffmpegStatic = require('ffmpeg-static');

let mainWindow;

const IMAGE_INPUTS = new Set(['.png', '.jpg', '.jpeg', '.webp', '.bmp', '.tif', '.tiff']);
const AUDIO_INPUTS = new Set(['.ogg', '.wav', '.mp3', '.flac', '.m4a', '.aac']);

function send(channel, payload) {
  if (mainWindow && !mainWindow.isDestroyed()) {
    mainWindow.webContents.send(channel, payload);
  }
}

function resolveFfmpeg() {
  if (!ffmpegStatic) throw new Error('FFmpeg was not found.');
  if (!app.isPackaged) return ffmpegStatic;
  return ffmpegStatic.replace('app.asar', 'app.asar.unpacked');
}

function kindOf(file) {
  const ext = path.extname(file).toLowerCase();
  if (IMAGE_INPUTS.has(ext)) return 'image';
  if (AUDIO_INPUTS.has(ext)) return 'audio';
  return 'unsupported';
}

function fileInfo(file) {
  const stat = fs.statSync(file);
  return {
    path: file,
    name: path.basename(file),
    ext: path.extname(file).toLowerCase(),
    size: stat.size,
    kind: kindOf(file)
  };
}

function safeOutputPath(outputDir, inputFile, targetExt) {
  const original = path.parse(inputFile);
  const cleanBase = original.name.replace(/[<>:"/\\|?*\x00-\x1F]/g, '_').trim() || 'asset';
  let candidate = path.join(outputDir, `${cleanBase}${targetExt}`);
  let number = 2;

  while (fs.existsSync(candidate)) {
    candidate = path.join(outputDir, `${cleanBase}_${number}${targetExt}`);
    number += 1;
  }
  return candidate;
}

async function prepImage(input, outputDir) {
  const output = safeOutputPath(outputDir, input, '.png');
  const ext = path.extname(input).toLowerCase();

  if (ext === '.png') {
    await fs.promises.copyFile(input, output);
    return { output, action: 'copied' };
  }

  await sharp(input, { failOn: 'none' })
    .png({ compressionLevel: 9, adaptiveFiltering: true })
    .toFile(output);

  return { output, action: 'converted' };
}

function ffmpegToOgg(input, output, quality) {
  return new Promise((resolve, reject) => {
    const ffmpeg = resolveFfmpeg();
    const child = spawn(ffmpeg, [
      '-hide_banner',
      '-loglevel', 'error',
      '-y',
      '-i', input,
      '-vn',
      '-c:a', 'libvorbis',
      '-q:a', String(quality),
      output
    ], { windowsHide: true });

    let errorText = '';
    child.stderr.on('data', chunk => { errorText += chunk.toString(); });
    child.on('error', reject);
    child.on('exit', code => {
      if (code === 0) resolve();
      else reject(new Error(errorText.trim() || `FFmpeg exited with code ${code}.`));
    });
  });
}

async function prepAudio(input, outputDir, quality) {
  const output = safeOutputPath(outputDir, input, '.ogg');
  const ext = path.extname(input).toLowerCase();

  if (ext === '.ogg') {
    await fs.promises.copyFile(input, output);
    return { output, action: 'copied' };
  }

  await ffmpegToOgg(input, output, quality);
  return { output, action: 'converted' };
}

async function processFiles(files, outputDir, settings = {}) {
  if (!outputDir) throw new Error('Choose an output folder first.');
  await fs.promises.mkdir(outputDir, { recursive: true });

  const quality = Math.min(10, Math.max(0, Number(settings.oggQuality ?? 6)));
  const results = [];

  for (let index = 0; index < files.length; index += 1) {
    const input = files[index];
    const info = fileInfo(input);
    send('prep:progress', {
      index,
      total: files.length,
      path: input,
      status: 'working'
    });

    try {
      if (info.kind === 'unsupported') throw new Error('Unsupported file type.');
      const result = info.kind === 'image'
        ? await prepImage(input, outputDir)
        : await prepAudio(input, outputDir, quality);

      const item = {
        path: input,
        status: 'done',
        output: result.output,
        action: result.action
      };
      results.push(item);
      send('prep:progress', { ...item, index, total: files.length });
    } catch (error) {
      const item = {
        path: input,
        status: 'error',
        error: error.message || String(error)
      };
      results.push(item);
      send('prep:progress', { ...item, index, total: files.length });
    }
  }

  return results;
}

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1120,
    height: 760,
    minWidth: 880,
    minHeight: 600,
    backgroundColor: '#0b0c0e',
    show: false,
    title: 'FNF Asset Prep',
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: true,
      nodeIntegration: false
    }
  });

  mainWindow.loadFile(path.join(__dirname, 'index.html'));
  mainWindow.once('ready-to-show', () => mainWindow.show());
}

app.whenReady().then(() => {
  createWindow();

  ipcMain.handle('files:pick', async () => {
    const result = await dialog.showOpenDialog(mainWindow, {
      properties: ['openFile', 'multiSelections'],
      filters: [
        { name: 'FNF asset inputs', extensions: ['png', 'jpg', 'jpeg', 'webp', 'bmp', 'tif', 'tiff', 'ogg', 'wav', 'mp3', 'flac', 'm4a', 'aac'] },
        { name: 'All files', extensions: ['*'] }
      ]
    });
    if (result.canceled) return [];
    return result.filePaths.map(fileInfo);
  });

  ipcMain.handle('files:inspect', (_event, files) => {
    return files
      .filter(file => typeof file === 'string' && fs.existsSync(file) && fs.statSync(file).isFile())
      .map(fileInfo);
  });

  ipcMain.handle('output:pick', async () => {
    const result = await dialog.showOpenDialog(mainWindow, {
      properties: ['openDirectory', 'createDirectory']
    });
    if (result.canceled || !result.filePaths[0]) return null;
    return result.filePaths[0];
  });

  ipcMain.handle('prep:run', (_event, payload) => {
    return processFiles(payload.files || [], payload.outputDir, payload.settings || {});
  });

  ipcMain.handle('output:open', (_event, folder) => shell.openPath(folder));

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow();
  });
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') app.quit();
});

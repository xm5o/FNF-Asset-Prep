const { contextBridge, ipcRenderer, webUtils } = require('electron');

contextBridge.exposeInMainWorld('assetPrep', {
  pickFiles: () => ipcRenderer.invoke('files:pick'),
  inspectFiles: files => ipcRenderer.invoke('files:inspect', files),
  pickOutput: () => ipcRenderer.invoke('output:pick'),
  run: payload => ipcRenderer.invoke('prep:run', payload),
  openOutput: folder => ipcRenderer.invoke('output:open', folder),
  getPathForFile: file => webUtils.getPathForFile(file),
  onProgress: callback => ipcRenderer.on('prep:progress', (_event, data) => callback(data))
});

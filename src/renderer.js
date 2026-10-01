const state = {
  files: [],
  outputDir: null,
  running: false
};

const $ = selector => document.querySelector(selector);
const queue = $('#queue');
const queueEmpty = $('#queueEmpty');
const queueSummary = $('#queueSummary');
const prepButton = $('#prepButton');
const statusText = $('#statusText');
const outputPath = $('#outputPath');
const openOutput = $('#openOutput');
const toast = $('#toast');

function formatSize(bytes) {
  const value = Number(bytes || 0);
  if (value < 1024) return `${value} B`;
  if (value < 1024 * 1024) return `${(value / 1024).toFixed(1)} KB`;
  return `${(value / 1024 / 1024).toFixed(1)} MB`;
}

function showToast(message) {
  toast.textContent = message;
  toast.hidden = false;
  clearTimeout(showToast.timer);
  showToast.timer = setTimeout(() => { toast.hidden = true; }, 3000);
}

function targetLabel(item) {
  if (item.kind === 'image') return item.ext === '.png' ? 'PNG copy' : 'Convert to PNG';
  if (item.kind === 'audio') return item.ext === '.ogg' ? 'OGG copy' : 'Convert to OGG';
  return 'Unsupported';
}

function addItems(items) {
  const known = new Set(state.files.map(item => item.path.toLowerCase()));
  for (const item of items) {
    if (!known.has(item.path.toLowerCase())) {
      state.files.push({ ...item, status: item.kind === 'unsupported' ? 'error' : 'ready', error: item.kind === 'unsupported' ? 'Unsupported file type.' : null });
      known.add(item.path.toLowerCase());
    }
  }
  render();
}

function render() {
  queue.innerHTML = '';
  queueEmpty.hidden = state.files.length > 0;
  queueSummary.textContent = `${state.files.length} file${state.files.length === 1 ? '' : 's'}`;
  prepButton.disabled = state.running || state.files.length === 0 || !state.outputDir || state.files.every(file => file.kind === 'unsupported');
  $('#clearButton').disabled = state.running || state.files.length === 0;

  state.files.forEach((item, index) => {
    const row = document.createElement('div');
    row.className = 'queue-item';
    row.innerHTML = `
      <div class="file-kind"></div>
      <div class="file-copy"><strong></strong><small></small></div>
      <div class="file-status"></div>
      <button class="remove-file" type="button" aria-label="Remove file">x</button>
    `;
    row.querySelector('.file-kind').textContent = item.kind === 'image' ? 'IMG' : item.kind === 'audio' ? 'AUD' : '?';
    row.querySelector('.file-copy strong').textContent = item.name;
    row.querySelector('.file-copy small').textContent = `${formatSize(item.size)} | ${targetLabel(item)}`;
    const status = row.querySelector('.file-status');
    status.className = `file-status ${item.status || 'ready'}`;
    status.textContent = item.status === 'done'
      ? item.action === 'copied' ? 'Copied' : 'Done'
      : item.status === 'working' ? 'Working'
      : item.status === 'error' ? 'Error'
      : 'Ready';
    status.title = item.error || item.output || '';
    row.querySelector('.remove-file').disabled = state.running;
    row.querySelector('.remove-file').addEventListener('click', () => {
      state.files.splice(index, 1);
      render();
    });
    queue.appendChild(row);
  });
}

async function chooseFiles() {
  const items = await window.assetPrep.pickFiles();
  addItems(items);
}

async function chooseOutput() {
  const folder = await window.assetPrep.pickOutput();
  if (!folder) return;
  state.outputDir = folder;
  outputPath.textContent = folder;
  outputPath.title = folder;
  openOutput.disabled = false;
  render();
}

$('#addButton').addEventListener('click', chooseFiles);
$('#outputButton').addEventListener('click', chooseOutput);
$('#clearButton').addEventListener('click', () => {
  state.files = [];
  render();
});

$('#qualityInput').addEventListener('input', event => {
  $('#qualityValue').textContent = event.target.value;
});

openOutput.addEventListener('click', () => {
  if (state.outputDir) window.assetPrep.openOutput(state.outputDir);
});

prepButton.addEventListener('click', async () => {
  const valid = state.files.filter(file => file.kind !== 'unsupported');
  if (!valid.length || !state.outputDir) return;

  state.running = true;
  valid.forEach(file => {
    file.status = 'ready';
    file.error = null;
    file.output = null;
    file.action = null;
  });
  statusText.textContent = `Preparing ${valid.length} files...`;
  render();

  try {
    const results = await window.assetPrep.run({
      files: valid.map(file => file.path),
      outputDir: state.outputDir,
      settings: { oggQuality: Number($('#qualityInput').value) }
    });

    const failed = results.filter(result => result.status === 'error').length;
    statusText.textContent = failed ? `Finished with ${failed} error${failed === 1 ? '' : 's'}.` : `Finished ${results.length} files.`;
    showToast(failed ? 'Some files could not be prepared. Check the queue.' : 'Assets are ready.');
  } catch (error) {
    statusText.textContent = 'Prep failed.';
    showToast(error.message || 'Could not prepare the files.');
  } finally {
    state.running = false;
    render();
  }
});

window.assetPrep.onProgress(progress => {
  const item = state.files.find(file => file.path === progress.path);
  if (!item) return;
  item.status = progress.status;
  item.output = progress.output || null;
  item.action = progress.action || null;
  item.error = progress.error || null;
  statusText.textContent = progress.status === 'working'
    ? `Working on ${item.name}`
    : statusText.textContent;
  render();
});

const dropZone = $('#dropZone');
['dragenter', 'dragover'].forEach(type => dropZone.addEventListener(type, event => {
  event.preventDefault();
  dropZone.classList.add('dragging');
}));
['dragleave', 'drop'].forEach(type => dropZone.addEventListener(type, event => {
  event.preventDefault();
  dropZone.classList.remove('dragging');
}));

dropZone.addEventListener('drop', async event => {
  const paths = [...event.dataTransfer.files]
    .map(file => window.assetPrep.getPathForFile(file))
    .filter(Boolean);
  if (!paths.length) return;
  const items = await window.assetPrep.inspectFiles(paths);
  addItems(items);
});

render();

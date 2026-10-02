using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FNFAssetPrep.Models;
using FNFAssetPrep.Services;
using Microsoft.Win32;

namespace FNFAssetPrep.Views;

public partial class AssetPrepView : UserControl
{
    private readonly AssetProcessor _processor = new();
    private readonly SettingsService _settingsService;
    private bool _isRunning;
    private bool _outputChangedByUser;

    public ObservableCollection<AssetItem> Assets { get; } = new();

    public AssetPrepView(SettingsService settingsService)
    {
        InitializeComponent();
        DataContext = this;
        _settingsService = settingsService;
        ApplySettings(forceFolder: true);
        UpdateUi();
    }

    public void ApplySettings(bool forceFolder = false)
    {
        if (forceFolder || !_outputChangedByUser)
        {
            OutputFolderText.Text = _settingsService.Settings.AssetOutputFolder;
        }

        SetQuality(_settingsService.Settings.OggQuality);
        UpdateUi();
    }

    public void AddFiles()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Add assets",
            Multiselect = true,
            CheckFileExists = true,
            Filter = "Supported assets|*.png;*.jpg;*.jpeg;*.webp;*.bmp;*.tif;*.tiff;*.ogg;*.wav;*.mp3;*.flac;*.m4a;*.aac|Images|*.png;*.jpg;*.jpeg;*.webp;*.bmp;*.tif;*.tiff|Audio|*.ogg;*.wav;*.mp3;*.flac;*.m4a;*.aac|All files|*.*"
        };

        if (dialog.ShowDialog(Window.GetWindow(this)) == true)
        {
            AddPaths(dialog.FileNames);
        }
    }

    private void AddFiles_Click(object sender, RoutedEventArgs e) => AddFiles();

    private void AddPaths(IEnumerable<string> paths)
    {
        var existing = new HashSet<string>(
            Assets.Select(item => item.SourcePath),
            StringComparer.OrdinalIgnoreCase);

        foreach (var path in paths)
        {
            if (!File.Exists(path) || !AssetProcessor.IsSupported(path) || !existing.Add(path))
            {
                continue;
            }

            Assets.Add(AssetProcessor.CreateItem(path));
        }

        UpdateUi();
    }

    private void RemoveSelected_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning) return;

        foreach (var item in AssetGrid.SelectedItems.Cast<AssetItem>().ToList())
        {
            Assets.Remove(item);
        }

        UpdateUi();
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning || Assets.Count == 0) return;

        if (_settingsService.Settings.ConfirmBeforeClear)
        {
            var answer = MessageBox.Show(
                Window.GetWindow(this),
                "Clear all files from the list?",
                "FNF Asset Prep",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (answer != MessageBoxResult.Yes) return;
        }

        Assets.Clear();
        ProgressText.Text = string.Empty;
        ProgressBar.Value = 0;
        UpdateUi();
    }

    private void ChooseOutput_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning) return;

        var dialog = new OpenFolderDialog
        {
            Title = "Choose where prepared assets will be saved",
            Multiselect = false,
            InitialDirectory = Directory.Exists(OutputFolderText.Text)
                ? OutputFolderText.Text
                : _settingsService.Settings.AssetOutputFolder
        };

        if (dialog.ShowDialog(Window.GetWindow(this)) == true)
        {
            OutputFolderText.Text = dialog.FolderName;
            _outputChangedByUser = true;
            UpdateUi();
        }
    }

    private void OpenOutput_Click(object sender, RoutedEventArgs e) =>
        ShellService.OpenFolder(OutputFolderText.Text.Trim());

    private async void PrepFiles_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning || Assets.Count == 0) return;

        var outputDirectory = OutputFolderText.Text.Trim();
        if (string.IsNullOrWhiteSpace(outputDirectory)) return;

        if (Assets.Any(item => item.Type == "Audio" && item.SourceFormat != "OGG")
            && !AssetProcessor.HasAudioEngine)
        {
            MessageBox.Show(
                Window.GetWindow(this),
                "FFmpeg is missing from the tools folder.",
                "Audio engine missing",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        _isRunning = true;
        UpdateUi();

        ProgressBar.Maximum = Assets.Count;
        ProgressBar.Value = 0;

        var quality = GetSelectedQuality();
        var successCount = 0;
        var failedCount = 0;

        for (var index = 0; index < Assets.Count; index++)
        {
            var item = Assets[index];
            item.Status = "Working";
            item.StatusDetail = null;
            ProgressText.Text = $"{index + 1} / {Assets.Count}";

            try
            {
                item.StatusDetail = await _processor.ProcessAsync(item, outputDirectory, quality);
                item.Status = "Done";
                successCount++;
            }
            catch (Exception exception)
            {
                item.Status = "Error";
                item.StatusDetail = exception.Message;
                failedCount++;
            }

            ProgressBar.Value = index + 1;
        }

        _isRunning = false;
        UpdateUi();
        ProgressText.Text = failedCount == 0
            ? $"{successCount} ready"
            : $"{successCount} ready, {failedCount} failed";

        if (failedCount == 0 && _settingsService.Settings.OpenFolderAfterTask)
        {
            ShellService.OpenFolder(outputDirectory);
        }

        if (_settingsService.Settings.ShowCompletionDialog)
        {
            MessageBox.Show(
                Window.GetWindow(this),
                ProgressText.Text,
                "FNF Asset Prep",
                MessageBoxButton.OK,
                failedCount == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
    }

    private void SetQuality(int quality)
    {
        foreach (var item in QualityCombo.Items.OfType<ComboBoxItem>())
        {
            if (item.Tag?.ToString() == quality.ToString())
            {
                QualityCombo.SelectedItem = item;
                return;
            }
        }

        QualityCombo.SelectedIndex = 3;
    }

    private int GetSelectedQuality() =>
        QualityCombo.SelectedItem is ComboBoxItem item
        && int.TryParse(item.Tag?.ToString(), out var quality)
            ? quality
            : 6;

    private void UpdateUi()
    {
        var hasFiles = Assets.Count > 0;
        EmptyState.Visibility = hasFiles ? Visibility.Collapsed : Visibility.Visible;
        FileTablePanel.Visibility = hasFiles ? Visibility.Visible : Visibility.Collapsed;

        AssetGrid.IsEnabled = !_isRunning;
        PrepButton.IsEnabled = !_isRunning
            && hasFiles
            && !string.IsNullOrWhiteSpace(OutputFolderText.Text);
    }

    private void View_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void View_Drop(object sender, DragEventArgs e)
    {
        if (_isRunning) return;
        if (e.Data.GetData(DataFormats.FileDrop) is string[] paths) AddPaths(paths);
    }

    private void View_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.O)
        {
            AddFiles();
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.L)
        {
            Clear_Click(sender, new RoutedEventArgs());
            e.Handled = true;
        }
        else if (e.Key == Key.Delete)
        {
            RemoveSelected_Click(sender, new RoutedEventArgs());
            e.Handled = true;
        }
    }
}

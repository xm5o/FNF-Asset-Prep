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

    private void AddFiles_Click(object sender, RoutedEventArgs e)
    {
        AddFiles();
    }

    private void AddPaths(IEnumerable<string> paths)
    {
        var existing = new HashSet<string>(
            Assets.Select(item => item.SourcePath),
            StringComparer.OrdinalIgnoreCase);

        foreach (var path in paths)
        {
            if (!File.Exists(path)
                || !AssetProcessor.IsSupported(path)
                || !existing.Add(path))
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

        var selected = AssetGrid.SelectedItems.Cast<AssetItem>().ToList();

        foreach (var item in selected)
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

            if (answer != MessageBoxResult.Yes)
            {
                return;
            }
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

    private void OpenOutput_Click(object sender, RoutedEventArgs e)
    {
        ShellService.OpenFolder(OutputFolderText.Text.Trim());
    }

    private async void PrepFiles_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning || Assets.Count == 0) return;

        var outputDirectory = OutputFolderText.Text.Trim();

        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            MessageBox.Show(
                Window.GetWindow(this),
                "Choose an output folder first.",
                "FNF Asset Prep",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var audioItems = Assets
            .Where(item => item.Type == "Audio" && item.SourceFormat != "OGG")
            .ToList();

        if (audioItems.Count > 0 && !AssetProcessor.HasAudioEngine)
        {
            MessageBox.Show(
                Window.GetWindow(this),
                "FFmpeg is missing from the tools folder. Image conversion still works, but audio conversion needs FFmpeg.",
                "Audio engine missing",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        _isRunning = true;
        SetControlsEnabled(false);

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
            ProgressText.Text = "Working on " + item.FileName;

            try
            {
                var output = await _processor.ProcessAsync(
                    item,
                    outputDirectory,
                    quality);

                item.Status = "Done";
                item.StatusDetail = output;
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
        SetControlsEnabled(true);
        UpdateUi();

        ProgressText.Text = failedCount == 0
            ? successCount + " file(s) prepared."
            : successCount + " completed, " + failedCount + " failed.";

        if (failedCount == 0 && _settingsService.Settings.OpenFolderAfterTask)
        {
            ShellService.OpenFolder(outputDirectory);
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

    private int GetSelectedQuality()
    {
        return QualityCombo.SelectedItem is ComboBoxItem item
            && int.TryParse(item.Tag?.ToString(), out var quality)
                ? quality
                : 6;
    }

    private void SetControlsEnabled(bool enabled)
    {
        AssetGrid.IsEnabled = enabled;
        PrepButton.IsEnabled = enabled && Assets.Count > 0;
    }

    private void UpdateUi()
    {
        var images = Assets.Count(item => item.Type == "Image");
        var audio = Assets.Count(item => item.Type == "Audio");

        ImageCountText.Text = images.ToString();
        AudioCountText.Text = audio.ToString();
        TotalCountText.Text = Assets.Count.ToString();
        EmptyHint.Visibility = Assets.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        PrepButton.IsEnabled = !_isRunning
            && Assets.Count > 0
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

        if (e.Data.GetData(DataFormats.FileDrop) is string[] paths)
        {
            AddPaths(paths);
        }
    }

    private void View_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.O)
        {
            AddFiles();
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.L)
        {
            Clear_Click(sender, new RoutedEventArgs());
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Delete)
        {
            RemoveSelected_Click(sender, new RoutedEventArgs());
            e.Handled = true;
        }
    }
}

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FNFAssetPrep.Models;
using FNFAssetPrep.Services;
using Microsoft.Win32;

namespace FNFAssetPrep;

public partial class MainWindow : Window
{
    private readonly AssetProcessor _processor = new();
    private bool _isRunning;

    public ObservableCollection<AssetItem> Assets { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        OutputFolderText.Text = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "FNF Asset Prep");

        AudioEngineStatus.Text = AssetProcessor.HasAudioEngine
            ? "Audio engine: ready"
            : "Audio engine: missing";

        UpdateUi();
    }

    private void AddFiles_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Add assets",
            Multiselect = true,
            CheckFileExists = true,
            Filter = "Supported assets|*.png;*.jpg;*.jpeg;*.webp;*.bmp;*.tif;*.tiff;*.ogg;*.wav;*.mp3;*.flac;*.m4a;*.aac|Images|*.png;*.jpg;*.jpeg;*.webp;*.bmp;*.tif;*.tiff|Audio|*.ogg;*.wav;*.mp3;*.flac;*.m4a;*.aac|All files|*.*"
        };

        if (dialog.ShowDialog(this) == true)
        {
            AddPaths(dialog.FileNames);
        }
    }

    private void AddPaths(IEnumerable<string> paths)
    {
        var existing = new HashSet<string>(
            Assets.Select(item => item.SourcePath),
            StringComparer.OrdinalIgnoreCase);

        var skipped = 0;

        foreach (var path in paths)
        {
            if (!File.Exists(path))
            {
                skipped++;
                continue;
            }

            if (!AssetProcessor.IsSupported(path))
            {
                skipped++;
                continue;
            }

            if (!existing.Add(path))
            {
                continue;
            }

            Assets.Add(AssetProcessor.CreateItem(path));
        }

        if (skipped > 0)
        {
            StatusText.Text = skipped + " unsupported or missing file(s) skipped.";
        }
        else
        {
            StatusText.Text = "Ready";
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
        if (_isRunning) return;

        Assets.Clear();
        StatusText.Text = "Ready";
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
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (dialog.ShowDialog(this) == true)
        {
            OutputFolderText.Text = dialog.FolderName;
            UpdateUi();
        }
    }

    private void OpenOutput_Click(object sender, RoutedEventArgs e)
    {
        var folder = OutputFolderText.Text.Trim();

        if (string.IsNullOrWhiteSpace(folder))
        {
            return;
        }

        Directory.CreateDirectory(folder);
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            ArgumentList = { folder },
            UseShellExecute = true
        });
    }

    private async void PrepFiles_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning || Assets.Count == 0) return;

        var outputDirectory = OutputFolderText.Text.Trim();
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            MessageBox.Show(
                this,
                "Choose an output folder first.",
                "FNF Asset Prep",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var audioItems = Assets.Where(item => item.Type == "Audio" && item.SourceFormat != "OGG").ToList();
        if (audioItems.Count > 0 && !AssetProcessor.HasAudioEngine)
        {
            MessageBox.Show(
                this,
                "The FFmpeg audio engine is missing from this test build. Image conversion still works, but audio conversion needs tools\\ffmpeg.exe.",
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
            StatusText.Text = "Preparing file " + (index + 1) + " of " + Assets.Count;

            try
            {
                var output = await _processor.ProcessAsync(item, outputDirectory, quality);
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

        StatusText.Text = failedCount == 0
            ? "Finished. " + successCount + " file(s) prepared."
            : "Finished with " + failedCount + " error(s).";

        ProgressText.Text = failedCount == 0
            ? "Your prepared assets are in the output folder."
            : "Select a file with Error status and check its tooltip or try the source file again.";

        if (failedCount == 0)
        {
            MessageBox.Show(
                this,
                successCount + " file(s) prepared successfully.",
                "FNF Asset Prep",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private int GetSelectedQuality()
    {
        if (QualityCombo.SelectedItem is not ComboBoxItem item)
        {
            return 6;
        }

        var text = item.Content?.ToString() ?? "6";
        var firstPart = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

        return int.TryParse(firstPart, out var quality) ? quality : 6;
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
        FileCountStatus.Text = Assets.Count + (Assets.Count == 1 ? " file" : " files");
        EmptyHint.Visibility = Assets.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        PrepButton.IsEnabled = !_isRunning && Assets.Count > 0 && !string.IsNullOrWhiteSpace(OutputFolderText.Text);
    }

    private void Window_DragOver(object sender, System.Windows.DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, System.Windows.DragEventArgs e)
    {
        if (_isRunning) return;

        if (e.Data.GetData(DataFormats.FileDrop) is string[] paths)
        {
            AddPaths(paths);
        }
    }

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.O)
        {
            AddFiles_Click(sender, new RoutedEventArgs());
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.O)
        {
            ChooseOutput_Click(sender, new RoutedEventArgs());
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

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            this,
            "FNF Asset Prep native rewrite\n\nA Windows desktop tool for preparing images as PNG and audio as OGG.\n\nDevelopment build. Not a public release.",
            "About FNF Asset Prep",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}

using System.Windows;
using System.Windows.Controls;
using FNFAssetPrep.Models;
using FNFAssetPrep.Services;
using Microsoft.Win32;

namespace FNFAssetPrep.Views;

public partial class YouTubeAudioView : UserControl
{
    private readonly SettingsService _settingsService;
    private readonly MediaDownloadService _downloadService = new();
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _folderChangedByUser;

    public YouTubeAudioView(SettingsService settingsService)
    {
        InitializeComponent();
        _settingsService = settingsService;
        ApplySettings(forceFolder: true);
        RefreshToolStatus();
    }

    public void ApplySettings(bool forceFolder = false)
    {
        var settings = _settingsService.Settings;

        if (forceFolder || !_folderChangedByUser)
        {
            DownloadFolderText.Text = settings.DownloadOutputFolder;
        }

        SelectCombo(FormatCombo, settings.DownloadFormat);
        SelectCombo(QualityCombo, settings.DownloadQuality);
        SelectCombo(FileNameCombo, settings.DownloadFileNameMode);
        MetadataCheckBox.IsChecked = settings.EmbedMetadata;
        ThumbnailCheckBox.IsChecked = settings.EmbedThumbnail;
        RefreshToolStatus();
    }

    private async void CheckLink_Click(object sender, RoutedEventArgs e)
    {
        var url = UrlTextBox.Text.Trim();

        if (!MediaDownloadService.IsYouTubeUrl(url))
        {
            ShowError("Enter a valid YouTube video URL.");
            return;
        }

        SetBusy(true, "Checking link...");

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(45));
            var media = await _downloadService.GetInfoAsync(url, cts.Token);

            VideoTitleText.Text = media.Title;
            VideoChannelText.Text = media.Channel;
            VideoDurationText.Text = media.Duration;
            AddActivity("Loaded: " + media.Title);
            DownloadStatusText.Text = "Ready to download";
        }
        catch (Exception exception)
        {
            VideoTitleText.Text = "Could not load this video.";
            VideoChannelText.Text = string.Empty;
            VideoDurationText.Text = string.Empty;
            ShowError(exception.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void Download_Click(object sender, RoutedEventArgs e)
    {
        var url = UrlTextBox.Text.Trim();
        var outputFolder = DownloadFolderText.Text.Trim();

        if (!MediaDownloadService.IsYouTubeUrl(url))
        {
            ShowError("Enter a valid YouTube video URL.");
            return;
        }

        if (string.IsNullOrWhiteSpace(outputFolder))
        {
            ShowError("Choose a download folder.");
            return;
        }

        if (!MediaDownloadService.HasDownloader || !AssetProcessor.HasAudioEngine)
        {
            ShowError("The download tools are missing from the tools folder.");
            return;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        SetBusy(true, "Starting download...");
        CancelButton.IsEnabled = true;
        DownloadProgressBar.Value = 0;

        var options = new DownloadOptions(
            ReadCombo(FormatCombo, "OGG"),
            ReadCombo(QualityCombo, "Best"),
            ReadCombo(FileNameCombo, "Title [ID]"),
            MetadataCheckBox.IsChecked == true,
            ThumbnailCheckBox.IsChecked == true);

        AddActivity($"Starting {options.Format} download.");

        var progress = new Progress<DownloadProgress>(item =>
        {
            if (item.Percent.HasValue)
            {
                DownloadProgressBar.IsIndeterminate = false;
                DownloadProgressBar.Value = item.Percent.Value;
            }
            else
            {
                DownloadProgressBar.IsIndeterminate = true;
            }

            if (!string.IsNullOrWhiteSpace(item.Message))
            {
                DownloadStatusText.Text = item.Message;
            }
        });

        try
        {
            var finalPath = await _downloadService.DownloadAudioAsync(
                url,
                outputFolder,
                options,
                progress,
                _cancellationTokenSource.Token);

            DownloadProgressBar.IsIndeterminate = false;
            DownloadProgressBar.Value = 100;
            DownloadStatusText.Text = "Download finished";

            if (!string.IsNullOrWhiteSpace(finalPath))
            {
                AddActivity("Saved: " + finalPath);
            }

            if (_settingsService.Settings.OpenFolderAfterTask)
            {
                ShellService.OpenFolder(outputFolder);
            }

            if (_settingsService.Settings.ShowCompletionDialog)
            {
                MessageBox.Show(
                    Window.GetWindow(this),
                    "Audio download finished.",
                    "FNF Asset Prep",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (OperationCanceledException)
        {
            DownloadProgressBar.IsIndeterminate = false;
            DownloadStatusText.Text = "Download cancelled";
            AddActivity("Cancelled.");
        }
        catch (Exception exception)
        {
            DownloadProgressBar.IsIndeterminate = false;
            ShowError(exception.Message);
        }
        finally
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            SetBusy(false);
            CancelButton.IsEnabled = false;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) =>
        _cancellationTokenSource?.Cancel();

    private void ChooseDownloadFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Choose where downloaded audio will be saved",
            Multiselect = false,
            InitialDirectory = Directory.Exists(DownloadFolderText.Text)
                ? DownloadFolderText.Text
                : _settingsService.Settings.DownloadOutputFolder
        };

        if (dialog.ShowDialog(Window.GetWindow(this)) == true)
        {
            DownloadFolderText.Text = dialog.FolderName;
            _folderChangedByUser = true;
        }
    }

    private void OpenDownloadFolder_Click(object sender, RoutedEventArgs e) =>
        ShellService.OpenFolder(DownloadFolderText.Text.Trim());

    private void SetBusy(bool busy, string? status = null)
    {
        CheckLinkButton.IsEnabled = !busy;
        DownloadButton.IsEnabled = !busy;
        UrlTextBox.IsEnabled = !busy;
        FormatCombo.IsEnabled = !busy;
        QualityCombo.IsEnabled = !busy;
        FileNameCombo.IsEnabled = !busy;

        if (!string.IsNullOrWhiteSpace(status))
        {
            DownloadStatusText.Text = status;
        }
    }

    private void RefreshToolStatus()
    {
        DownloaderStatusText.Text = MediaDownloadService.HasDownloader ? "yt-dlp: ready" : "yt-dlp: missing";
        FfmpegStatusText.Text = AssetProcessor.HasAudioEngine ? "FFmpeg: ready" : "FFmpeg: missing";
    }

    private void AddActivity(string message)
    {
        ActivityTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        ActivityTextBox.ScrollToEnd();
    }

    private void ShowError(string message)
    {
        DownloadStatusText.Text = message;
        AddActivity("Error: " + message);
    }

    private static void SelectCombo(ComboBox combo, string value)
    {
        foreach (var item in combo.Items.OfType<ComboBoxItem>())
        {
            if (string.Equals(item.Content?.ToString(), value, StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedItem = item;
                return;
            }
        }

        combo.SelectedIndex = 0;
    }

    private static string ReadCombo(ComboBox combo, string fallback) =>
        combo.SelectedItem is ComboBoxItem item ? item.Content?.ToString() ?? fallback : fallback;
}

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
    private MediaInfo? _mediaInfo;

    public YouTubeAudioView(SettingsService settingsService)
    {
        InitializeComponent();

        _settingsService = settingsService;
        ApplySettings(forceFolder: true);
        RefreshToolStatus();
    }

    public void ApplySettings(bool forceFolder = false)
    {
        if (forceFolder || !_folderChangedByUser)
        {
            DownloadFolderText.Text = _settingsService.Settings.DownloadOutputFolder;
        }

        SetFormat(_settingsService.Settings.DownloadFormat);
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
        _mediaInfo = null;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(45));
            _mediaInfo = await _downloadService.GetInfoAsync(url, cts.Token);

            VideoTitleText.Text = _mediaInfo.Title;
            VideoChannelText.Text = _mediaInfo.Channel;
            VideoDurationText.Text = _mediaInfo.Duration;
            AddActivity("Loaded: " + _mediaInfo.Title);
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

        if (!MediaDownloadService.HasDownloader)
        {
            ShowError("yt-dlp is missing from the tools folder.");
            return;
        }

        if (!AssetProcessor.HasAudioEngine)
        {
            ShowError("FFmpeg is missing from the tools folder.");
            return;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        SetBusy(true, "Starting download...");
        CancelButton.IsEnabled = true;
        DownloadProgressBar.Value = 0;

        var format = GetSelectedFormat();
        AddActivity("Starting " + format + " download.");

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
                AddActivity(item.Message);
            }
        });

        try
        {
            var finalPath = await _downloadService.DownloadAudioAsync(
                url,
                outputFolder,
                format,
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

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        _cancellationTokenSource?.Cancel();
    }

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

    private void OpenDownloadFolder_Click(object sender, RoutedEventArgs e)
    {
        ShellService.OpenFolder(DownloadFolderText.Text.Trim());
    }

    private void SetFormat(string format)
    {
        foreach (var item in FormatCombo.Items.OfType<ComboBoxItem>())
        {
            if (string.Equals(
                item.Content?.ToString(),
                format,
                StringComparison.OrdinalIgnoreCase))
            {
                FormatCombo.SelectedItem = item;
                return;
            }
        }

        FormatCombo.SelectedIndex = 0;
    }

    private string GetSelectedFormat()
    {
        return FormatCombo.SelectedItem is ComboBoxItem item
            ? item.Content?.ToString() ?? "OGG"
            : "OGG";
    }

    private void SetBusy(bool busy, string? status = null)
    {
        CheckLinkButton.IsEnabled = !busy;
        DownloadButton.IsEnabled = !busy;
        UrlTextBox.IsEnabled = !busy;
        FormatCombo.IsEnabled = !busy;

        if (!string.IsNullOrWhiteSpace(status))
        {
            DownloadStatusText.Text = status;
        }
    }

    private void RefreshToolStatus()
    {
        DownloaderStatusText.Text = MediaDownloadService.HasDownloader
            ? "yt-dlp: ready"
            : "yt-dlp: missing";

        FfmpegStatusText.Text = AssetProcessor.HasAudioEngine
            ? "FFmpeg: ready"
            : "FFmpeg: missing";
    }

    private void AddActivity(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        ActivityTextBox.AppendText("[" + timestamp + "] " + message + Environment.NewLine);
        ActivityTextBox.ScrollToEnd();
    }

    private void ShowError(string message)
    {
        DownloadStatusText.Text = message;
        AddActivity("Error: " + message);
    }
}

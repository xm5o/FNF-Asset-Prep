using System.Windows;
using System.Windows.Controls;
using FNFAssetPrep.Services;
using FNFAssetPrep.Views;

namespace FNFAssetPrep;

public partial class MainWindow : Window
{
    private readonly SettingsService _settingsService = new();
    private readonly AssetPrepView _assetPrepView;
    private readonly YouTubeAudioView _youTubeAudioView;
    private readonly SettingsView _settingsView;
    private readonly AboutView _aboutView;

    public MainWindow()
    {
        InitializeComponent();

        _settingsService.Load();

        _assetPrepView = new AssetPrepView(_settingsService);
        _youTubeAudioView = new YouTubeAudioView(_settingsService);
        _settingsView = new SettingsView(_settingsService);
        _aboutView = new AboutView();

        _settingsView.SettingsSaved += SettingsView_SettingsSaved;

        NavigationList.SelectedIndex = 0;
        UpdateToolStatus();
    }

    private void NavigationList_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (NavigationList.SelectedItem is not ListBoxItem item)
        {
            return;
        }

        switch (item.Tag?.ToString())
        {
            case "youtube":
                PageHost.Content = _youTubeAudioView;
                GlobalStatusText.Text = "YouTube Audio";
                break;

            case "settings":
                _settingsView.Refresh();
                PageHost.Content = _settingsView;
                GlobalStatusText.Text = "Settings";
                break;

            case "about":
                _aboutView.Refresh();
                PageHost.Content = _aboutView;
                GlobalStatusText.Text = "About";
                break;

            default:
                PageHost.Content = _assetPrepView;
                GlobalStatusText.Text = "Asset Prep";
                break;
        }
    }

    private void SettingsView_SettingsSaved(object? sender, EventArgs e)
    {
        _assetPrepView.ApplySettings();
        _youTubeAudioView.ApplySettings();
        _aboutView.Refresh();
        UpdateToolStatus();
        GlobalStatusText.Text = "Settings saved";
    }

    private void UpdateToolStatus()
    {
        var ffmpeg = AssetProcessor.HasAudioEngine ? "FFmpeg ready" : "FFmpeg missing";
        var downloader = MediaDownloadService.HasDownloader ? "yt-dlp ready" : "yt-dlp missing";
        ToolStatusText.Text = ffmpeg + " | " + downloader;
    }

    private void MenuAddFiles_Click(object sender, RoutedEventArgs e)
    {
        NavigationList.SelectedIndex = 0;
        _assetPrepView.AddFiles();
    }

    private void MenuAssetPrep_Click(object sender, RoutedEventArgs e)
    {
        NavigationList.SelectedIndex = 0;
    }

    private void MenuYouTube_Click(object sender, RoutedEventArgs e)
    {
        NavigationList.SelectedIndex = 1;
    }

    private void MenuSettings_Click(object sender, RoutedEventArgs e)
    {
        NavigationList.SelectedIndex = 2;
    }

    private void MenuAbout_Click(object sender, RoutedEventArgs e)
    {
        NavigationList.SelectedIndex = 3;
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using FNFAssetPrep.Services;
using FNFAssetPrep.Views;

namespace FNFAssetPrep;

public partial class MainWindow : Window
{
    private readonly SettingsService _settingsService = new();
    private readonly AssetPrepView _assetPrepView;
    private readonly AudioConverterView _audioConverterView;
    private readonly YouTubeAudioView _youTubeAudioView;
    private readonly SettingsView _settingsView;
    private readonly AboutView _aboutView;
    private string _currentPageTag = "assets";

    public MainWindow()
    {
        _settingsService.Load();
        ThemeService.Apply(_settingsService.Settings.ThemeMode);

        InitializeComponent();
        RestoreWindowSize();

        _assetPrepView = new AssetPrepView(_settingsService);
        _audioConverterView = new AudioConverterView(_settingsService);
        _youTubeAudioView = new YouTubeAudioView(_settingsService);
        _settingsView = new SettingsView(_settingsService);
        _aboutView = new AboutView();

        _settingsView.SettingsSaved += SettingsView_SettingsSaved;

        SelectInitialPage();
        UpdateToolStatus();
    }

    private void SelectInitialPage()
    {
        var tag = _settingsService.Settings.RememberLastPage
            ? _settingsService.Settings.LastPage
            : StartPageTag(_settingsService.Settings.StartPage);

        SelectNavigationTag(tag);
    }

    private void NavigationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NavigationList.SelectedItem is not ListBoxItem item)
        {
            return;
        }

        _currentPageTag = item.Tag?.ToString() ?? "assets";

        switch (_currentPageTag)
        {
            case "converter":
                _audioConverterView.ApplySettings();
                PageHost.Content = _audioConverterView;
                GlobalStatusText.Text = "Audio Converter";
                break;

            case "youtube":
                _youTubeAudioView.ApplySettings();
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
                _assetPrepView.ApplySettings();
                PageHost.Content = _assetPrepView;
                GlobalStatusText.Text = "Asset Prep";
                break;
        }
    }

    private void SettingsView_SettingsSaved(object? sender, EventArgs e)
    {
        ThemeService.Apply(_settingsService.Settings.ThemeMode);
        _assetPrepView.ApplySettings();
        _audioConverterView.ApplySettings();
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
        ThemeStatusText.Text = ThemeService.ResolvedTheme + " theme";
    }

    private void RestoreWindowSize()
    {
        var settings = _settingsService.Settings;

        if (!settings.RememberWindowSize)
        {
            return;
        }

        Width = settings.WindowWidth;
        Height = settings.WindowHeight;
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        var settings = _settingsService.Settings;
        settings.LastPage = _currentPageTag;

        if (settings.RememberWindowSize)
        {
            var bounds = WindowState == WindowState.Normal
                ? new Rect(Left, Top, ActualWidth, ActualHeight)
                : RestoreBounds;

            settings.WindowWidth = Math.Clamp(bounds.Width, MinWidth, 2200);
            settings.WindowHeight = Math.Clamp(bounds.Height, MinHeight, 1600);
        }

        _settingsService.Save(settings);
    }

    private void SelectNavigationTag(string tag)
    {
        foreach (var item in NavigationList.Items.OfType<ListBoxItem>())
        {
            if (string.Equals(item.Tag?.ToString(), tag, StringComparison.OrdinalIgnoreCase))
            {
                NavigationList.SelectedItem = item;
                return;
            }
        }

        NavigationList.SelectedIndex = 0;
    }

    private static string StartPageTag(string value) =>
        value switch
        {
            "Audio Converter" => "converter",
            "YouTube Audio" => "youtube",
            _ => "assets"
        };

    private void MenuAddFiles_Click(object sender, RoutedEventArgs e)
    {
        SelectNavigationTag("assets");
        _assetPrepView.AddFiles();
    }

    private void MenuAssetPrep_Click(object sender, RoutedEventArgs e) => SelectNavigationTag("assets");
    private void MenuConverter_Click(object sender, RoutedEventArgs e) => SelectNavigationTag("converter");
    private void MenuYouTube_Click(object sender, RoutedEventArgs e) => SelectNavigationTag("youtube");
    private void MenuSettings_Click(object sender, RoutedEventArgs e) => SelectNavigationTag("settings");
    private void MenuAbout_Click(object sender, RoutedEventArgs e) => SelectNavigationTag("about");
    private void Exit_Click(object sender, RoutedEventArgs e) => Close();
}

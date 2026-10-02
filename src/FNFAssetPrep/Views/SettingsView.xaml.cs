using System.Windows;
using System.Windows.Controls;
using FNFAssetPrep.Models;
using FNFAssetPrep.Services;
using Microsoft.Win32;

namespace FNFAssetPrep.Views;

public partial class SettingsView : UserControl
{
    private readonly SettingsService _settingsService;

    public event EventHandler? SettingsSaved;

    public SettingsView(SettingsService settingsService)
    {
        InitializeComponent();
        _settingsService = settingsService;
        Refresh();
    }

    public void Refresh()
    {
        var s = _settingsService.Settings;

        AssetFolderText.Text = s.AssetOutputFolder;
        ConverterFolderText.Text = s.ConverterOutputFolder;
        DownloadFolderText.Text = s.DownloadOutputFolder;

        SelectByContent(ThemeCombo, s.ThemeMode);
        SelectByContent(StartPageCombo, s.StartPage);
        SelectByContent(ConverterFormatCombo, s.ConverterFormat);
        SelectByContent(ConverterQualityCombo, s.ConverterQuality);
        SelectByContent(DownloadFormatCombo, s.DownloadFormat);
        SelectByContent(DownloadQualityCombo, s.DownloadQuality);
        SelectByContent(FileNameModeCombo, s.DownloadFileNameMode);
        SelectByTag(OggQualityCombo, s.OggQuality.ToString());

        RememberPageCheckBox.IsChecked = s.RememberLastPage;
        RememberWindowCheckBox.IsChecked = s.RememberWindowSize;
        EmbedMetadataCheckBox.IsChecked = s.EmbedMetadata;
        EmbedThumbnailCheckBox.IsChecked = s.EmbedThumbnail;
        OpenFolderCheckBox.IsChecked = s.OpenFolderAfterTask;
        ConfirmClearCheckBox.IsChecked = s.ConfirmBeforeClear;
        CompletionDialogCheckBox.IsChecked = s.ShowCompletionDialog;

        ToolsStatusText.Text =
            (AssetProcessor.HasAudioEngine ? "FFmpeg ready" : "FFmpeg missing")
            + " | "
            + (MediaDownloadService.HasDownloader ? "yt-dlp ready" : "yt-dlp missing");

        SettingsPathText.Text = _settingsService.SettingsPath;
    }

    private void BrowseAssetFolder_Click(object sender, RoutedEventArgs e) =>
        SetFolder(AssetFolderText, "Choose the default Asset Prep output folder");

    private void BrowseConverterFolder_Click(object sender, RoutedEventArgs e) =>
        SetFolder(ConverterFolderText, "Choose the default Audio Converter output folder");

    private void BrowseDownloadFolder_Click(object sender, RoutedEventArgs e) =>
        SetFolder(DownloadFolderText, "Choose the default YouTube Audio output folder");

    private void SetFolder(TextBox target, string title)
    {
        var dialog = new OpenFolderDialog
        {
            Title = title,
            Multiselect = false,
            InitialDirectory = Directory.Exists(target.Text)
                ? target.Text
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (dialog.ShowDialog(Window.GetWindow(this)) == true)
        {
            target.Text = dialog.FolderName;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var old = _settingsService.Settings;

        var settings = new AppSettings
        {
            ThemeMode = ReadContent(ThemeCombo, "System"),
            StartPage = ReadContent(StartPageCombo, "Asset Prep"),
            RememberLastPage = RememberPageCheckBox.IsChecked == true,
            LastPage = old.LastPage,
            RememberWindowSize = RememberWindowCheckBox.IsChecked == true,
            WindowWidth = old.WindowWidth,
            WindowHeight = old.WindowHeight,

            AssetOutputFolder = AssetFolderText.Text.Trim(),
            OggQuality = ReadTagInt(OggQualityCombo, 6),

            ConverterOutputFolder = ConverterFolderText.Text.Trim(),
            ConverterFormat = ReadContent(ConverterFormatCombo, "OGG"),
            ConverterQuality = ReadContent(ConverterQualityCombo, "High"),

            DownloadOutputFolder = DownloadFolderText.Text.Trim(),
            DownloadFormat = ReadContent(DownloadFormatCombo, "OGG"),
            DownloadQuality = ReadContent(DownloadQualityCombo, "Best"),
            DownloadFileNameMode = ReadContent(FileNameModeCombo, "Title [ID]"),
            EmbedMetadata = EmbedMetadataCheckBox.IsChecked == true,
            EmbedThumbnail = EmbedThumbnailCheckBox.IsChecked == true,

            OpenFolderAfterTask = OpenFolderCheckBox.IsChecked == true,
            ConfirmBeforeClear = ConfirmClearCheckBox.IsChecked == true,
            ShowCompletionDialog = CompletionDialogCheckBox.IsChecked == true
        };

        _settingsService.Save(settings);
        SettingsSaved?.Invoke(this, EventArgs.Empty);
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        var answer = MessageBox.Show(
            Window.GetWindow(this),
            "Reset all settings to their defaults?",
            "FNF Asset Prep",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes) return;

        _settingsService.Reset();
        Refresh();
        SettingsSaved?.Invoke(this, EventArgs.Empty);
    }

    private static void SelectByContent(ComboBox comboBox, string value)
    {
        foreach (var item in comboBox.Items.OfType<ComboBoxItem>())
        {
            if (string.Equals(item.Content?.ToString(), value, StringComparison.OrdinalIgnoreCase))
            {
                comboBox.SelectedItem = item;
                return;
            }
        }

        comboBox.SelectedIndex = 0;
    }

    private static void SelectByTag(ComboBox comboBox, string value)
    {
        foreach (var item in comboBox.Items.OfType<ComboBoxItem>())
        {
            if (item.Tag?.ToString() == value)
            {
                comboBox.SelectedItem = item;
                return;
            }
        }

        comboBox.SelectedIndex = 0;
    }

    private static int ReadTagInt(ComboBox comboBox, int fallback) =>
        comboBox.SelectedItem is ComboBoxItem item
        && int.TryParse(item.Tag?.ToString(), out var value)
            ? value
            : fallback;

    private static string ReadContent(ComboBox comboBox, string fallback) =>
        comboBox.SelectedItem is ComboBoxItem item
            ? item.Content?.ToString() ?? fallback
            : fallback;
}

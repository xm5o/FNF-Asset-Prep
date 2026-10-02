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
        var settings = _settingsService.Settings;

        AssetFolderText.Text = settings.AssetOutputFolder;
        DownloadFolderText.Text = settings.DownloadOutputFolder;
        OpenFolderCheckBox.IsChecked = settings.OpenFolderAfterTask;
        ConfirmClearCheckBox.IsChecked = settings.ConfirmBeforeClear;

        SelectByContent(DownloadFormatCombo, settings.DownloadFormat);
        SelectByTag(OggQualityCombo, settings.OggQuality.ToString());

        ToolsStatusText.Text =
            (AssetProcessor.HasAudioEngine ? "FFmpeg ready" : "FFmpeg missing")
            + " | "
            + (MediaDownloadService.HasDownloader ? "yt-dlp ready" : "yt-dlp missing");

        SettingsPathText.Text = _settingsService.SettingsPath;
    }

    private void BrowseAssetFolder_Click(object sender, RoutedEventArgs e)
    {
        var selected = BrowseForFolder(
            "Choose the default Asset Prep output folder",
            AssetFolderText.Text);

        if (!string.IsNullOrWhiteSpace(selected))
        {
            AssetFolderText.Text = selected;
        }
    }

    private void BrowseDownloadFolder_Click(object sender, RoutedEventArgs e)
    {
        var selected = BrowseForFolder(
            "Choose the default YouTube Audio output folder",
            DownloadFolderText.Text);

        if (!string.IsNullOrWhiteSpace(selected))
        {
            DownloadFolderText.Text = selected;
        }
    }

    private string? BrowseForFolder(string title, string current)
    {
        var dialog = new OpenFolderDialog
        {
            Title = title,
            Multiselect = false,
            InitialDirectory = Directory.Exists(current)
                ? current
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        return dialog.ShowDialog(Window.GetWindow(this)) == true
            ? dialog.FolderName
            : null;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var settings = new AppSettings
        {
            AssetOutputFolder = AssetFolderText.Text.Trim(),
            DownloadOutputFolder = DownloadFolderText.Text.Trim(),
            OggQuality = ReadTagInt(OggQualityCombo, 6),
            DownloadFormat = ReadContent(DownloadFormatCombo, "OGG"),
            OpenFolderAfterTask = OpenFolderCheckBox.IsChecked == true,
            ConfirmBeforeClear = ConfirmClearCheckBox.IsChecked == true
        };

        _settingsService.Save(settings);
        SettingsSaved?.Invoke(this, EventArgs.Empty);

        MessageBox.Show(
            Window.GetWindow(this),
            "Settings saved.",
            "FNF Asset Prep",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        var answer = MessageBox.Show(
            Window.GetWindow(this),
            "Reset all settings to their defaults?",
            "FNF Asset Prep",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
        {
            return;
        }

        _settingsService.Reset();
        Refresh();
        SettingsSaved?.Invoke(this, EventArgs.Empty);
    }

    private static void SelectByContent(ComboBox comboBox, string value)
    {
        foreach (var item in comboBox.Items.OfType<ComboBoxItem>())
        {
            if (string.Equals(
                item.Content?.ToString(),
                value,
                StringComparison.OrdinalIgnoreCase))
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

    private static int ReadTagInt(ComboBox comboBox, int fallback)
    {
        return comboBox.SelectedItem is ComboBoxItem item
            && int.TryParse(item.Tag?.ToString(), out var value)
                ? value
                : fallback;
    }

    private static string ReadContent(ComboBox comboBox, string fallback)
    {
        return comboBox.SelectedItem is ComboBoxItem item
            ? item.Content?.ToString() ?? fallback
            : fallback;
    }
}

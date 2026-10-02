using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using FNFAssetPrep.Models;
using FNFAssetPrep.Services;
using Microsoft.Win32;

namespace FNFAssetPrep.Views;

public partial class AudioConverterView : UserControl
{
    private readonly SettingsService _settingsService;
    private readonly AudioConverterService _converter = new();
    private bool _running;
    private bool _folderChanged;

    public ObservableCollection<AudioConvertItem> Items { get; } = new();

    public AudioConverterView(SettingsService settingsService)
    {
        InitializeComponent();
        DataContext = this;
        _settingsService = settingsService;
        ApplySettings(forceFolder: true);
        UpdateUi();
    }

    public void ApplySettings(bool forceFolder = false)
    {
        if (forceFolder || !_folderChanged)
        {
            OutputFolderText.Text = _settingsService.Settings.ConverterOutputFolder;
        }

        SelectCombo(FormatCombo, _settingsService.Settings.ConverterFormat);
        SelectCombo(QualityCombo, _settingsService.Settings.ConverterQuality);
        UpdateUi();
    }

    private void AddFiles_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Add audio files",
            Multiselect = true,
            Filter = "Audio files|*.ogg;*.wav;*.mp3;*.flac;*.m4a;*.aac;*.opus;*.wma|All files|*.*"
        };

        if (dialog.ShowDialog(Window.GetWindow(this)) == true)
        {
            AddPaths(dialog.FileNames);
        }
    }

    private void AddPaths(IEnumerable<string> paths)
    {
        var existing = new HashSet<string>(Items.Select(item => item.SourcePath), StringComparer.OrdinalIgnoreCase);

        foreach (var path in paths)
        {
            if (!File.Exists(path) || !AudioConverterService.IsSupported(path) || !existing.Add(path))
            {
                continue;
            }

            Items.Add(new AudioConvertItem { SourcePath = path });
        }

        UpdateUi();
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (_running) return;

        foreach (var item in AudioGrid.SelectedItems.Cast<AudioConvertItem>().ToList())
        {
            Items.Remove(item);
        }

        UpdateUi();
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        if (_running) return;
        Items.Clear();
        UpdateUi();
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Choose converted audio folder",
            InitialDirectory = Directory.Exists(OutputFolderText.Text)
                ? OutputFolderText.Text
                : _settingsService.Settings.ConverterOutputFolder
        };

        if (dialog.ShowDialog(Window.GetWindow(this)) == true)
        {
            OutputFolderText.Text = dialog.FolderName;
            _folderChanged = true;
            UpdateUi();
        }
    }

    private void Open_Click(object sender, RoutedEventArgs e) =>
        ShellService.OpenFolder(OutputFolderText.Text.Trim());

    private async void Convert_Click(object sender, RoutedEventArgs e)
    {
        if (_running || Items.Count == 0) return;

        _running = true;
        UpdateUi();

        var format = ReadCombo(FormatCombo, "OGG");
        var quality = ReadCombo(QualityCombo, "High");
        var output = OutputFolderText.Text.Trim();
        var failed = 0;

        foreach (var item in Items)
        {
            item.Status = "Working";

            try
            {
                item.OutputPath = await _converter.ConvertAsync(item.SourcePath, output, format, quality);
                item.Status = "Done";
            }
            catch
            {
                item.Status = "Error";
                failed++;
            }
        }

        _running = false;
        UpdateUi();

        if (failed == 0 && _settingsService.Settings.OpenFolderAfterTask)
        {
            ShellService.OpenFolder(output);
        }

        if (_settingsService.Settings.ShowCompletionDialog)
        {
            MessageBox.Show(
                Window.GetWindow(this),
                failed == 0 ? "Audio conversion finished." : $"{failed} file(s) failed.",
                "FNF Asset Prep",
                MessageBoxButton.OK,
                failed == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
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
        if (_running) return;
        if (e.Data.GetData(DataFormats.FileDrop) is string[] paths) AddPaths(paths);
    }

    private void UpdateUi()
    {
        var hasFiles = Items.Count > 0;
        EmptyState.Visibility = hasFiles ? Visibility.Collapsed : Visibility.Visible;
        FileTablePanel.Visibility = hasFiles ? Visibility.Visible : Visibility.Collapsed;
        AudioGrid.IsEnabled = !_running;
        ConvertButton.IsEnabled = !_running && hasFiles && !string.IsNullOrWhiteSpace(OutputFolderText.Text);
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

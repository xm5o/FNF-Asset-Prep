using System.Text.Json;
using FNFAssetPrep.Models;

namespace FNFAssetPrep.Services;

public sealed class SettingsService
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public AppSettings Settings { get; private set; } = AppSettings.CreateDefaults();

    public string SettingsPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FNF Asset Prep",
            "settings.json");

    public event EventHandler? SettingsChanged;

    public void Load()
    {
        var defaults = AppSettings.CreateDefaults();

        try
        {
            if (!File.Exists(SettingsPath))
            {
                Settings = defaults;
                return;
            }

            var json = File.ReadAllText(SettingsPath);
            var loaded = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);

            if (loaded is null)
            {
                Settings = defaults;
                return;
            }

            loaded.ThemeMode = Validate(loaded.ThemeMode, new[] { "System", "Light", "Dark" }, "System");
            loaded.StartPage = Validate(loaded.StartPage, new[] { "Asset Prep", "Audio Converter", "YouTube Audio" }, "Asset Prep");
            loaded.LastPage = Validate(loaded.LastPage, new[] { "assets", "converter", "youtube", "settings", "about" }, "assets");

            loaded.AssetOutputFolder = FolderOrDefault(loaded.AssetOutputFolder, defaults.AssetOutputFolder);
            loaded.ConverterOutputFolder = FolderOrDefault(loaded.ConverterOutputFolder, defaults.ConverterOutputFolder);
            loaded.DownloadOutputFolder = FolderOrDefault(loaded.DownloadOutputFolder, defaults.DownloadOutputFolder);

            loaded.OggQuality = Math.Clamp(loaded.OggQuality, 3, 8);

            var formats = new[] { "OGG", "MP3", "WAV", "FLAC", "M4A", "OPUS", "AAC", "ALAC" };
            loaded.ConverterFormat = Validate(loaded.ConverterFormat, formats, "OGG").ToUpperInvariant();
            loaded.DownloadFormat = Validate(loaded.DownloadFormat, formats, "OGG").ToUpperInvariant();

            var qualities = new[] { "Best", "High", "Balanced", "Small" };
            loaded.ConverterQuality = Validate(loaded.ConverterQuality, qualities, "High");
            loaded.DownloadQuality = Validate(loaded.DownloadQuality, qualities, "Best");

            loaded.DownloadFileNameMode = Validate(
                loaded.DownloadFileNameMode,
                new[] { "Title", "Title [ID]", "Channel - Title" },
                "Title [ID]");

            loaded.WindowWidth = Math.Clamp(loaded.WindowWidth, 940, 2200);
            loaded.WindowHeight = Math.Clamp(loaded.WindowHeight, 620, 1600);

            Settings = loaded;
        }
        catch
        {
            Settings = defaults;
        }
    }

    public void Save(AppSettings settings)
    {
        Settings = settings;

        var directory = Path.GetDirectoryName(SettingsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(
            SettingsPath,
            JsonSerializer.Serialize(Settings, _jsonOptions));

        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Reset()
    {
        Save(AppSettings.CreateDefaults());
    }

    private static string FolderOrDefault(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value;

    private static string Validate(string? value, IEnumerable<string> allowed, string fallback) =>
        allowed.FirstOrDefault(item =>
            string.Equals(item, value, StringComparison.OrdinalIgnoreCase)) ?? fallback;
}

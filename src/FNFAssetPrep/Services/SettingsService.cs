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

            loaded.AssetOutputFolder = string.IsNullOrWhiteSpace(loaded.AssetOutputFolder)
                ? defaults.AssetOutputFolder
                : loaded.AssetOutputFolder;

            loaded.DownloadOutputFolder = string.IsNullOrWhiteSpace(loaded.DownloadOutputFolder)
                ? defaults.DownloadOutputFolder
                : loaded.DownloadOutputFolder;

            loaded.OggQuality = Math.Clamp(loaded.OggQuality, 3, 8);

            var validFormats = new[] { "OGG", "MP3", "WAV", "FLAC", "M4A", "OPUS", "AAC", "ALAC" };
            loaded.DownloadFormat = validFormats.Contains(loaded.DownloadFormat, StringComparer.OrdinalIgnoreCase)
                ? loaded.DownloadFormat.ToUpperInvariant()
                : "OGG";

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
}

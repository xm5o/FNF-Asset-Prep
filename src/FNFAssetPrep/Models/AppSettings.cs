namespace FNFAssetPrep.Models;

public sealed class AppSettings
{
    public string ThemeMode { get; set; } = "System";
    public string StartPage { get; set; } = "Asset Prep";
    public bool RememberLastPage { get; set; } = true;
    public string LastPage { get; set; } = "assets";
    public bool RememberWindowSize { get; set; } = true;
    public double WindowWidth { get; set; } = 1120;
    public double WindowHeight { get; set; } = 760;

    public string AssetOutputFolder { get; set; } = string.Empty;
    public int OggQuality { get; set; } = 6;

    public string ConverterOutputFolder { get; set; } = string.Empty;
    public string ConverterFormat { get; set; } = "OGG";
    public string ConverterQuality { get; set; } = "High";

    public string DownloadOutputFolder { get; set; } = string.Empty;
    public string DownloadFormat { get; set; } = "OGG";
    public string DownloadQuality { get; set; } = "Best";
    public string DownloadFileNameMode { get; set; } = "Title [ID]";
    public bool EmbedMetadata { get; set; } = true;
    public bool EmbedThumbnail { get; set; }

    public bool OpenFolderAfterTask { get; set; }
    public bool ConfirmBeforeClear { get; set; } = true;
    public bool ShowCompletionDialog { get; set; }

    public static AppSettings CreateDefaults()
    {
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var music = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);

        if (string.IsNullOrWhiteSpace(music))
        {
            music = documents;
        }

        return new AppSettings
        {
            ThemeMode = "System",
            StartPage = "Asset Prep",
            RememberLastPage = true,
            LastPage = "assets",
            RememberWindowSize = true,
            WindowWidth = 1120,
            WindowHeight = 760,
            AssetOutputFolder = Path.Combine(documents, "FNF Asset Prep", "Prepared Assets"),
            OggQuality = 6,
            ConverterOutputFolder = Path.Combine(music, "FNF Asset Prep", "Converted Audio"),
            ConverterFormat = "OGG",
            ConverterQuality = "High",
            DownloadOutputFolder = Path.Combine(music, "FNF Asset Prep", "Downloads"),
            DownloadFormat = "OGG",
            DownloadQuality = "Best",
            DownloadFileNameMode = "Title [ID]",
            EmbedMetadata = true,
            EmbedThumbnail = false,
            OpenFolderAfterTask = false,
            ConfirmBeforeClear = true,
            ShowCompletionDialog = false
        };
    }
}

namespace FNFAssetPrep.Models;

public sealed class AppSettings
{
    public string AssetOutputFolder { get; set; } = string.Empty;
    public string DownloadOutputFolder { get; set; } = string.Empty;
    public int OggQuality { get; set; } = 6;
    public string DownloadFormat { get; set; } = "OGG";
    public bool OpenFolderAfterTask { get; set; }
    public bool ConfirmBeforeClear { get; set; } = true;

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
            AssetOutputFolder = Path.Combine(documents, "FNF Asset Prep", "Prepared Assets"),
            DownloadOutputFolder = Path.Combine(music, "FNF Asset Prep", "Downloads"),
            OggQuality = 6,
            DownloadFormat = "OGG",
            OpenFolderAfterTask = false,
            ConfirmBeforeClear = true
        };
    }
}

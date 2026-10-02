namespace FNFAssetPrep.Models;

public sealed record DownloadOptions(
    string Format,
    string Quality,
    string FileNameMode,
    bool EmbedMetadata,
    bool EmbedThumbnail);

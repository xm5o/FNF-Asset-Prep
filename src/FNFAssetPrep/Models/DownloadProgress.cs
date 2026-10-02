namespace FNFAssetPrep.Models;

public sealed record DownloadProgress(
    double? Percent,
    string Message,
    string? FinalPath = null);

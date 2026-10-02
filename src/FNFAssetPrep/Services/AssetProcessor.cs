using System.Diagnostics;
using System.IO;
using FNFAssetPrep.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

namespace FNFAssetPrep.Services;

public sealed class AssetProcessor
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp", ".bmp", ".tif", ".tiff"
    };

    private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".ogg", ".wav", ".mp3", ".flac", ".m4a", ".aac"
    };

    public static bool IsSupported(string path)
    {
        var extension = Path.GetExtension(path);
        return ImageExtensions.Contains(extension) || AudioExtensions.Contains(extension);
    }

    public static AssetItem CreateItem(string path)
    {
        var extension = Path.GetExtension(path);

        if (ImageExtensions.Contains(extension))
        {
            return new AssetItem
            {
                SourcePath = path,
                Type = "Image",
                TargetFormat = "PNG"
            };
        }

        if (AudioExtensions.Contains(extension))
        {
            return new AssetItem
            {
                SourcePath = path,
                Type = "Audio",
                TargetFormat = "OGG"
            };
        }

        throw new NotSupportedException("Unsupported file type.");
    }

    public static string FfmpegPath =>
        Path.Combine(AppContext.BaseDirectory, "tools", "ffmpeg.exe");

    public static bool HasAudioEngine => File.Exists(FfmpegPath);

    public async Task<string> ProcessAsync(
        AssetItem item,
        string outputDirectory,
        int oggQuality,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(outputDirectory);

        return item.Type switch
        {
            "Image" => await ProcessImageAsync(item.SourcePath, outputDirectory, cancellationToken),
            "Audio" => await ProcessAudioAsync(item.SourcePath, outputDirectory, oggQuality, cancellationToken),
            _ => throw new NotSupportedException("Unsupported asset type.")
        };
    }

    private static async Task<string> ProcessImageAsync(
        string sourcePath,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        var outputPath = OutputPathHelper.GetUniquePath(outputDirectory, sourcePath, ".png");
        var extension = Path.GetExtension(sourcePath);

        if (extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
        {
            File.Copy(sourcePath, outputPath, overwrite: false);
            return outputPath;
        }

        using var image = await Image.LoadAsync(sourcePath, cancellationToken);
        var encoder = new PngEncoder
        {
            CompressionLevel = PngCompressionLevel.BestCompression
        };

        await image.SaveAsPngAsync(outputPath, encoder, cancellationToken);
        return outputPath;
    }

    private static async Task<string> ProcessAudioAsync(
        string sourcePath,
        string outputDirectory,
        int oggQuality,
        CancellationToken cancellationToken)
    {
        var outputPath = OutputPathHelper.GetUniquePath(outputDirectory, sourcePath, ".ogg");
        var extension = Path.GetExtension(sourcePath);

        if (extension.Equals(".ogg", StringComparison.OrdinalIgnoreCase))
        {
            File.Copy(sourcePath, outputPath, overwrite: false);
            return outputPath;
        }

        if (!HasAudioEngine)
        {
            throw new FileNotFoundException(
                "FFmpeg was not found. This test build should include tools\\ffmpeg.exe.",
                FfmpegPath);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = FfmpegPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        startInfo.ArgumentList.Add("-hide_banner");
        startInfo.ArgumentList.Add("-loglevel");
        startInfo.ArgumentList.Add("error");
        startInfo.ArgumentList.Add("-y");
        startInfo.ArgumentList.Add("-i");
        startInfo.ArgumentList.Add(sourcePath);
        startInfo.ArgumentList.Add("-vn");
        startInfo.ArgumentList.Add("-c:a");
        startInfo.ArgumentList.Add("libvorbis");
        startInfo.ArgumentList.Add("-q:a");
        startInfo.ArgumentList.Add(oggQuality.ToString());
        startInfo.ArgumentList.Add(outputPath);

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        var stderr = await stderrTask;

        if (process.ExitCode != 0)
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(stderr)
                    ? "FFmpeg could not convert this audio file."
                    : stderr.Trim());
        }

        return outputPath;
    }
}

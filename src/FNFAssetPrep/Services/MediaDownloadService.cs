using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using FNFAssetPrep.Models;

namespace FNFAssetPrep.Services;

public sealed partial class MediaDownloadService
{
    public static string YtDlpPath =>
        Path.Combine(AppContext.BaseDirectory, "tools", "yt-dlp.exe");

    public static bool HasDownloader => File.Exists(YtDlpPath);

    public static bool IsYouTubeUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme is not ("http" or "https"))
        {
            return false;
        }

        var host = uri.Host.TrimStart('.').ToLowerInvariant();

        return host == "youtu.be"
            || host == "youtube.com"
            || host.EndsWith(".youtube.com", StringComparison.Ordinal);
    }

    public async Task<MediaInfo> GetInfoAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        EnsureReady(url, needsFfmpeg: false);

        var startInfo = CreateStartInfo();
        startInfo.ArgumentList.Add("--dump-single-json");
        startInfo.ArgumentList.Add("--skip-download");
        startInfo.ArgumentList.Add("--no-playlist");
        startInfo.ArgumentList.Add("--no-warnings");
        startInfo.ArgumentList.Add(url);

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        using var registration = cancellationToken.Register(() => Kill(process));

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(stderr)
                    ? "The video information could not be loaded."
                    : CleanError(stderr));
        }

        using var document = JsonDocument.Parse(stdout);
        var root = document.RootElement;

        var title = ReadString(root, "title", "Unknown title");
        var channel = ReadString(root, "channel", string.Empty);

        if (string.IsNullOrWhiteSpace(channel))
        {
            channel = ReadString(root, "uploader", "Unknown channel");
        }

        var durationSeconds = root.TryGetProperty("duration", out var durationElement)
            && durationElement.ValueKind == JsonValueKind.Number
            && durationElement.TryGetDouble(out var seconds)
                ? seconds
                : 0;

        var duration = durationSeconds > 0
            ? TimeSpan.FromSeconds(durationSeconds).ToString(
                durationSeconds >= 3600 ? @"h\:mm\:ss" : @"m\:ss",
                CultureInfo.InvariantCulture)
            : "Unknown";

        var sourceUrl = ReadString(root, "webpage_url", url);

        return new MediaInfo(title, channel, duration, sourceUrl);
    }

    public async Task<string?> DownloadAudioAsync(
        string url,
        string outputDirectory,
        DownloadOptions options,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        EnsureReady(url, needsFfmpeg: true);
        Directory.CreateDirectory(outputDirectory);

        var startInfo = CreateStartInfo();
        startInfo.ArgumentList.Add("--no-playlist");
        startInfo.ArgumentList.Add("--no-warnings");
        startInfo.ArgumentList.Add("--newline");
        startInfo.ArgumentList.Add("--progress");
        startInfo.ArgumentList.Add("--windows-filenames");
        startInfo.ArgumentList.Add("--ffmpeg-location");
        startInfo.ArgumentList.Add(Path.GetDirectoryName(AssetProcessor.FfmpegPath)!);
        startInfo.ArgumentList.Add("--extract-audio");
        startInfo.ArgumentList.Add("--audio-format");
        startInfo.ArgumentList.Add(MapFormat(options.Format));
        startInfo.ArgumentList.Add("--audio-quality");
        startInfo.ArgumentList.Add(MapQuality(options.Quality));

        if (options.EmbedMetadata)
        {
            startInfo.ArgumentList.Add("--embed-metadata");
        }

        if (options.EmbedThumbnail)
        {
            startInfo.ArgumentList.Add("--embed-thumbnail");
            startInfo.ArgumentList.Add("--convert-thumbnails");
            startInfo.ArgumentList.Add("jpg");
        }

        startInfo.ArgumentList.Add("--print");
        startInfo.ArgumentList.Add("after_move:FINAL:%(filepath)s");
        startInfo.ArgumentList.Add("--output");
        startInfo.ArgumentList.Add(Path.Combine(outputDirectory, FileNameTemplate(options.FileNameMode)));
        startInfo.ArgumentList.Add(url);

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        using var registration = cancellationToken.Register(() => Kill(process));

        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        string? finalPath = null;

        while (await process.StandardOutput.ReadLineAsync(cancellationToken) is { } line)
        {
            if (line.StartsWith("FINAL:", StringComparison.Ordinal))
            {
                finalPath = line["FINAL:".Length..].Trim();
                progress?.Report(new DownloadProgress(100, "Finished", finalPath));
                continue;
            }

            progress?.Report(new DownloadProgress(TryReadPercent(line), CleanLine(line)));
        }

        await process.WaitForExitAsync(cancellationToken);
        var stderr = await errorTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(stderr)
                    ? "The audio download failed."
                    : CleanError(stderr));
        }

        return finalPath;
    }

    private static ProcessStartInfo CreateStartInfo() => new()
    {
        FileName = YtDlpPath,
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };

    private static void EnsureReady(string url, bool needsFfmpeg)
    {
        if (!IsYouTubeUrl(url))
        {
            throw new ArgumentException("Enter a valid YouTube video URL.");
        }

        if (!HasDownloader)
        {
            throw new FileNotFoundException("yt-dlp was not found in the tools folder.", YtDlpPath);
        }

        if (needsFfmpeg && !AssetProcessor.HasAudioEngine)
        {
            throw new FileNotFoundException("FFmpeg was not found in the tools folder.", AssetProcessor.FfmpegPath);
        }
    }

    private static string MapFormat(string format) =>
        format.Trim().ToUpperInvariant() switch
        {
            "OGG" => "vorbis",
            "MP3" => "mp3",
            "WAV" => "wav",
            "FLAC" => "flac",
            "M4A" => "m4a",
            "OPUS" => "opus",
            "AAC" => "aac",
            "ALAC" => "alac",
            _ => "vorbis"
        };

    private static string MapQuality(string quality) =>
        quality switch
        {
            "Best" => "0",
            "High" => "2",
            "Balanced" => "5",
            "Small" => "7",
            _ => "0"
        };

    private static string FileNameTemplate(string mode) =>
        mode switch
        {
            "Title" => "%(title).180B.%(ext)s",
            "Channel - Title" => "%(uploader).70B - %(title).130B.%(ext)s",
            _ => "%(title).160B [%(id)s].%(ext)s"
        };

    private static string ReadString(JsonElement element, string property, string fallback)
    {
        if (!element.TryGetProperty(property, out var value)
            || value.ValueKind != JsonValueKind.String)
        {
            return fallback;
        }

        return value.GetString() ?? fallback;
    }

    private static double? TryReadPercent(string line)
    {
        var match = PercentRegex().Match(line);
        if (!match.Success)
        {
            return null;
        }

        return double.TryParse(
            match.Groups[1].Value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var value)
                ? Math.Clamp(value, 0, 100)
                : null;
    }

    private static void Kill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
        }
    }

    private static string CleanLine(string value) =>
        value.Replace("[download]", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();

    private static string CleanError(string value)
    {
        var lines = value
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .TakeLast(4);

        return string.Join(Environment.NewLine, lines);
    }

    [GeneratedRegex(@"(\d+(?:\.\d+)?)%")]
    private static partial Regex PercentRegex();
}

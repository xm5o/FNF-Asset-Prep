using System.Diagnostics;

namespace FNFAssetPrep.Services;

public sealed class AudioConverterService
{
    private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".ogg", ".wav", ".mp3", ".flac", ".m4a", ".aac", ".opus", ".wma"
    };

    public static bool IsSupported(string path) =>
        AudioExtensions.Contains(Path.GetExtension(path));

    public async Task<string> ConvertAsync(
        string sourcePath,
        string outputDirectory,
        string format,
        string quality,
        CancellationToken cancellationToken = default)
    {
        if (!AssetProcessor.HasAudioEngine)
        {
            throw new FileNotFoundException("FFmpeg is missing from the tools folder.", AssetProcessor.FfmpegPath);
        }

        Directory.CreateDirectory(outputDirectory);

        var outputPath = OutputPathHelper.GetUniquePath(
            outputDirectory,
            sourcePath,
            OutputExtension(format));

        var startInfo = new ProcessStartInfo
        {
            FileName = AssetProcessor.FfmpegPath,
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

        foreach (var argument in CodecArguments(format, quality))
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.ArgumentList.Add(outputPath);

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        using var registration = cancellationToken.Register(() =>
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
        });

        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        var error = await errorTask;

        if (process.ExitCode != 0)
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(error)
                    ? "FFmpeg could not convert this file."
                    : error.Trim());
        }

        return outputPath;
    }

    private static string OutputExtension(string format) =>
        format.ToUpperInvariant() switch
        {
            "OGG" => ".ogg",
            "MP3" => ".mp3",
            "WAV" => ".wav",
            "FLAC" => ".flac",
            "M4A" => ".m4a",
            "OPUS" => ".opus",
            "AAC" => ".aac",
            "ALAC" => ".m4a",
            _ => ".ogg"
        };

    private static IEnumerable<string> CodecArguments(string format, string quality)
    {
        var q = quality switch
        {
            "Best" => "0",
            "High" => "2",
            "Balanced" => "4",
            "Small" => "6",
            _ => "2"
        };

        var bitrate = quality switch
        {
            "Best" => "256k",
            "High" => "192k",
            "Balanced" => "160k",
            "Small" => "112k",
            _ => "192k"
        };

        return format.ToUpperInvariant() switch
        {
            "OGG" => new[] { "-c:a", "libvorbis", "-q:a", q },
            "MP3" => new[] { "-c:a", "libmp3lame", "-q:a", q },
            "WAV" => new[] { "-c:a", "pcm_s16le" },
            "FLAC" => new[] { "-c:a", "flac" },
            "M4A" => new[] { "-c:a", "aac", "-b:a", bitrate },
            "OPUS" => new[] { "-c:a", "libopus", "-b:a", bitrate },
            "AAC" => new[] { "-c:a", "aac", "-b:a", bitrate },
            "ALAC" => new[] { "-c:a", "alac" },
            _ => new[] { "-c:a", "libvorbis", "-q:a", q }
        };
    }
}

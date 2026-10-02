using System.IO;

namespace FNFAssetPrep.Services;

public static class OutputPathHelper
{
    public static string GetUniquePath(string outputDirectory, string sourcePath, string targetExtension)
    {
        var baseName = Path.GetFileNameWithoutExtension(sourcePath);
        var candidate = Path.Combine(outputDirectory, baseName + targetExtension);
        var number = 2;

        while (File.Exists(candidate))
        {
            candidate = Path.Combine(outputDirectory, baseName + "_" + number + targetExtension);
            number++;
        }

        return candidate;
    }
}

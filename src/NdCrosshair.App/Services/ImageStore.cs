using System.IO;
using System.Security.Cryptography;
using System.Windows.Media.Imaging;
using NdCrosshair.App.Localization;
using NdCrosshair.Core;

namespace NdCrosshair.App.Services;

internal sealed record ImageImportResult(string? FileName, string? Error);

internal static class ImageStore
{
    public const long MaxFileBytes = 8 * 1024 * 1024;
    public const int MaxPixels = 2048;
    public const int MaxFrames = 300;

    private static readonly string[] SupportedExtensions = [".png", ".gif", ".jpg", ".jpeg", ".bmp"];

    public static string DirectoryPath => Path.Combine(Path.GetDirectoryName(AppIdentity.ConfigFilePath)!, "images");

    public static string? PathFor(string fileName) =>
        ImageLayer.IsSafeFileName(fileName) ? Path.Combine(DirectoryPath, fileName) : null;

    public static ImageImportResult Import(string sourcePath)
    {
        var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        if (!SupportedExtensions.Contains(extension))
        {
            return new ImageImportResult(null, Loc.T("ImageErrorFormat"));
        }

        byte[] bytes;
        try
        {
            var info = new FileInfo(sourcePath);
            if (info.Length > MaxFileBytes)
            {
                return new ImageImportResult(null, Loc.T("ImageErrorTooLarge"));
            }

            bytes = File.ReadAllBytes(sourcePath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new ImageImportResult(null, Loc.Format("ImageErrorRead", exception.Message));
        }

        if (!IsValidImage(bytes))
        {
            return new ImageImportResult(null, Loc.T("ImageErrorFormat"));
        }

        var fileName = Convert.ToHexString(SHA256.HashData(bytes))[..16].ToLowerInvariant() + extension;
        try
        {
            Directory.CreateDirectory(DirectoryPath);
            var target = Path.Combine(DirectoryPath, fileName);
            if (!File.Exists(target))
            {
                var temp = target + ".tmp";
                File.WriteAllBytes(temp, bytes);
                File.Move(temp, target, overwrite: true);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new ImageImportResult(null, Loc.Format("ImageErrorRead", exception.Message));
        }

        return new ImageImportResult(fileName, null);
    }

    public static bool IsValidImage(byte[] bytes)
    {
        try
        {
            using var stream = new MemoryStream(bytes);
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            return decoder.Frames.Count is > 0 and <= MaxFrames
                && decoder.Frames.All(frame => frame.PixelWidth is > 0 and <= MaxPixels && frame.PixelHeight is > 0 and <= MaxPixels);
        }
        catch (Exception exception) when (exception is NotSupportedException or FileFormatException or ArgumentException or InvalidOperationException or OverflowException or System.Runtime.InteropServices.COMException)
        {
            return false;
        }
    }
}

using System.Security.Cryptography;

namespace TwoHumpHelpers;

public static class FileInfoHashExtensions
{
    public static string CreateMd5Hash(this FileInfo file)
    {
        using var md5 = MD5.Create();
        using var fileStream = file.Open(FileMode.Open, FileAccess.Read);
        var hashValue = md5.ComputeHash(fileStream);
        var result = BitConverter.ToString(hashValue).Replace("-", "").ToLowerInvariant();
        return result;
    }

    public static string CreateMd5Hash(this string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        return fileInfo.CreateMd5Hash();
    }
}
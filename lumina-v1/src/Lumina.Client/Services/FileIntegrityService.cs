using System.Security.Cryptography;

namespace Lumina.Client.Services;

public static class FileIntegrityService
{
    public static string TemporaryPath(string finalPath) => finalPath + ".part";

    public static async Task<bool> VerifyAsync(
        string path,
        string? expectedSha512,
        string? expectedSha1,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(expectedSha512) && string.IsNullOrWhiteSpace(expectedSha1))
            return true;

        await using var stream = File.OpenRead(path);
        if (!string.IsNullOrWhiteSpace(expectedSha512))
        {
            var actual = Convert.ToHexString(await SHA512.HashDataAsync(stream, cancellationToken)).ToLowerInvariant();
            return actual.Equals(expectedSha512.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        var sha1 = Convert.ToHexString(await SHA1.HashDataAsync(stream, cancellationToken)).ToLowerInvariant();
        return sha1.Equals(expectedSha1!.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}

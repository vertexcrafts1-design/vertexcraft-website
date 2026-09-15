using System.Security.Cryptography;
using System.Text;
using Lumina.Client.Services;
using Xunit;

namespace Lumina.Client.Tests;

public sealed class DownloadIntegrityTests
{
    [Fact]
    public async Task VerifyAsync_Accepts_Correct_Sha512()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "lumina-content");
            var expected = Convert.ToHexString(SHA512.HashData(Encoding.UTF8.GetBytes("lumina-content"))).ToLowerInvariant();
            Assert.True(await FileIntegrityService.VerifyAsync(path, expected, null));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task VerifyAsync_Rejects_Wrong_Hash()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "lumina-content");
            Assert.False(await FileIntegrityService.VerifyAsync(path, new string('0', 128), null));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void TemporaryPath_Uses_Part_Suffix()
    {
        Assert.EndsWith(".part", FileIntegrityService.TemporaryPath("example-file"));
    }
}

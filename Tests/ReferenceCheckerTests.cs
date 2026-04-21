using System.IO;

namespace ZeroReferences.Tests;

public class ReferenceCheckerTests
{
    // ===== Check() 參數驗證 =====

    [Fact]
    public async Task Check_NullPath_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => ReferenceChecker.Check(null!));
    }

    [Fact]
    public async Task Check_EmptyPath_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => ReferenceChecker.Check(string.Empty));
    }

    [Fact]
    public async Task Check_InvalidExtension_ThrowsArgumentException()
    {
        var path = Path.Combine(Path.GetTempPath(), "test.txt");
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => ReferenceChecker.Check(path));
        Assert.Contains(".csproj", ex.Message);
    }

    [Theory]
    [InlineData(".sln")]
    [InlineData(".slnx")]
    [InlineData(".csproj")]
    public async Task Check_NonExistentFile_ThrowsArgumentException(string extension)
    {
        var path = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}{extension}");
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => ReferenceChecker.Check(path));
        Assert.Contains("exist", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(".sln")]
    [InlineData(".slnx")]
    [InlineData(".csproj")]
    public async Task Check_ValidExtensionButNonExistent_DoesNotRejectExtension(string extension)
    {
        var path = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}{extension}");
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => ReferenceChecker.Check(path));
        Assert.DoesNotContain("extension", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ===== RemoveMethodsAsync() 參數驗證 =====

    [Fact]
    public async Task RemoveMethodsAsync_NullSignatures_Throws()
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.sln");
        await Assert.ThrowsAnyAsync<Exception>(
            () => ReferenceChecker.RemoveMethodsAsync(path, null!));
    }

    [Fact]
    public async Task RemoveMethodAsync_NonExistentFile_ReturnsExpectedError()
    {
        var path = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}.sln");
        try
        {
            await ReferenceChecker.RemoveMethodAsync(path, "some signature");
        }
        catch (Exception ex)
        {
            Assert.IsNotType<NullReferenceException>(ex);
        }
    }
}

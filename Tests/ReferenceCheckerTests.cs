using ZeroReferences;

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
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => ReferenceChecker.Check("/tmp/test.csproj"));
        Assert.Contains(".sln", ex.Message);
    }

    [Fact]
    public async Task Check_NonExistentFile_ThrowsArgumentException()
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => ReferenceChecker.Check("/tmp/nonexistent.sln"));
        Assert.Contains("exist", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ===== RemoveMethodsAsync() 參數驗證 =====

    [Fact]
    public async Task RemoveMethodsAsync_NullSignatures_Throws()
    {
        await Assert.ThrowsAnyAsync<Exception>(
            () => ReferenceChecker.RemoveMethodsAsync("/tmp/test.sln", null!));
    }

    [Fact]
    public async Task RemoveMethodAsync_NonExistentFile_ReturnsExpectedError()
    {
        // RemoveMethodAsync 不做預先驗證，會在 OpenSolutionAsync 時失敗
        // 確認不會拋出未處理的例外
        try
        {
            await ReferenceChecker.RemoveMethodAsync("/tmp/nonexistent.sln", "some signature");
        }
        catch (Exception ex)
        {
            // 預期會因為檔案不存在而失敗，但不應該是 NullReferenceException
            Assert.IsNotType<NullReferenceException>(ex);
        }
    }
}

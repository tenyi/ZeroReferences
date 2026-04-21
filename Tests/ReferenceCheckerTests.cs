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

    // ===== Check() 整合測試 =====

    /// <summary>
    /// 驗證孤兒方法會被正確偵測。
    /// 建立兩個方法：UsedMethod（被 Main 呼叫）與 OrphanMethod（無引用）。
    /// Check() 應該只回傳 OrphanMethod。
    /// </summary>
    [Fact]
    public async Task Check_OrphanMethod_DetectsUnreferencedMethod()
    {
        var code = @"
namespace TestNs {
    public class MyClass {
        public void UsedMethod() { }
        public void OrphanMethod() { }
        public static void Main() { new MyClass().UsedMethod(); }
    }
}";
        var slnPath = await TestSolutionBuilder.CreateSolutionAsync(("MyClass.cs", code));
        try
        {
            var result = await ReferenceChecker.Check(slnPath);
            Assert.Contains(result, r => r.Contains("OrphanMethod"));
            Assert.DoesNotContain(result, r => r.Contains("UsedMethod"));
        }
        finally
        {
            TestSolutionBuilder.Cleanup(slnPath);
        }
    }

    /// <summary>
    /// 驗證有引用的方法不會被標記為孤兒方法。
    /// MethodA 被 MethodB 呼叫，MethodB 被 Main 呼叫，兩者都不應是孤兒方法。
    /// </summary>
    [Fact]
    public async Task Check_ReferencedMethod_NotMarkedAsOrphan()
    {
        var code = @"
namespace TestNs {
    public class MyClass {
        public void MethodA() { }
        public void MethodB() { MethodA(); }
        public static void Main() { new MyClass().MethodB(); }
    }
}";
        var slnPath = await TestSolutionBuilder.CreateSolutionAsync(("MyClass.cs", code));
        try
        {
            var result = await ReferenceChecker.Check(slnPath);
            Assert.DoesNotContain(result, r => r.Contains("MethodA"));
            Assert.DoesNotContain(result, r => r.Contains("MethodB"));
        }
        finally
        {
            TestSolutionBuilder.Cleanup(slnPath);
        }
    }

    /// <summary>
    /// 驗證 Controller 類別中的方法會被排除。
    /// </summary>
    [Fact]
    public async Task Check_ControllerClass_Excluded()
    {
        var code = @"
namespace TestNs {
    public class MyController {
        public void Foo() { }
    }
}";
        var slnPath = await TestSolutionBuilder.CreateSolutionAsync(("MyController.cs", code));
        try
        {
            var result = await ReferenceChecker.Check(slnPath);
            Assert.DoesNotContain(result, r => r.Contains("MyController"));
        }
        finally
        {
            TestSolutionBuilder.Cleanup(slnPath);
        }
    }

    /// <summary>
    /// 驗證 Test 類別中的方法會被排除。
    /// </summary>
    [Fact]
    public async Task Check_TestClass_Excluded()
    {
        var code = @"
namespace TestNs {
    public class MyTest {
        public void TestFoo() { }
    }
}";
        var slnPath = await TestSolutionBuilder.CreateSolutionAsync(("MyTest.cs", code));
        try
        {
            var result = await ReferenceChecker.Check(slnPath);
            Assert.DoesNotContain(result, r => r.Contains("MyTest"));
        }
        finally
        {
            TestSolutionBuilder.Cleanup(slnPath);
        }
    }

    /// <summary>
    /// 驗證刪除單一方法後，該方法不再出現於 Check() 回傳結果中。
    /// </summary>
    [Fact]
    public async Task RemoveMethodAsync_SingleMethod_RemovesFromProject()
    {
        var code = @"
namespace TestNs {
    public class MyClass {
        public void OrphanMethod() { }
    }
}";
        var slnPath = await TestSolutionBuilder.CreateSolutionAsync(("MyClass.cs", code));
        try
        {
            // 第一次檢查：確認孤兒方法存在
            var before = await ReferenceChecker.Check(slnPath);
            Assert.Contains(before, r => r.Contains("OrphanMethod"));

            // 刪除該方法
            var signature = before.First(r => r.Contains("OrphanMethod"));
            var (result, _) = await ReferenceChecker.RemoveMethodAsync(slnPath, signature);
            Assert.Equal(RemoveResult.Success, result);

            // 第二次檢查：確認方法已不存在
            var after = await ReferenceChecker.Check(slnPath);
            Assert.DoesNotContain(after, r => r.Contains("OrphanMethod"));
        }
        finally
        {
            TestSolutionBuilder.Cleanup(slnPath);
        }
    }

    /// <summary>
    /// 驗證刪除具有介面實作的方法時，介面中的方法也會被連帶刪除。
    /// </summary>
    [Fact]
    public async Task RemoveMethodAsync_WithInterfaceImplementation_RemovesInterfaceMethod()
    {
        var code = @"
namespace TestNs {
    public interface IMyInterface {
        void DoSomething();
    }
    public class MyClass : IMyInterface {
        public void DoSomething() { }
    }
}";
        var slnPath = await TestSolutionBuilder.CreateSolutionAsync(("Code.cs", code));
        try
        {
            // 確認孤兒方法存在（實作與介面方法都是孤兒）
            var before = await ReferenceChecker.Check(slnPath);
            Assert.Contains(before, r => r.Contains("DoSomething"));

            // 刪除實作類別中的方法
            var signature = before.First(r => r.Contains("MyClass") && r.Contains("DoSomething"));
            var (result, message) = await ReferenceChecker.RemoveMethodAsync(slnPath, signature);
            Assert.Equal(RemoveResult.Success, result);

            // 第二次檢查：確認兩個 DoSomething 都已刪除
            var after = await ReferenceChecker.Check(slnPath);
            Assert.DoesNotContain(after, r => r.Contains("DoSomething"));
        }
        finally
        {
            TestSolutionBuilder.Cleanup(slnPath);
        }
    }

    /// <summary>
    /// 驗證刪除 base class 的 virtual/abstract 方法時，
    /// derived class 中 override 該方法也會被連帶刪除。
    /// </summary>
    [Fact]
    public async Task RemoveMethodAsync_WithOverride_RemovesOverridingMethod()
    {
        var code = @"
namespace TestNs {
    public abstract class BaseClass {
        public abstract void DoWork();
    }
    public class DerivedClass : BaseClass {
        public override void DoWork() { }
    }
}";
        var slnPath = await TestSolutionBuilder.CreateSolutionAsync(("Code.cs", code));
        try
        {
            var before = await ReferenceChecker.Check(slnPath);
            Assert.Contains(before, r => r.Contains("DoWork"));

            // 刪除 base class 的方法（會一併刪除 override）
            var signature = before.First(r => r.Contains("BaseClass") && r.Contains("DoWork"));
            var (result, _) = await ReferenceChecker.RemoveMethodAsync(slnPath, signature);
            Assert.Equal(RemoveResult.Success, result);

            var after = await ReferenceChecker.Check(slnPath);
            Assert.DoesNotContain(after, r => r.Contains("DoWork"));
        }
        finally
        {
            TestSolutionBuilder.Cleanup(slnPath);
        }
    }

    /// <summary>
    /// 驗證 RemoveMethodsAsync 刪除多個方法時，若有部分簽名找不到，會回傳 Partial。
    /// </summary>
    [Fact]
    public async Task RemoveMethodsAsync_PartialNotFound_ReturnsPartial()
    {
        var code = @"
namespace TestNs {
    public class MyClass {
        public void MethodA() { }
        public void MethodB() { }
    }
}";
        var slnPath = await TestSolutionBuilder.CreateSolutionAsync(("MyClass.cs", code));
        try
        {
            var before = await ReferenceChecker.Check(slnPath);
            var methodA = before.First(r => r.Contains("MethodA"));

            // 只傳一個存在的簽名 + 一個不存在的簽名
            var (result, message) = await ReferenceChecker.RemoveMethodsAsync(
                slnPath, new List<string> { methodA, "NonExistentSignature_XYZ()" });

            Assert.Equal(RemoveResult.Partial, result);
            Assert.Contains("未找到", message);
        }
        finally
        {
            TestSolutionBuilder.Cleanup(slnPath);
        }
    }

    /// <summary>
    /// 驗證 Main 方法不會被視為孤兒方法（應被排除）。
    /// </summary>
    [Fact]
    public async Task Check_MainMethod_Excluded()
    {
        var code = @"
namespace TestNs {
    public class Program {
        public static void Main() { }
    }
}";
        var slnPath = await TestSolutionBuilder.CreateSolutionAsync(("Program.cs", code));
        try
        {
            var result = await ReferenceChecker.Check(slnPath);
            Assert.DoesNotContain(result, r => r.Contains("Main"));
        }
        finally
        {
            TestSolutionBuilder.Cleanup(slnPath);
        }
    }
}

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
            await ReferenceChecker.RemoveMethodAsync(path, new MethodResult("some signature", "", "", -1));
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
            Assert.Contains(result, r => r.Signature.Contains("OrphanMethod"));
            Assert.DoesNotContain(result, r => r.Signature.Contains("UsedMethod"));
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
            Assert.DoesNotContain(result, r => r.Signature.Contains("MethodA"));
            Assert.DoesNotContain(result, r => r.Signature.Contains("MethodB"));
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
            Assert.DoesNotContain(result, r => r.Signature.Contains("MyController"));
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
            Assert.DoesNotContain(result, r => r.Signature.Contains("MyTest"));
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
            Assert.Contains(before, r => r.Signature.Contains("OrphanMethod"));

            // 刪除該方法
            var method = before.First(r => r.Signature.Contains("OrphanMethod"));
            var (result, _) = await ReferenceChecker.RemoveMethodAsync(slnPath, method);
            Assert.Equal(RemoveResult.Success, result);

            // 第二次檢查：確認方法已不存在
            var after = await ReferenceChecker.Check(slnPath);
            Assert.DoesNotContain(after, r => r.Signature.Contains("OrphanMethod"));
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
            Assert.Contains(before, r => r.Signature.Contains("DoSomething"));

            // 刪除實作類別中的方法
            var method = before.First(r => r.Signature.Contains("MyClass") && r.Signature.Contains("DoSomething"));
            var (result, message) = await ReferenceChecker.RemoveMethodAsync(slnPath, method);
            Assert.Equal(RemoveResult.Success, result);

            // 第二次檢查：確認兩個 DoSomething 都已刪除
            var after = await ReferenceChecker.Check(slnPath);
            Assert.DoesNotContain(after, r => r.Signature.Contains("DoSomething"));
        }
        finally
        {
            TestSolutionBuilder.Cleanup(slnPath);
        }
    }

    /// <summary>
    /// 驗證不同專案中完整簽名相同的方法不會被一起刪除。
    /// </summary>
    [Fact]
    public async Task RemoveMethodAsync_SameSignatureInDifferentProjects_RemovesOnlySelectedDeclaration()
    {
        var code = @"
namespace DuplicateNamespace {
    public class DuplicateType {
        public void Orphan() { }
    }
}";
        var slnPath = await TestSolutionBuilder.CreateMultiProjectSolutionAsync(new[]
        {
            ("ProjectA", "Code.cs", code),
            ("ProjectB", "Code.cs", code)
        });
        try
        {
            var before = await ReferenceChecker.Check(slnPath);
            var duplicateResults = before
                .Where(r => r.Signature.Contains("DuplicateType.Orphan"))
                .ToList();
            Assert.Equal(2, duplicateResults.Count);
            Assert.NotEqual(duplicateResults[0].DisplayName, duplicateResults[1].DisplayName);
            var selected = before.Single(r =>
                r.Signature.Contains("DuplicateType.Orphan") &&
                r.ProjectPath.EndsWith("ProjectA.csproj", StringComparison.OrdinalIgnoreCase));

            var (result, _) = await ReferenceChecker.RemoveMethodAsync(slnPath, selected);

            Assert.Equal(RemoveResult.Success, result);
            var projectA = Path.Combine(Path.GetDirectoryName(slnPath)!, "ProjectA", "Code.cs");
            var projectB = Path.Combine(Path.GetDirectoryName(slnPath)!, "ProjectB", "Code.cs");
            Assert.DoesNotContain("Orphan", await File.ReadAllTextAsync(projectA));
            Assert.Contains("Orphan", await File.ReadAllTextAsync(projectB));
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
            Assert.Contains(before, r => r.Signature.Contains("DoWork"));

            // 刪除 base class 的方法（會一併刪除 override）
            var method = before.First(r => r.Signature.Contains("BaseClass") && r.Signature.Contains("DoWork"));
            var (result, _) = await ReferenceChecker.RemoveMethodAsync(slnPath, method);
            Assert.Equal(RemoveResult.Success, result);

            var after = await ReferenceChecker.Check(slnPath);
            Assert.DoesNotContain(after, r => r.Signature.Contains("DoWork"));
        }
        finally
        {
            TestSolutionBuilder.Cleanup(slnPath);
        }
    }

    /// <summary>
    /// 驗證衍生類別中同名但非介面實作的 helper 不會被連帶刪除。
    /// </summary>
    [Fact]
    public async Task RemoveMethodAsync_DerivedPrivateHelperIsNotInterfaceImplementation()
    {
        var code = @"
namespace InterfaceRemoval {
    public interface IWorker {
        void Execute();
    }
    public class BaseWorker : IWorker {
        public virtual void Execute() { }
    }
    public class DerivedWorker : BaseWorker {
        private void Execute() { }
        public void KeepHelperAlive() {
            Execute();
        }
    }
}";
        var slnPath = await TestSolutionBuilder.CreateSolutionAsync(("Code.cs", code));
        try
        {
            var before = await ReferenceChecker.Check(slnPath);
            var method = before.First(r => r.Signature.Contains("BaseWorker") && r.Signature.Contains("Execute"));

            var (result, _) = await ReferenceChecker.RemoveMethodAsync(slnPath, method);

            Assert.Equal(RemoveResult.Success, result);
            var sourcePath = Path.Combine(Path.GetDirectoryName(slnPath)!, "Code.cs");
            var source = await File.ReadAllTextAsync(sourcePath);
            Assert.Contains("private void Execute", source);
            Assert.Contains("KeepHelperAlive", source);
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
            var methodA = before.First(r => r.Signature.Contains("MethodA"));

            // 只傳一個存在的簽名 + 一個不存在的簽名
            var (result, message) = await ReferenceChecker.RemoveMethodsAsync(
                slnPath, new List<MethodResult>
                {
                    methodA,
                    new MethodResult("NonExistentSignature_XYZ()", "", "", -1)
                });

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
            Assert.DoesNotContain(result, r => r.Signature.Contains("Main"));
        }
        finally
        {
            TestSolutionBuilder.Cleanup(slnPath);
        }
    }
}

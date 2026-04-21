using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;

namespace ZeroReferences.Tests;

/// <summary>
/// 整合測試輔助類別，用於建立臨時測試解決方案。
/// </summary>
internal static class TestSolutionBuilder
{
    /// <summary>
    /// 建立包含測試程式碼的臨時解決方案檔案。
    /// </summary>
    /// <param name="files">檔案名稱到程式碼內容的對應。</param>
    /// <returns>臨時解決方案的路徑。</returns>
    public static async Task<string> CreateSolutionAsync(params (string fileName, string code)[] files)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ZeroRefsTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        // 建立一個簡單的 csproj
        var csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.CodeAnalysis"" Version=""5.3.0"" />
  </ItemGroup>
</Project>";

        var projectFileName = "TestProject.csproj";
        await File.WriteAllTextAsync(Path.Combine(tempDir, projectFileName), csprojContent);

        // 建立各個原始碼檔案
        foreach (var (fileName, code) in files)
        {
            await File.WriteAllTextAsync(Path.Combine(tempDir, fileName), code);
        }

        // 建立 sln
        var slnContent = $@"Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
Project(""{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}"") = ""TestProject"", ""{projectFileName}"", ""{{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}}""
EndProject
Global
    GlobalSection(SolutionConfigurationPlatforms) = preSolution
        Debug|Any CPU = Debug|Any CPU
    EndGlobalSection
    GlobalSection(ProjectConfigurationPlatforms) = preSolution
        {{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
    EndGlobalSection
    GlobalSection(SolutionProperties) = preSolution
        SolutionDir = {tempDir}
    EndGlobalSection
EndGlobal
";
        var slnPath = Path.Combine(tempDir, "TestSolution.sln");
        await File.WriteAllTextAsync(slnPath, slnContent);

        return slnPath;
    }

    /// <summary>
    /// 建立臨時的單一專案檔案（無需 sln）。</summary>
    public static async Task<string> CreateProjectAsync(params (string fileName, string code)[] files)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ZeroRefsTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        var csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.CodeAnalysis"" Version=""5.3.0"" />
  </ItemGroup>
</Project>";

        var projectFileName = "TestProject.csproj";
        await File.WriteAllTextAsync(Path.Combine(tempDir, projectFileName), csprojContent);

        foreach (var (fileName, code) in files)
        {
            await File.WriteAllTextAsync(Path.Combine(tempDir, fileName), code);
        }

        return Path.Combine(tempDir, projectFileName);
    }

    /// <summary>
    /// 刪除臨時解決方案目錄。</summary>
    public static void Cleanup(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (dir != null && Directory.Exists(dir))
        {
            try
            {
                Directory.Delete(dir, true);
            }
            catch
            {
                // 忽略刪除失敗（檔案可能仍被鎖定）
            }
        }
    }
}

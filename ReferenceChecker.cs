using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;

namespace ZeroReferences
{
    /// <summary>
    /// 提供檢查 .NET 解決方案中未參照方法之功能的靜態類別。
    /// 使用 Microsoft.CodeAnalysis (Roslyn) 組合式 API 分析解決方案，
    /// 找出所有定義為 public 但在整個解決方案中沒有被引用的方法（孤兒方法）。
    /// </summary>
    public static class ReferenceChecker
    {
        /// <summary>
        /// 分析指定的解決方案檔案，找出所有未被引用的 public 方法。
        /// </summary>
        /// <param name="solutionPath">.sln/.slnx 檔案的完整路徑。</param>
        /// <returns>回傳包含所有未參照方法全限定名稱的清單。</returns>
        /// <exception cref="ArgumentException">當路徑為空、格式不正確或檔案不存在時拋出。</exception>
        public static async Task<List<string>> Check(string solutionPath)
        {
            // 存放未參照方法的清單
            List<string> list = new List<string>();

            // ===== 參數驗證 =====
            // 檢查路徑是否為空
            if (string.IsNullOrEmpty(solutionPath))
            {
                throw new ArgumentException("Solution path cannot be null or empty.");
            }
            // 檢查副檔名是否為 .sln
            string ext = Path.GetExtension(solutionPath).ToLower();
            if (ext != ".sln" && ext != ".slnx")
            {
                throw new ArgumentException("Solution path must have a .sln / .slnx extension.");
            }
            // 檢查檔案是否存在
            if (!File.Exists(solutionPath))
            {
                throw new ArgumentException("Solution file does not exist.");
            }

            // ===== 建立工作區並開啟解決方案 =====
            // 使用 using 確保工作區資源正確釋放，避免檔案鎖定與記憶體洩漏
            using var workspace = MSBuildWorkspace.Create();
            var solution = await workspace.OpenSolutionAsync(solutionPath);

            // ===== 遍歷解決方案中的每個專案 =====
            foreach (var project in solution.Projects)
            {
                // 取得專案的編譯物件（Compilation），用於語意分析
                var compilation = await project.GetCompilationAsync();
                if (compilation == null) { throw new ArgumentException("並沒有任何 compilation"); }

                // 存放此專案中找到的所有方法宣告
                var methods = new List<MethodDeclarationSyntax>();

                // ===== 遍歷專案中的每個文件 =====
                foreach (var document in project.Documents)
                {
                    // 取得文件的語法樹
                    var syntaxTree = await document.GetSyntaxTreeAsync();
                    if (syntaxTree != null)
                    {
                        // 取得語法樹的根節點
                        var root = await syntaxTree.GetRootAsync();
                        // 找出所有方法宣告並加入清單
                        methods.AddRange(root.DescendantNodes().OfType<MethodDeclarationSyntax>());
                    }
                }

                // ===== 檢查每個方法的引用情形 =====
                foreach (var method in methods)
                {
                    // 取得方法對應的語意模型
                    var model = compilation.GetSemanticModel(method.SyntaxTree);
                    // 取得方法符號（IMethodSymbol），用於查詢引用
                    var symbol = model.GetDeclaredSymbol(method) as IMethodSymbol;
                    if (symbol == null) { throw new ArgumentException("並沒有任何 symbol"); }

                    // 只檢查 public 方法（排除 private、protected、internal 等）
                    if (symbol.DeclaredAccessibility == Microsoft.CodeAnalysis.Accessibility.Public)
                    {
                        // 使用 ToDisplayString 擴充方法取得方法的完整簽名
                        // 預設格式包含：類別名稱、方法名稱、參數型別、返回類型
                        string name = symbol.ToDisplayString();

                        // 跳過 Controller 類別中的方法（通常是 MVC/Web API 的控制器方法）
                        if (name.Contains("Controller"))
                        {
                            continue;
                        }
                        // 跳過 Test 相關類別中的方法（測試方法的引用不計入）
                        if (name.Contains("Test"))
                        {
                            continue;
                        }

                        // 使用 SymbolFinder 在整個解決方案中查詢此方法的所有引用位置
                        var references = await SymbolFinder.FindReferencesAsync(symbol, solution);
                        var referenceCount = references.Sum(r => r.Locations.Count());

                        // 引用次數為 0，表示此方法是孤兒方法
                        if (referenceCount == 0)
                        {
                            list.Add(name);
                            Console.WriteLine($"Method '{name}' has no references.");
                        }
                    }
                }
            }

            // 回傳所有未參照方法的清單
            return list;
        }
    }
}

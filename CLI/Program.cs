using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;

namespace ZeroReferences
{
    /// <summary>
    /// ZeroReferences 命令列工具的進入點類別。
    /// 使用 Roslyn 分析指定的 .sln 檔案，找出未被引用的 public 方法。
    /// </summary>
    public class Program
    {
        /// <summary>
        /// 應用程式的主進入點。載入指定 solution，遍歷所有專案中的 public 方法，
        /// 計算引用次數，並輸出引用次數為零且不屬於 Controller 類別的方法。
        /// </summary>
        /// <param name="args">命令列參數（目前未使用）。</param>
        static async Task Main(string[] args)
        {
            // 使用 using 確保工作區資源正確釋放，避免檔案鎖定與記憶體洩漏
            using var workspace = MSBuildWorkspace.Create();

            // 開啟指定的 solution 檔案（路徑為硬編碼，執行前需確認）
            var solution = await workspace.OpenSolutionAsync(@"C:\Git\Attendance\Sinotech.Mis.HR.Attendance.AttendanceCard\AttendanceCard.sln");

            // 儲存每個方法的全名與其引用次數
            Dictionary<string, int> methodReferenceCounts = new Dictionary<string, int>();

            // 遍歷 solution 中的每個專案
            foreach (var project in solution.Projects)
            {
                // 取得專案的編譯結果，用於語意分析
                var compilation = await project.GetCompilationAsync();
                if (compilation == null) { return; }

                // 收集此專案中所有方法宣告的語法節點
                var methods = new List<MethodDeclarationSyntax>();

                // 遍歷專案中的每份文件，擷取所有方法宣告
                foreach (var document in project.Documents)
                {
                    var syntaxTree = await document.GetSyntaxTreeAsync();
                    if (syntaxTree != null)
                    {
                        var root = await syntaxTree.GetRootAsync();
                        // 從語法樹中找出所有方法宣告節點
                        methods.AddRange(root.DescendantNodes().OfType<MethodDeclarationSyntax>());
                    }
                }

                // 針對每個方法宣告，透過語意模型計算引用次數
                foreach (var method in methods)
                {
                    // 取得語意模型以進行符號分析
                    var model = compilation.GetSemanticModel(method.SyntaxTree);
                    // 將語法節點轉換為符號，以便查詢引用
                    var symbol = model.GetDeclaredSymbol(method) as IMethodSymbol;

                    // 只處理 public 存取層級的方法
                    if (symbol != null && symbol.DeclaredAccessibility == Accessibility.Public)
                    {
                        // 使用 Roslyn 的符號查找器，非同步搜尋整個 solution 中的引用
                        var references = await SymbolFinder.FindReferencesAsync(symbol, solution);

                        // 計算總引用次數（一個方法可能在多處被引用）
                        var referenceCount = references.Sum(r => r.Locations.Count());

                        // 以「類別名稱.方法名稱」作為識別鍵
                        string name = $"{symbol.ContainingType.Name}.{symbol.Name}";

                        // 使用 TryGetValue 避免重複雜湊查找
                        if (methodReferenceCounts.TryGetValue(name, out int existingCount))
                        {
                            methodReferenceCounts[name] = existingCount + referenceCount;
                        }
                        else
                        {
                            methodReferenceCounts[name] = referenceCount;
                        }
                    }
                }
            }

            // 輸出所有引用次數為零且非 Controller 的方法
            foreach (var item in methodReferenceCounts)
            {
                // 排除 Controller 類別的方法（通常由框架呼叫，不需要直接引用）
                if (item.Value == 0 && !item.Key.Contains("Controller"))
                {
                    Console.WriteLine($"Method '{item.Key}' has no references.");
                }
            }
        }
    }
}

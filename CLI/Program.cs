namespace ZeroReferences;

/// <summary>
/// ZeroReferences 命令列工具的進入點類別。
/// 使用 Roslyn 分析指定的 .sln/.slnx/.csproj 檔案，找出未被引用的 public / private / protected 方法。
/// </summary>
public class Program
{
    /// <summary>
    /// 應用程式的主進入點。載入指定 solution，遍歷所有專案中的 public / private / protected 方法，
    /// 計算引用次數，並輸出引用次數為零且不屬於 Controller / Test 類別的方法。
    /// </summary>
    /// <param name="args">命令列參數（目前未使用）。</param>
    /// <returns>成功回傳 0；參數錯誤或分析失敗回傳 1。</returns>
    static async Task<int> Main(string[] args)
    {
        // 檢查命令列參數
        if (args.Length == 0)
        {
            Console.Error.WriteLine("用法: ZeroReferences <solution_or_project_path>");
            Console.Error.WriteLine("例如: ZeroReferences C:\\Path\\To\\Solution.sln");
            Console.Error.WriteLine("      ZeroReferences C:\\Path\\To\\Project.csproj");
            return 1;
        }

        string solutionPath = args[0];

        try
        {
            // 使用 Core 程式庫的 Check 方法进行分析
            var unusedMethods = await ReferenceChecker.Check(solutionPath);

            // 輸出所有未參照方法
            Console.WriteLine($"\n找到 {unusedMethods.Count} 個未參照方法:\n");
            foreach (var method in unusedMethods)
            {
                Console.WriteLine($"  {method.DisplayName}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"錯誤: {ex.Message}");
            if (ex.InnerException is not null)
                Console.Error.WriteLine($"  內部例外: {ex.InnerException.Message}");
            return 1;
        }
    }
}

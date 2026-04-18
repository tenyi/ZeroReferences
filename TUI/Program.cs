using System.Text;
using ZeroReferences;

namespace ZeroReferences.TUI;

/// <summary>
/// ZeroReferences 終端機使用者介面（TUI）應用程式。
/// 使用純 Console API 實作，無外部 TUI 框架依賴。
/// </summary>
public class Program
{
    // ===== ANSI Escape Codes =====
    private const string ESC = "\x1b";
    private const string CLEAR_SCREEN = ESC + "[2J";
    private const string CLEAR_LINE = ESC + "[2K";
    private const string HIDE_CURSOR = ESC + "[?25l";
    private const string SHOW_CURSOR = ESC + "[?25h";
    private const string MOVE_CURSOR = ESC + "[{0};{1}H";
    private const string BOLD = ESC + "[1m";
    private const string RED = ESC + "[31m";
    private const string GREEN = ESC + "[32m";
    private const string YELLOW = ESC + "[33m";
    private const string CYAN = ESC + "[36m";
    private const string RESET = ESC + "[0m";

    /// <summary>儲存所有檢查結果。</summary>
    private static List<string> _allResults = new();

    /// <summary>目前篩選模式：0=全部, 1=public, 2=private, 3=protected。</summary>
    private static int _currentFilter = 0;

    /// <summary>已勾選的項目索引集合。</summary>
    private static HashSet<int> _selectedIndices = new();

    /// <summary>游標目前所在的索引位置（獨立於勾選狀態）。</summary>
    private static int _cursorIndex = 0;

    /// <summary>目前解決方案路徑。</summary>
    private static string _solutionPath = string.Empty;

    /// <summary>目前資料來源（經過篩選後）。</summary>
    private static List<string> _filteredResults = new();

    /// <summary>
    /// 應用程式主進入點。
    /// </summary>
    public static async Task Main(string[] args)
    {
        Console.Write(CLEAR_SCREEN);
        Console.Write(HIDE_CURSOR);

        try
        {
            if (args.Length > 0)
            {
                _solutionPath = args[0];
            }

            await RunAsync();
        }
        finally
        {
            Console.Write(SHOW_CURSOR);
            Console.WriteLine();
        }
    }

    /// <summary>
    /// 執行 TUI 主迴圈。
    /// </summary>
    private static async Task RunAsync()
    {
        bool running = true;
        bool inputMode = false;
        StringBuilder pathInput = new();

        // 如果命令列有帶路徑，自動檢查
        if (!string.IsNullOrEmpty(_solutionPath) && System.IO.File.Exists(_solutionPath))
        {
            await CheckProjectAsync();
        }

        while (running)
        {
            if (inputMode)
            {
                DrawInputMode(pathInput.ToString());
                var key = Console.ReadKey(true);

                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        if (pathInput.Length > 0)
                        {
                            _solutionPath = pathInput.ToString();
                            inputMode = false;
                            await CheckProjectAsync();
                        }
                        break;

                    case ConsoleKey.Escape:
                        inputMode = false;
                        pathInput.Clear();
                        break;

                    case ConsoleKey.Backspace:
                        if (pathInput.Length > 0)
                            pathInput.Length--;
                        break;

                    default:
                        if (!char.IsControl(key.KeyChar))
                            pathInput.Append(key.KeyChar);
                        break;
                }
            }
            else
            {
                DrawScreen();
                var key = Console.ReadKey(true);

                switch (key.Key)
                {
                    // Enter：無路徑→輸入模式；有勾選→刪除；有結果→重新檢查
                    case ConsoleKey.Enter:
                        if (string.IsNullOrEmpty(_solutionPath))
                        {
                            inputMode = true;
                            pathInput.Clear();
                        }
                        else if (_selectedIndices.Count > 0)
                        {
                            await DeleteSelectedAsync();
                        }
                        else
                        {
                            await CheckProjectAsync();
                        }
                        break;

                    case ConsoleKey.Tab:
                    case ConsoleKey.Spacebar:
                        ToggleSelection();
                        break;

                    case ConsoleKey.UpArrow:
                        MoveCursor(-1);
                        break;

                    case ConsoleKey.DownArrow:
                        MoveCursor(1);
                        break;

                    case ConsoleKey.D1:
                    case ConsoleKey.NumPad1:
                        SetFilter(0);
                        break;

                    case ConsoleKey.D2:
                    case ConsoleKey.NumPad2:
                        SetFilter(1);
                        break;

                    case ConsoleKey.D3:
                    case ConsoleKey.NumPad3:
                        SetFilter(2);
                        break;

                    case ConsoleKey.D4:
                    case ConsoleKey.NumPad4:
                        SetFilter(3);
                        break;

                    // E 或 R：E=刪除選取，R=重新檢查
                    case ConsoleKey.E:
                        if (_selectedIndices.Count > 0)
                            await DeleteSelectedAsync();
                        break;

                    case ConsoleKey.R:
                        if (!string.IsNullOrEmpty(_solutionPath))
                            await CheckProjectAsync();
                        break;

                    // I：重新輸入路徑
                    case ConsoleKey.I:
                        inputMode = true;
                        pathInput.Clear();
                        break;

                    case ConsoleKey.Q:
                        running = false;
                        break;
                }
            }
        }
    }

    // ===== 繪製方法 =====

    /// <summary>
    /// 繪製路徑輸入模式。
    /// </summary>
    private static void DrawInputMode(string currentInput)
    {
        Console.Write(CLEAR_SCREEN);
        Console.Write(string.Format(MOVE_CURSOR, 1, 1));

        Console.Write($"{BOLD}{CYAN}╔═══════════════════════════════════════════════════════════════════════════╗{RESET}\n");
        Console.Write($"{BOLD}{CYAN}║         ZeroReferences TUI - 輸入 Solution 路徑                         ║{RESET}\n");
        Console.Write($"{BOLD}{CYAN}╚═══════════════════════════════════════════════════════════════════════════╝{RESET}\n\n");

        Console.Write($"  {BOLD}輸入 Solution 路徑 (.sln 或 .slnx):{RESET}\n\n");
        Console.Write($"  {YELLOW}> {RESET}{currentInput}");
        Console.Write(new string(' ', Math.Max(0, 80 - currentInput.Length)));
        Console.Write("\n\n");
        Console.WriteLine("  ╭─────────────────────────────────────────────────────────────────────╮");
        Console.WriteLine("  │ [Enter] 確認   [Esc] 取消   [Backspace] 刪除                        │");
        Console.WriteLine("  ╰─────────────────────────────────────────────────────────────────────╯");

        Console.Write(string.Format(MOVE_CURSOR, 5, 5 + currentInput.Length));
    }

    /// <summary>
    /// 繪製主畫面。
    /// </summary>
    private static void DrawScreen()
    {
        Console.Write(CLEAR_SCREEN);
        Console.Write(string.Format(MOVE_CURSOR, 1, 1));

        // 標題
        Console.Write($"{BOLD}{CYAN}╔═══════════════════════════════════════════════════════════════════════════╗{RESET}\n");
        Console.Write($"{BOLD}{CYAN}║         ZeroReferences TUI - .NET 孤兒方法檢查工具                      ║{RESET}\n");
        Console.Write($"{BOLD}{CYAN}╚═══════════════════════════════════════════════════════════════════════════╝{RESET}\n\n");

        // Solution 路徑
        Console.Write($"  {BOLD}Solution 路徑:{RESET} ");
        if (string.IsNullOrEmpty(_solutionPath))
            Console.Write($"{YELLOW}(未設定){RESET}");
        else
            Console.Write(_solutionPath);
        Console.Write("\n\n");

        // 操作說明
        Console.WriteLine("  ╭──────────────────────────────────────────────────────────────────────────╮");
        Console.WriteLine("  │ [1-4] 篩選  [↑↓] 移動  [Tab/空白鍵] 勾選  [Enter/E] 刪除  [Q] 離開   │");
        Console.WriteLine("  │ [R] 重新檢查  [I] 重新輸入路徑                                          │");
        Console.WriteLine("  ╰──────────────────────────────────────────────────────────────────────────╯\n");

        // 篩選狀態
        string filterText = _currentFilter switch
        {
            1 => "Public",
            2 => "Private",
            3 => "Protected",
            _ => "全部"
        };
        Console.Write($"  篩選: {BOLD}{YELLOW}[{filterText}]{RESET}  |  已勾選: {BOLD}{GREEN}{_selectedIndices.Count}{RESET} 項目\n\n");

        // 結果列表
        Console.Write($"  {BOLD}未參照方法 ({_filteredResults.Count} 個):{RESET}\n");
        Console.Write("  ┌───────────────────────────────────────────────────────────────────────┐\n");

        if (_filteredResults.Count == 0)
        {
            Console.Write("  │                                                                       │\n");
            Console.Write("  │                       (無結果或尚未檢查)                               │\n");
            Console.Write("  │                                                                       │\n");
        }
        else
        {
            int maxDisplay = Math.Max(Console.WindowHeight - 22, 3);
            int displayEnd = Math.Min(_filteredResults.Count, maxDisplay);

            for (int i = 0; i < displayEnd; i++)
            {
                bool isChecked = _selectedIndices.Contains(i);
                bool isCursor = i == _cursorIndex;

                string checkbox = isChecked ? $"{GREEN}[x]{RESET}" : "[ ]";
                string cursorMark = isCursor ? $"{YELLOW}▸{RESET} " : "  ";

                string line = _filteredResults[i];
                if (line.Length > 66) line = line.Substring(0, 63) + "...";

                string color = "";
                if (line.StartsWith("public ")) color = CYAN;
                else if (line.StartsWith("private ")) color = GREEN;
                else if (line.StartsWith("protected ")) color = YELLOW;

                Console.Write($"  │ {cursorMark}{checkbox} {color}{line}{RESET}");
                int padding = 72 - line.Length;
                if (padding > 0) Console.Write(new string(' ', padding));
                Console.Write(" │\n");
            }
        }

        Console.Write("  └───────────────────────────────────────────────────────────────────────┘\n\n");

        // 底部提示
        if (string.IsNullOrEmpty(_solutionPath))
        {
            Console.Write($"  {YELLOW}按 [Enter] 或 [I] 輸入 Solution 路徑{RESET}");
        }
        else if (_selectedIndices.Count > 0)
        {
            Console.Write($"  {GREEN}已勾選 {_selectedIndices.Count} 個方法，按 [Enter] 或 [E] 刪除{RESET}");
        }
        else if (_filteredResults.Count > 0)
        {
            Console.Write($"  使用 [Tab/空白鍵] 勾選項目，按 [R] 重新檢查");
        }
    }

    // ===== 核心操作方法 =====

    /// <summary>
    /// 移動游標（不影響勾選狀態）。
    /// </summary>
    private static void MoveCursor(int direction)
    {
        if (_filteredResults.Count == 0) return;
        _cursorIndex = Math.Clamp(_cursorIndex + direction, 0, _filteredResults.Count - 1);
    }

    /// <summary>
    /// 切換目前游標位置的勾選狀態。
    /// </summary>
    private static void ToggleSelection()
    {
        if (_filteredResults.Count == 0) return;

        if (_selectedIndices.Contains(_cursorIndex))
            _selectedIndices.Remove(_cursorIndex);
        else
            _selectedIndices.Add(_cursorIndex);
    }

    /// <summary>
    /// 設定篩選條件並重設游標。
    /// </summary>
    private static void SetFilter(int filter)
    {
        _currentFilter = filter;
        _selectedIndices.Clear();
        _cursorIndex = 0;
        ApplyFilter();
    }

    /// <summary>
    /// 套用篩選條件到結果列表。
    /// </summary>
    private static void ApplyFilter()
    {
        _filteredResults = _allResults.Where(method =>
        {
            return _currentFilter switch
            {
                1 => method.StartsWith("public ", StringComparison.OrdinalIgnoreCase),
                2 => method.StartsWith("private ", StringComparison.OrdinalIgnoreCase),
                3 => method.StartsWith("protected ", StringComparison.OrdinalIgnoreCase),
                _ => true
            };
        }).ToList();
    }

    /// <summary>
    /// 執行專案檢查。
    /// </summary>
    private static async Task CheckProjectAsync()
    {
        if (string.IsNullOrWhiteSpace(_solutionPath))
        {
            ShowMessage($"{RED}錯誤: 請輸入 Solution 路徑{RESET}");
            return;
        }

        string ext = System.IO.Path.GetExtension(_solutionPath).ToLower();
        if (ext != ".sln" && ext != ".slnx")
        {
            ShowMessage($"{RED}錯誤: 檔案必須是 .sln 或 .slnx 格式{RESET}");
            return;
        }

        if (!System.IO.File.Exists(_solutionPath))
        {
            ShowMessage($"{RED}錯誤: 檔案不存在{RESET}");
            return;
        }

        // 清空舊結果
        _allResults.Clear();
        _selectedIndices.Clear();
        _cursorIndex = 0;
        _filteredResults.Clear();

        // 顯示進度
        DrawScreen();
        Console.Write(string.Format(MOVE_CURSOR, Console.WindowHeight - 2, 1));
        Console.Write($"{CYAN}  檢查中，請稍候...{RESET}");

        try
        {
            var results = await ReferenceChecker.Check(_solutionPath);
            _allResults.AddRange(results);
            ApplyFilter();

            ShowMessage($"{GREEN}  檢查完成，找到 {_allResults.Count} 個未參照方法。按任意鍵繼續...{RESET}");
        }
        catch (Exception ex)
        {
            ShowMessage($"{RED}  錯誤: {ex.Message}  按任意鍵繼續...{RESET}");
        }
    }

    /// <summary>
    /// 刪除已勾選的方法。
    /// </summary>
    private static async Task DeleteSelectedAsync()
    {
        if (_selectedIndices.Count == 0 || string.IsNullOrEmpty(_solutionPath))
            return;

        var signatures = _selectedIndices
            .Where(i => i >= 0 && i < _filteredResults.Count)
            .Select(i => _filteredResults[i])
            .ToList();

        if (signatures.Count == 0) return;

        // 確認對話框
        Console.Write(CLEAR_SCREEN);
        Console.Write(string.Format(MOVE_CURSOR, 1, 1));
        Console.WriteLine($"\n  {BOLD}確認刪除{RESET}\n");
        Console.WriteLine($"  即將刪除 {signatures.Count} 個方法：\n");

        foreach (var sig in signatures.Take(10))
        {
            string display = sig.Length > 65 ? sig.Substring(0, 62) + "..." : sig;
            Console.WriteLine($"    {RED}•{RESET} {display}");
        }
        if (signatures.Count > 10)
            Console.WriteLine($"    ... 還有 {signatures.Count - 10} 個");

        Console.WriteLine($"\n  {YELLOW}如有實作介面，介面方法也會一併刪除。{RESET}");
        Console.Write($"\n  確定刪除？ {BOLD}(Y/N){RESET}: ");

        var confirmKey = Console.ReadKey(true);
        if (confirmKey.Key != ConsoleKey.Y) return;

        Console.Write($"\n  {CYAN}刪除中...{RESET}");

        try
        {
            var (success, message) = await ReferenceChecker.RemoveMethodsAsync(_solutionPath, signatures);

            if (success)
            {
                Console.WriteLine($"\n  {GREEN}✔ {message}{RESET}");
                Console.Write($"  {CYAN}重新檢查中...{RESET}");

                _allResults.Clear();
                _selectedIndices.Clear();
                _cursorIndex = 0;

                var results = await ReferenceChecker.Check(_solutionPath);
                _allResults.AddRange(results);
                ApplyFilter();

                Console.WriteLine($"\n  {GREEN}✔ 重新檢查完成，找到 {_allResults.Count} 個未參照方法。{RESET}");
            }
            else
            {
                Console.WriteLine($"\n  {RED}✖ {message}{RESET}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n  {RED}錯誤: {ex.Message}{RESET}");
        }

        Console.Write("\n  按任意鍵繼續...");
        Console.ReadKey(true);
    }

    /// <summary>
    /// 在畫面底部顯示訊息並等待按鍵。
    /// </summary>
    private static void ShowMessage(string message)
    {
        Console.Write(string.Format(MOVE_CURSOR, Console.WindowHeight - 2, 1));
        Console.Write(CLEAR_LINE);
        Console.Write(message);
        Console.ReadKey(true);
    }
}

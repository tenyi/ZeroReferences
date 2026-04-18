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
    /// 找出指定存取層級（public / private / protected）且在整個解決方案中沒有被引用的方法（孤兒方法）。
    /// </summary>
    public static class ReferenceChecker
    {
        /// <summary>
        /// 方法簽名顯示格式：包含存取修飾詞、回傳型別、完整型別名稱與參數型別。
        /// 例如：public void MyNamespace.MyClass.MyMethod(int)
        /// </summary>
        private static readonly SymbolDisplayFormat MethodSignatureDisplayFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            memberOptions: SymbolDisplayMemberOptions.IncludeAccessibility |
                           SymbolDisplayMemberOptions.IncludeType |
                           SymbolDisplayMemberOptions.IncludeContainingType |
                           SymbolDisplayMemberOptions.IncludeParameters,
            parameterOptions: SymbolDisplayParameterOptions.IncludeType |
                              SymbolDisplayParameterOptions.IncludeParamsRefOut,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                                  SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

        /// <summary>
        /// 判斷方法的存取層級是否為本工具要掃描的範圍（public / private / protected）。
        /// 另外一併納入 protected internal 與 private protected。
        /// </summary>
        private static bool ShouldAnalyzeAccessibility(Microsoft.CodeAnalysis.Accessibility accessibility)
        {
            return accessibility == Microsoft.CodeAnalysis.Accessibility.Public ||
                   accessibility == Microsoft.CodeAnalysis.Accessibility.Private ||
                   accessibility == Microsoft.CodeAnalysis.Accessibility.Protected ||
                   accessibility == Microsoft.CodeAnalysis.Accessibility.ProtectedOrInternal ||
                   accessibility == Microsoft.CodeAnalysis.Accessibility.ProtectedAndInternal;
        }

        /// <summary>
        /// 產生方法顯示簽名，供 UI 顯示與刪除比對共用，避免格式不一致。
        /// </summary>
        private static string GetMethodSignature(IMethodSymbol symbol)
        {
            return symbol.ToDisplayString(MethodSignatureDisplayFormat);
        }

        /// <summary>
        /// 分析指定的解決方案檔案，找出所有未被引用的 public / private / protected 方法。
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
            // 檢查副檔名是否為 .sln / .slnx / .csproj
            string ext = Path.GetExtension(solutionPath).ToLower();
            if (ext != ".sln" && ext != ".slnx" && ext != ".csproj")
            {
                throw new ArgumentException("Path must have a .sln / .slnx / .csproj extension.");
            }
            // 檢查檔案是否存在
            if (!File.Exists(solutionPath))
            {
                throw new ArgumentException("Solution/project file does not exist.");
            }

            // ===== 建立工作區並開啟解決方案或專案 =====
            using var workspace = MSBuildWorkspace.Create();
            Solution solution;
            IEnumerable<Project> projects;

            if (ext == ".csproj")
            {
                var project = await workspace.OpenProjectAsync(solutionPath);
                solution = project.Solution;
                projects = new[] { project };
            }
            else
            {
                solution = await workspace.OpenSolutionAsync(solutionPath);
                projects = solution.Projects;
            }

            // ===== 遍歷專案 =====
            foreach (var project in projects)
            {
                // 取得專案的編譯物件（Compilation），用於語意分析
                var compilation = await project.GetCompilationAsync();
                if (compilation == null) continue;

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
                    if (symbol == null) continue;

                    // 只檢查目標存取層級的方法（public / private / protected）
                    if (ShouldAnalyzeAccessibility(symbol.DeclaredAccessibility))
                    {
                        // 使用統一格式取得完整簽名（含存取修飾詞）
                        string name = GetMethodSignature(symbol);

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
                        // 跳過 Main 方法（程式入口點，不應視為孤兒方法）
                        if (symbol.Name == "Main")
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
                        }
                    }
                }
            }

            // 回傳所有未參照方法的清單
            return list;
        }

        /// <summary>
        /// 批次刪除多個方法。只開啟一次工作區，在單一交易中完成所有刪除。
        /// 若方法有實作介面（explicit 或 implicit），也會一併刪除介面中的方法宣告。
        /// </summary>
        /// <param name="solutionPath">.sln/.slnx/.csproj 檔案的完整路徑。</param>
        /// <param name="methodSignatures">要刪除的方法完整簽字串清單。</param>
        /// <returns>回傳 tuple，包含是否成功及訊息。</returns>
        public static async Task<(bool success, string message)> RemoveMethodsAsync(
            string solutionPath, List<string> methodSignatures)
        {
            // ===== 建立工作區並開啟解決方案或專案 =====
            using var workspace = MSBuildWorkspace.Create();
            var solution = await OpenSolutionOrProjectAsync(workspace, solutionPath);

            // 用於記錄需要從各文件中刪除的方法語法節點
            var nodesToRemove = new Dictionary<Microsoft.CodeAnalysis.DocumentId, List<MethodDeclarationSyntax>>();

            // 記錄每個簽名是否找到對應方法
            var foundSignatures = new HashSet<string>();
            var signatureSet = new HashSet<string>(methodSignatures);

            // ===== 遍歷解決方案尋找所有目標方法 =====
            foreach (var project in solution.Projects)
            {
                var compilation = await project.GetCompilationAsync();
                if (compilation == null) continue;

                foreach (var document in project.Documents)
                {
                    var syntaxTree = await document.GetSyntaxTreeAsync();
                    if (syntaxTree == null) continue;

                    var root = await syntaxTree.GetRootAsync();
                    var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

                    foreach (var method in methods)
                    {
                        var model = compilation.GetSemanticModel(method.SyntaxTree);
                        var symbol = model.GetDeclaredSymbol(method) as IMethodSymbol;
                        if (symbol == null) continue;

                        string signature = GetMethodSignature(symbol);

                        // 比對是否為任一目標簽名
                        if (signatureSet.Contains(signature))
                        {
                            foundSignatures.Add(signature);

                            if (!nodesToRemove.ContainsKey(document.Id))
                            {
                                nodesToRemove[document.Id] = new List<MethodDeclarationSyntax>();
                            }
                            nodesToRemove[document.Id].Add(method);

                            // 處理 Explicit Interface Implementation
                            foreach (var ifaceMethod in symbol.ExplicitInterfaceImplementations)
                            {
                                await FindAndMarkInterfaceMethodForRemoval(
                                    solution, ifaceMethod, nodesToRemove);
                            }

                            // 處理 Implicit Interface Implementation
                            await FindAndMarkImplicitInterfaceMethodsForRemoval(
                                solution, compilation, symbol, nodesToRemove);

                            // ===== 處理 override 鏈：找出所有 override 此方法的 derived class 方法 =====
                            // 當刪除 abstract 或 virtual 方法時，所有 override 它的方法都應該被刪除
                            if (symbol.IsVirtual || symbol.IsAbstract || symbol.IsOverride)
                            {
                                await FindAndMarkOverridingMethodsForRemoval(
                                    solution, symbol, nodesToRemove);
                            }
                        }
                    }
                }
            }

            // ===== 檢查是否有未找到的簽名 =====
            var notFound = methodSignatures.Where(s => !foundSignatures.Contains(s)).ToList();
            if (nodesToRemove.Count == 0)
            {
                return (false, $"找不到任何指定的方法。");
            }

            // ===== 執行刪除操作 =====
            // Roslyn 語法樹是不可變的，手動逐一 RemoveNode 會導致後續節點
            // （屬於原始樹）在新樹中找不到而靜默失敗。
            // 使用 TrackNodes / GetCurrentNode 機制，讓 Roslyn 自動追蹤
            // 節點在多次編輯間的對應關係。
            var updatedSolution = solution;

            foreach (var kvp in nodesToRemove)
            {
                var documentId = kvp.Key;
                var nodes = kvp.Value;
                var document = updatedSolution.GetDocument(documentId);
                if (document == null) continue;

                var root = await document.GetSyntaxRootAsync();
                if (root == null) continue;

                // 告訴 Roslyn 追蹤這些節點，以便後續在修改後的樹中找到它們
                var currentRoot = root.TrackNodes(nodes);

                // 逐一從最新樹中找到追蹤的節點並刪除
                foreach (var originalNode in nodes)
                {
                    var currentNode = currentRoot.GetCurrentNode(originalNode);
                    if (currentNode != null)
                    {
                        currentRoot = currentRoot.RemoveNode(
                            currentNode, SyntaxRemoveOptions.KeepNoTrivia)!;
                    }
                }

                updatedSolution = updatedSolution.WithDocumentSyntaxRoot(documentId, currentRoot);
            }

            // ===== 套用變更 =====
            bool applied = workspace.TryApplyChanges(updatedSolution);
            if (applied)
            {
                string msg = $"已成功刪除 {foundSignatures.Count} 個方法。";
                if (notFound.Count > 0)
                {
                    msg += $"\n（其中 {notFound.Count} 個簽名未找到匹配方法）";
                }
                return (true, msg);
            }
            else
            {
                return (false, "套用變更失敗。");
            }
        }

        /// <summary>
        /// 刪除指定的單一方法。重新開啟解決方案，找到符合簽名的方法並刪除。
        /// 若該方法有實作介面（explicit 或 implicit），也會一併刪除介面中的方法宣告。
        /// </summary>
        /// <param name="solutionPath">.sln/.slnx/.csproj 檔案的完整路徑。</param>
        /// <param name="methodSignature">方法的完整簽名字串（由 ToDisplayString() 產生）。</param>
        /// <returns>回傳 tuple，包含是否成功及訊息。</returns>
        public static async Task<(bool success, string message)> RemoveMethodAsync(
            string solutionPath, string methodSignature)
        {
            // ===== 建立工作區並開啟解決方案或專案 =====
            using var workspace = MSBuildWorkspace.Create();
            var solution = await OpenSolutionOrProjectAsync(workspace, solutionPath);

            // 用於記錄需要從各文件中刪除的方法語法節點
            // Key = DocumentId, Value = 要刪除的方法語法節點清單
            var nodesToRemove = new Dictionary<Microsoft.CodeAnalysis.DocumentId, List<MethodDeclarationSyntax>>();

            // ===== 遍歷解決方案尋找目標方法 =====
            foreach (var project in solution.Projects)
            {
                var compilation = await project.GetCompilationAsync();
                if (compilation == null) continue;

                foreach (var document in project.Documents)
                {
                    var syntaxTree = await document.GetSyntaxTreeAsync();
                    if (syntaxTree == null) continue;

                    var root = await syntaxTree.GetRootAsync();
                    var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

                    foreach (var method in methods)
                    {
                        var model = compilation.GetSemanticModel(method.SyntaxTree);
                        var symbol = model.GetDeclaredSymbol(method) as IMethodSymbol;
                        if (symbol == null) continue;

                        // 比對簽名字串是否與目標一致
                        if (GetMethodSignature(symbol) == methodSignature)
                        {
                            // 記錄此方法節點待刪除
                            if (!nodesToRemove.ContainsKey(document.Id))
                            {
                                nodesToRemove[document.Id] = new List<MethodDeclarationSyntax>();
                            }
                            nodesToRemove[document.Id].Add(method);

                            // ===== 處理 Explicit Interface Implementation =====
                            // 例如：void IMyInterface.MyMethod()
                            var explicitInterfaces = symbol.ExplicitInterfaceImplementations;
                            foreach (var ifaceMethod in explicitInterfaces)
                            {
                                await FindAndMarkInterfaceMethodForRemoval(
                                    solution, ifaceMethod, nodesToRemove);
                            }

                            // ===== 處理 Implicit Interface Implementation =====
                            // 例如：public void MyMethod() 隱含實作了 IMyInterface.MyMethod()
                            await FindAndMarkImplicitInterfaceMethodsForRemoval(
                                solution, compilation, symbol, nodesToRemove);

                            // ===== 處理 override 鏈：找出所有 override 此方法的 derived class 方法 =====
                            if (symbol.IsVirtual || symbol.IsAbstract || symbol.IsOverride)
                            {
                                await FindAndMarkOverridingMethodsForRemoval(
                                    solution, symbol, nodesToRemove);
                            }
                        }
                    }
                }
            }

            // ===== 若找不到任何方法，回傳失敗 =====
            if (nodesToRemove.Count == 0)
            {
                return (false, $"找不到方法：{methodSignature}");
            }

            // ===== 執行刪除操作 =====
            // 使用 TrackNodes / GetCurrentNode 追蹤機制，正確處理
            // 同一文件中多個節點（含介面方法）的刪除。
            var updatedSolution = solution;

            foreach (var kvp in nodesToRemove)
            {
                var documentId = kvp.Key;
                var nodes = kvp.Value;
                var document = updatedSolution.GetDocument(documentId);
                if (document == null) continue;

                var root = await document.GetSyntaxRootAsync();
                if (root == null) continue;

                var currentRoot = root.TrackNodes(nodes);
                foreach (var originalNode in nodes)
                {
                    var currentNode = currentRoot.GetCurrentNode(originalNode);
                    if (currentNode != null)
                    {
                        currentRoot = currentRoot.RemoveNode(
                            currentNode, SyntaxRemoveOptions.KeepNoTrivia)!;
                    }
                }

                updatedSolution = updatedSolution.WithDocumentSyntaxRoot(documentId, currentRoot);
            }

            // ===== 套用變更到工作區 =====
            bool applied = workspace.TryApplyChanges(updatedSolution);
            if (applied)
            {
                return (true, $"已成功刪除方法：{methodSignature}");
            }
            else
            {
                return (false, "套用變更失敗。");
            }
        }

        /// <summary>
        /// 找到介面中的方法宣告並標記為待刪除。
        /// 用於處理 Explicit Interface Implementation 的情況。
        /// </summary>
        /// <param name="solution">Roslyn 解決方案物件。</param>
        /// <param name="ifaceMethod">介面中的方法符號。</param>
        /// <param name="nodesToRemove">待刪除節點的字典。</param>
        private static async Task FindAndMarkInterfaceMethodForRemoval(
            Solution solution,
            IMethodSymbol ifaceMethod,
            Dictionary<Microsoft.CodeAnalysis.DocumentId, List<MethodDeclarationSyntax>> nodesToRemove)
        {
            // 取得介面方法所在的語法節點
            foreach (var project in solution.Projects)
            {
                var compilation = await project.GetCompilationAsync();
                if (compilation == null) continue;

                foreach (var document in project.Documents)
                {
                    var syntaxTree = await document.GetSyntaxTreeAsync();
                    if (syntaxTree == null) continue;

                    var root = await syntaxTree.GetRootAsync();
                    var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

                    foreach (var method in methods)
                    {
                        var model = compilation.GetSemanticModel(method.SyntaxTree);
                        var symbol = model.GetDeclaredSymbol(method) as IMethodSymbol;
                        if (symbol == null) continue;

                        // 比對是否為同一個介面方法
                        if (SymbolEqualityComparer.Default.Equals(symbol, ifaceMethod))
                        {
                            if (!nodesToRemove.ContainsKey(document.Id))
                            {
                                nodesToRemove[document.Id] = new List<MethodDeclarationSyntax>();
                            }
                            // 避免重複加入
                            if (!nodesToRemove[document.Id].Contains(method))
                            {
                                nodesToRemove[document.Id].Add(method);
                            }
                            return; // 找到後即可返回
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 找到隱含實作的介面方法並標記為待刪除。
        /// 隱含實作是指類別中以 public void MyMethod() 或 public override void MyMethod()
        /// 實作 IMyInterface.MyMethod() 的情況。
        /// </summary>
        /// <param name="solution">Roslyn 解決方案物件。</param>
        /// <param name="compilation">編譯物件，用於語意分析。</param>
        /// <param name="methodSymbol">類別中要刪除的方法符號。</param>
        /// <param name="nodesToRemove">待刪除節點的字典。</param>
        private static async Task FindAndMarkImplicitInterfaceMethodsForRemoval(
            Solution solution,
            Compilation compilation,
            IMethodSymbol methodSymbol,
            Dictionary<Microsoft.CodeAnalysis.DocumentId, List<MethodDeclarationSyntax>> nodesToRemove)
        {
            // 取得包含此方法的類別所實作的所有介面
            var containingType = methodSymbol.ContainingType;
            if (containingType == null) return;

            // 遍歷此類別的所有介面（包含繼承鏈上的介面）
            foreach (var iface in containingType.AllInterfaces)
            {
                // 在介面中尋找與目標方法同名且簽名一致的方法
                foreach (var member in iface.GetMembers(methodSymbol.Name))
                {
                    if (member is IMethodSymbol ifaceMethod)
                    {
                        // 比對參數數量
                        if (ifaceMethod.Parameters.Length != methodSymbol.Parameters.Length)
                            continue;

                        // 逐一比對參數型別是否一致
                        bool paramsMatch = true;
                        for (int i = 0; i < ifaceMethod.Parameters.Length; i++)
                        {
                            if (!SymbolEqualityComparer.Default.Equals(
                                ifaceMethod.Parameters[i].Type,
                                methodSymbol.Parameters[i].Type))
                            {
                                paramsMatch = false;
                                break;
                            }
                        }

                        if (paramsMatch)
                        {
                            // ===== 找到匹配的介面方法，標記為待刪除 =====
                            await FindAndMarkInterfaceMethodForRemoval(
                                solution, ifaceMethod, nodesToRemove);

                            // ===== 額外：找到所有實作此介面方法的類別方法（含 override）並標記刪除 =====
                            // 這是因為當刪除介面方法時，所有實作它的類別方法都會變成無效
                            // 包括帶有 override 關鍵字的方法
                            await FindAllImplementingMethodsForRemoval(
                                solution, compilation, ifaceMethod, nodesToRemove);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 找到所有實作指定介面方法的類別方法，並標記為待刪除。
        /// 這確保了当介面被刪除時，所有 override 或隱含實作該介面的類別方法也會被刪除。
        /// </summary>
        /// <param name="solution">Roslyn 解決方案物件。</param>
        /// <param name="compilation">編譯物件，用於語意分析。</param>
        /// <param name="ifaceMethod">介面中的方法符號。</param>
        /// <param name="nodesToRemove">待刪除節點的字典。</param>
        private static async Task FindAllImplementingMethodsForRemoval(
            Solution solution,
            Compilation compilation,
            IMethodSymbol ifaceMethod,
            Dictionary<Microsoft.CodeAnalysis.DocumentId, List<MethodDeclarationSyntax>> nodesToRemove)
        {
            // 遍歷解決方案中的所有專案和文件
            foreach (var project in solution.Projects)
            {
                var projCompilation = await project.GetCompilationAsync();
                if (projCompilation == null) continue;

                foreach (var document in project.Documents)
                {
                    var syntaxTree = await document.GetSyntaxTreeAsync();
                    if (syntaxTree == null) continue;

                    var root = await syntaxTree.GetRootAsync();
                    var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

                    foreach (var method in methods)
                    {
                        var model = projCompilation.GetSemanticModel(method.SyntaxTree);
                        var symbol = model.GetDeclaredSymbol(method) as IMethodSymbol;
                        if (symbol == null) continue;

                        // 檢查是否是 explicit implementation
                        foreach (var explicitImpl in symbol.ExplicitInterfaceImplementations)
                        {
                            if (SymbolEqualityComparer.Default.Equals(explicitImpl, ifaceMethod))
                            {
                                AddNodeToRemove(document.Id, method, nodesToRemove);
                                break;
                            }
                        }

                        // 檢查是否為 implicit implementation（方法名稱和參數匹配介面方法）
                        if (symbol.Name == ifaceMethod.Name &&
                            symbol.Parameters.Length == ifaceMethod.Parameters.Length)
                        {
                            bool paramsMatch = true;
                            for (int i = 0; i < symbol.Parameters.Length; i++)
                            {
                                if (!SymbolEqualityComparer.Default.Equals(
                                    symbol.Parameters[i].Type,
                                    ifaceMethod.Parameters[i].Type))
                                {
                                    paramsMatch = false;
                                    break;
                                }
                            }

                            if (paramsMatch)
                            {
                                // 檢查此方法是否在實作該介面的類型中
                                var methodContainingType = symbol.ContainingType;
                                if (methodContainingType != null)
                                {
                                    foreach (var iface in methodContainingType.AllInterfaces)
                                    {
                                        if (SymbolEqualityComparer.Default.Equals(iface, ifaceMethod.ContainingType))
                                        {
                                            // 找到了！即使這個方法不是孤兒（可能有其他引用），
                                            // 也要刪除它，因為它正在實作即將被刪除的介面方法
                                            AddNodeToRemove(document.Id, method, nodesToRemove);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 將方法節點安全地加入待刪除字典。
        /// 避免重複加入同一個節點。
        /// </summary>
        private static void AddNodeToRemove(
            Microsoft.CodeAnalysis.DocumentId documentId,
            MethodDeclarationSyntax method,
            Dictionary<Microsoft.CodeAnalysis.DocumentId, List<MethodDeclarationSyntax>> nodesToRemove)
        {
            if (!nodesToRemove.ContainsKey(documentId))
            {
                nodesToRemove[documentId] = new List<MethodDeclarationSyntax>();
            }
            if (!nodesToRemove[documentId].Contains(method))
            {
                nodesToRemove[documentId].Add(method);
            }
        }

        /// <summary>
        /// 找到所有 override 指定方法（透過繼承鏈）的方法，並標記為待刪除。
        /// 當刪除 abstract class 的方法時，所有 override 它的 derived class 方法都應該被刪除。
        /// </summary>
        /// <param name="solution">Roslyn 解決方案物件。</param>
        /// <param name="methodToOverride">被 override 的方法（可能是 abstract 或 virtual）。</param>
        /// <param name="nodesToRemove">待刪除節點的字典。</param>
        private static async Task FindAndMarkOverridingMethodsForRemoval(
            Solution solution,
            IMethodSymbol methodToOverride,
            Dictionary<Microsoft.CodeAnalysis.DocumentId, List<MethodDeclarationSyntax>> nodesToRemove)
        {
            // 遍歷解決方案中的所有專案和文件
            foreach (var project in solution.Projects)
            {
                var compilation = await project.GetCompilationAsync();
                if (compilation == null) continue;

                foreach (var document in project.Documents)
                {
                    var syntaxTree = await document.GetSyntaxTreeAsync();
                    if (syntaxTree == null) continue;

                    var root = await syntaxTree.GetRootAsync();
                    var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

                    foreach (var method in methods)
                    {
                        var model = compilation.GetSemanticModel(method.SyntaxTree);
                        var symbol = model.GetDeclaredSymbol(method) as IMethodSymbol;
                        if (symbol == null) continue;

                        // 檢查此方法的 override 鏈是否包含目標方法
                        var overridden = symbol.OverriddenMethod;
                        while (overridden != null)
                        {
                            if (SymbolEqualityComparer.Default.Equals(overridden, methodToOverride))
                            {
                                // 找到了！這個方法 override 了目標方法，應該被刪除
                                AddNodeToRemove(document.Id, method, nodesToRemove);
                                break;
                            }
                            overridden = overridden.OverriddenMethod;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 根據副檔名決定開啟解決方案或專案，回傳 Solution 物件。
        /// .csproj 使用 OpenProjectAsync，.sln/.slnx 使用 OpenSolutionAsync。
        /// </summary>
        private static async Task<Solution> OpenSolutionOrProjectAsync(
            MSBuildWorkspace workspace, string path)
        {
            string ext = Path.GetExtension(path).ToLower();
            if (ext == ".csproj")
            {
                var project = await workspace.OpenProjectAsync(path);
                return project.Solution;
            }
            return await workspace.OpenSolutionAsync(path);
        }
    }
}

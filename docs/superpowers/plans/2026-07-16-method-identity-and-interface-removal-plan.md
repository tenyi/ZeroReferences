# 安全方法刪除修正 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task with review checkpoints.

**Goal:** 讓掃描結果能唯一定位跨專案的同名方法，並只刪除 Roslyn 確認的真正介面實作。

**Architecture:** Core 新增包含簽名與來源位置的公開結果模型；`Check` 回傳此模型，刪除 API 接收同一模型並以專案、文件與宣告位置定位語法節點。四個前端只將結果模型用於顯示／篩選並原樣傳回 Core；介面連帶刪除改用 `FindImplementationForInterfaceMember`。

**Tech Stack:** C#、.NET 10、Roslyn `Microsoft.CodeAnalysis` 5.3.0、xUnit、MSBuildWorkspace。

## Global Constraints

- 保留 `.csproj` 開啟後掃描該專案與相依專案的既有行為。
- 保留現有 Controller、Test、Main 排除規則與 public/private/protected 篩選規則。
- `MainForm.Designer.cs` 不得修改。
- 所有行為變更先以失敗測試重現，再寫最小 production code。
- 不引入新的 NuGet 套件。

---

### Task 1: 建立跨專案與介面 helper 的失敗整合測試

**Files:**
- Modify: `Tests/TestSolutionBuilder.cs`
- Modify: `Tests/ReferenceCheckerTests.cs`

**Interfaces:**
- `TestSolutionBuilder.CreateMultiProjectSolutionAsync` 產生包含兩個獨立專案的 `.sln`，回傳 solution 路徑。

- [ ] **Step 1: 擴充測試 builder**

新增 `CreateMultiProjectSolutionAsync`，在同一臨時目錄建立 `ProjectA/ProjectA.csproj`、`ProjectB/ProjectB.csproj`、各自的 `Code.cs` 與 solution 檔。兩個檔案都宣告：

```csharp
namespace DuplicateNamespace;

public class DuplicateType
{
    public void Orphan() { }
}
```

專案檔只使用 `Microsoft.NET.Sdk`、`net10.0`、nullable 與 implicit usings，並且不建立 ProjectReference，確保兩個方法只靠專案位置區分。

- [ ] **Step 2: 新增跨專案刪除測試**

在 `Tests/ReferenceCheckerTests.cs` 新增 `RemoveMethodAsync_SameSignatureInDifferentProjects_RemovesOnlySelectedDeclaration`。在 API 尚未遷移前，測試先用現有字串結果要求兩筆結果必須可區分，並傳入 ProjectA 的選取值給 `RemoveMethodAsync`；現有實作會因全域字串比對而刪除兩個專案，測試因此在行為層級失敗。最後以 `File.ReadAllTextAsync` 驗證 ProjectA 的 `Orphan` 已移除，而 ProjectB 的 `Orphan` 仍存在。Task 2 再將相同測試改為使用 `MethodResult.ProjectPath`。

- [ ] **Step 3: 新增非實作 helper 測試**

新增 `RemoveMethodAsync_DerivedPrivateHelperIsNotInterfaceImplementation`，測試程式碼固定為：

```csharp
namespace InterfaceRemoval;

public interface IWorker
{
    void Execute();
}

public class BaseWorker : IWorker
{
    public virtual void Execute() { }
}

public class DerivedWorker : BaseWorker
{
    private void Execute() { }

    public void KeepHelperAlive()
    {
        Execute();
    }
}
```

測試以現有結果字串選取 `BaseWorker.Execute`，刪除後驗證 `DerivedWorker` 的 private `Execute` 與 `KeepHelperAlive` 仍在檔案中，且結果不因同名猜測而刪除 helper。

- [ ] **Step 4: 執行測試確認 RED**

Run: `dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~SameSignatureInDifferentProjects|FullyQualifiedName~DerivedPrivateHelperIsNotInterfaceImplementation"`

Expected: 測試因目前以簽名字串全域比對，以及介面 helper 的名稱／參數近似比對而失敗；不得以測試 setup 例外作為唯一失敗原因。

### Task 2: 新增結果模型並讓 Core 以來源位置識別方法

**Files:**
- Modify: `Core/ReferenceChecker.cs`
- Modify: `Tests/ReferenceCheckerTests.cs`

**Interfaces:**
- 新增 `public sealed record MethodResult(string Signature, string ProjectPath, string DocumentPath, int SpanStart)`。
- `MethodResult.ToString()` 回傳 `Signature`；`DisplayName` 固定包含 project/document 資訊，確保相同簽名也能辨識。
- `ReferenceChecker.Check(string solutionPath)` 改回傳 `Task<List<MethodResult>>`。
- `RemoveMethodAsync(string solutionPath, MethodResult method)` 與 `RemoveMethodsAsync(string solutionPath, IReadOnlyCollection<MethodResult> methods)` 接收結果模型。

- [ ] **Step 1: 定義最小結果模型**

在 `ZeroReferences` namespace 建立 `MethodResult`，以 `Signature`、`ProjectPath`、`DocumentPath`、`SpanStart` 作為 immutable positional properties。`ToString()` 回傳簽名；`DisplayName` 回傳 `$"{Signature} [{ProjectPath}: {DocumentPath}]"`，使用完整路徑避免同名專案／文件再次碰撞。

- [ ] **Step 2: 讓方法列舉保留來源位置**

將 `EnumerateMethodsAsync` 回傳的 tuple 擴充為 `Project project`、`Document document`、`MethodDeclarationSyntax method` 與 `IMethodSymbol symbol`，在 `Check` 建立 `MethodResult`：

```csharp
new MethodResult(
    GetMethodSignature(symbol),
    project.FilePath ?? string.Empty,
    document.FilePath ?? string.Empty,
    method.SpanStart)
```

保留 `solution.Projects` 的完整列舉，不將 `.csproj` 限制回單一專案。

- [ ] **Step 3: 以來源位置建立刪除目標**

新增私有 `FindMethodByResult`，只接受 project path、document path 與 span start；從目前 solution 找到相同 project/document，再以 `MethodDeclarationSyntax.SpanStart` 找到唯一節點與符號。找不到時回傳 null，使批次結果能正確形成 `Partial`。

- [ ] **Step 4: 以結果模型取代字串比對**

`RemoveMethodAsync` 與 `RemoveMethodsAsync` 改由 `FindMethodByResult` 建立 `nodesToRemove`，不再執行 `GetMethodSignature(symbol) == methodSignature` 或 `HashSet<string>` 全域比對。連帶介面／override 的既有處理仍使用找到的 `IMethodSymbol`。

- [ ] **Step 5: 更新 Core 測試編譯與結果斷言**

將既有測試中 `before.First(...)` 取得的字串改為 `MethodResult`，斷言改用 `.Signature`；刪除呼叫直接傳入結果物件。確認 Task 1 的跨專案測試在此步驟轉為 GREEN，而介面 helper 測試仍保持 RED，等待 Task 3 的語意判定修正。

### Task 3: 修正真正介面實作判定

**Files:**
- Modify: `Core/ReferenceChecker.cs`
- Modify: `Tests/ReferenceCheckerTests.cs`

**Interfaces:**
- `FindAndMarkImplicitInterfaceMethodsForRemoval` 只接受 `Solution`、`IMethodSymbol` 與 node dictionary。
- `FindAllImplementingMethodsForRemoval` 以 `ITypeSymbol.FindImplementationForInterfaceMember` 驗證實作符號。

- [ ] **Step 1: 以 Roslyn 實作關係取代近似比對**

在 `FindAndMarkImplicitInterfaceMethodsForRemoval` 對 `containingType.AllInterfaces` 的每個 `IMethodSymbol ifaceMethod` 執行：

```csharp
var implementation = containingType.FindImplementationForInterfaceMember(ifaceMethod);
if (SymbolEqualityComparer.Default.Equals(implementation, methodSymbol))
{
    await FindAndMarkInterfaceMethodForRemoval(solution, ifaceMethod, nodesToRemove);
    await FindAllImplementingMethodsForRemoval(solution, ifaceMethod, nodesToRemove);
}
```

移除名稱、參數數量與參數型別的手動迴圈。

- [ ] **Step 2: 精確尋找所有實作者**

在 `FindAllImplementingMethodsForRemoval` 遍歷方法時，以：

```csharp
var implementation = symbol.ContainingType?
    .FindImplementationForInterfaceMember(ifaceMethod);
if (SymbolEqualityComparer.Default.Equals(implementation, symbol))
{
    AddNodeToRemove(document.Id, method, nodesToRemove);
}
```

取代 `symbol.Name`、參數數量、型別與 `AllInterfaces` 的猜測式匹配；保留 explicit implementation 的既有符號比對。

- [ ] **Step 3: 執行兩個回歸測試確認 GREEN**

Run: `dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~SameSignatureInDifferentProjects|FullyQualifiedName~DerivedPrivateHelperIsNotInterfaceImplementation"`

Expected: 兩個測試 PASS，且 Derived 的 private helper 未被刪除。

### Task 4: 遷移四個前端至結果模型

**Files:**
- Modify: `GUI/MainForm.cs`
- Modify: `AvaloniaGUI/MainWindow.axaml.cs`
- Modify: `TUI/Program.cs`
- Modify: `CLI/Program.cs`

**Interfaces:**
- UI 內部清單改為 `List<MethodResult>`。
- ListBox／TUI 顯示 `MethodResult.DisplayName`，accessibility 篩選使用 `MethodResult.Signature`。
- 刪除時直接傳遞所選的 `MethodResult` 集合。

- [ ] **Step 1: 遷移 WinForms**

將 `allMethodResults` 改為 `List<MethodResult>`；篩選檢查 `method.Signature.StartsWith(...)`；ListBox 加入 `method.DisplayName`，並以 ListBox item 對應的結果模型收集刪除目標。Designer 不得修改。

- [ ] **Step 2: 遷移 Avalonia**

將 `_allMethodResults` 改為 `List<MethodResult>`；篩選使用 `.Signature`；`ResultListBox` 綁定顯示模型或 `DisplayName`；刪除使用所選 `MethodResult`，不再 `.Cast<string>()`。

- [ ] **Step 3: 遷移 TUI 與 CLI**

TUI 的 `_allResults`／`_filteredResults` 改為 `MethodResult` 集合，畫面與確認訊息使用 `DisplayName`，篩選與色彩判斷使用 `Signature`，刪除傳入模型集合。CLI 輸出 `DisplayName`，保留目前計數與錯誤處理。

- [ ] **Step 4: 建置前端專案**

Run: `dotnet build ZeroReferences.slnx /p:Configuration=Debug /p:Platform="Any CPU" /p:EnableWindowsTargeting=true`

Expected: Core、Tests、CLI、TUI、AvaloniaGUI 與 GUI 均成功編譯，無 `string`／`MethodResult` 型別轉換錯誤。

### Task 5: 完整驗證與整理

**Files:**
- Verify: `Core/ReferenceChecker.cs`
- Verify: `Tests/ReferenceCheckerTests.cs`
- Verify: `GUI/MainForm.cs`
- Verify: `AvaloniaGUI/MainWindow.axaml.cs`
- Verify: `TUI/Program.cs`
- Verify: `CLI/Program.cs`

- [ ] **Step 1: 執行完整測試**

Run: `dotnet test Tests/Tests.csproj`

Expected: 所有既有測試與新增回歸測試 PASS。

- [ ] **Step 2: 驗證工作樹與差異範圍**

Run: `git diff --check`

Expected: 沒有 whitespace error；變更只涵蓋 Core、Tests、四個前端與本次計畫文件，不包含 `MainForm.Designer.cs` 或無關產物。

- [ ] **Step 3: 重新檢視關鍵不變條件**

確認 `.csproj` 仍透過 `solution.Projects` 全掃描；Controller/Test/Main 排除與 accessibility 篩選未改變；刪除同名方法時只修改選定的 project/document/span。

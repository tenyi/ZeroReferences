# ZeroReferences 程式碼審查報告

**審查日期：** 2026-04-21
**對照基準：** `Review_qwen36.md`（舊版審查）
**專案路徑：** `D:\Git\ZeroReferences`

---

## 已修復項目摘要

本次審查已針對舊版 12 項問題中的 8 項完成修復。以下逐項記錄修復方式：

| 舊編號 | 問題 | 修復方式 |
|--------|------|----------|
| #1 (P0) | 排除邏輯 `name.Contains("Controller/Test")` 過於寬鬆 | 改為 `symbol.ContainingType.Name.Contains()`，只比對類別名稱 |
| #6 (P1) | WinForms 刪除失敗後 `removeMethodButton` 未恢復 | `finally` 區塊加入 `removeMethodButton.Enabled = resultListBox.SelectedItems.Count > 0` |
| #7 (P1) | Avalonia `"private"` / `"protected"` 缺少尾隨空格 | 補上空格，三個 UI 專案篩選邏輯現已一致 |
| N1 (P1) | Avalonia 刪除失敗後 `RemoveMethodButton` 未恢復 | `finally` 區塊加入恢復邏輯 |
| N2 (P2) | `MSBuildWorkspace.Create()` 未註冊診斷事件 | 新增 `CreateWorkspace()` helper，使用 `RegisterWorkspaceFailedHandler` |
| N4 (P2) | 部分失敗時 `success` 仍為 `true` | 新增 `RemoveResult` 列舉（`Success`/`Partial`/`Failed`），三個 UI 呼叫端分別處理 |
| N3 (P3) | `nodesToRemove` 使用 `List<>` 做 `Contains` 重複檢查 | `List<>` → `HashSet<>`，移除 4 處手動 `Contains` 檢查 |
| #9 (P3) | 測試硬編碼 `/tmp/` 路徑 | 改用 `Path.GetTempPath()` + `Guid.NewGuid()` |
| #10 (P3) | CLI 只輸出 `ex.Message` | 加入 `InnerException` 輸出 |
| #12 (P3) | `ShouldAnalyzeAccessibility` 冗長 | 改用 C# `is` pattern + `or` 表達式 |
| #5 (P3) | WinForms `MainForm.cs:219` 錯誤訊息英文且缺少 `.csproj` | 改為繁體中文並補上 `.csproj` |

---

## 仍需改進之處

### 1. ~~`ReferenceChecker.cs:250-253` — 部分失敗時 `success` 仍為 `true`~~ ✅ 已修復 (P2)

新增 `RemoveResult` 列舉型態（`Success` / `Partial` / `Failed`），取代原本的 `bool` 回傳值。`RemoveMethodsAsync` 和 `RemoveMethodAsync` 均已更新回傳型別。三個 UI 呼叫端（WinForms、Avalonia、TUI）分別處理 `Partial` 狀態，顯示警告樣式而非成功樣式，且仍會執行重新檢查以更新清單。

---

### 2. ~~`nodesToRemove` 使用 `List<>` 做 `Contains` 重複檢查~~ ✅ 已修復

將 `Dictionary<DocumentId, List<MethodDeclarationSyntax>>` 改為 `Dictionary<DocumentId, HashSet<MethodDeclarationSyntax>>`，所有 5 個 helper 方法的參數型別已一併更新。`HashSet.Add()` 本身即處理重複，原本 4 處手動的 `Contains()` 檢查已移除。

---

### 3. ~~`Core/ReferenceChecker.cs` — 大量程式碼重複~~ ✅ 已修復 (P2)

抽取通用的 `EnumerateMethodsAsync(Solution)` 方法，回傳 `List<(Document, MethodDeclarationSyntax, IMethodSymbol)>`。原本 6 個方法（含 `Check`）中重複的「遍歷 solution → compilation → documents → methods」模式全部改為呼叫此列舉器。

同時修正以下問題：
- `FindAndMarkImplicitInterfaceMethodsForRemoval` 和 `FindAllImplementingMethodsForRemoval` 的 `Compilation` 參數從未被使用，已移除。
- `FindAndMarkInterfaceMethodForRemoval` 改用 `AddNodeToRemove` 取代手動字典插入。
- `Check` 方法改用 `OpenSolutionOrProjectAsync` 取代自行處理 .sln/.csproj 分支。

檔案從 730 行縮減至約 460 行。

---

### 4. 無 `CancellationToken` 支援 (P3)

`Check`、`RemoveMethodsAsync`、`RemoveMethodAsync` 三個公開方法都不接受 `CancellationToken` 參數。大型解決方案的分析可能需要數分鐘，使用者無法取消作業。這也使得 TUI 和 GUI 無法提供「取消檢查」的功能。

**建議：** 為三個公開方法加上 `CancellationToken cancellationToken = default` 參數，在 `foreach` 迴圈中定期檢查 `cancellationToken.IsCancellationRequested`。

---

### 5. ~~`GUI/MainForm.cs:219` — 錯誤提示訊息語言不一致~~ ✅ 已修復 (P3)

將英文訊息 `"Please select a valid .sln/.slnx file."` 改為繁體中文 `"請選擇有效的 .sln/.slnx/.csproj 檔案。"`，並將標題改為 `"無效的檔案"` 與其他 UI 訊息一致。

---

### 6. `TUI/Program.cs:490-495` — `ShowMessage` 同步阻塞 (P3)

```csharp
private static void ShowMessage(string message)
{
    Console.Write(string.Format(MOVE_CURSOR, Console.WindowHeight - 2, 1));
    Console.Write(CLEAR_LINE);
    Console.Write(message);
    Console.ReadKey(true);  // 同步阻塞
}
```

`ShowMessage` 被 `async` 方法呼叫（如 `CheckProjectAsync`、`DeleteSelectedAsync`），其 `Console.ReadKey(true)` 會同步阻塞整個主迴圈。這在 TUI 中是可接受的設計（使用者本來就需要按鍵才能繼續），但若未來需要加入背景任務（如進度更新），此設計會成為障礙。

**建議：** 現階段可不處理。若需支援背景任務，應將按鍵等待改為非阻塞輪詢。

---

### 7. 測試覆蓋率不足 (P2)

目前 `ReferenceCheckerTests.cs` 僅包含 11 個參數驗證測試，**無任何核心功能測試**：

- 無「正確找到孤兒方法」的正面測試
- 無「有引用的方法不被標記」的負面測試
- 無「介面實作方法的刪除」整合測試
- 無「override 鏈的刪除」測試
- 無「Controller/Test 類別排除」的回歸測試

這表示重構或修改核心邏輯時，缺乏安全網。

**建議：** 建立包含小型解決方案的測試 fixture（可用 Roslyn `AdhocWorkspace` 或嵌入式測試專案），至少覆蓋：

1. 基本孤兒方法偵測
2. 有引用的方法不被列入
3. Controller / Test 類別排除
4. 單一方法刪除
5. 介面實作方法連帶刪除

---

### 8. `ReferenceChecker.cs` — 排除清單缺乏擴充性 (P3)

目前的 Controller / Test 排除邏輯是硬編碼在核心方法中：

```csharp
if (symbol.ContainingType.Name.Contains("Controller")) { continue; }
if (symbol.ContainingType.Name.Contains("Test")) { continue; }
if (symbol.Name == "Main") { continue; }
```

使用者無法自訂排除規則。例如有些專案可能需要排除 `ViewModel`、`Handler`、`Middleware` 等模式。

**建議：** 長期可考慮將排除規則提取為可設定的參數（如 `HashSet<string>` 或 regex 清單），讓 `Check` 方法接受選項物件。

---

## 📊 總體評分

| 面向 | 舊版評分 | 本版評分 | 變化說明 |
|------|----------|----------|----------|
| **架構設計** | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | 維持不變，多專案架構清晰 |
| **核心邏輯** | ⭐⭐⭐½ | ⭐⭐⭐⭐ | 排除邏輯已修正，Workspace 診斷已加入 |
| **程式碼品質** | ⭐⭐⭐ | ⭐⭐⭐⭐ | `HashSet` 替換、`ShouldAnalyzeAccessibility` 簡化，重複碼仍需改善 |
| **錯誤處理** | ⭐⭐⭐ | ⭐⭐⭐½ | CLI/Avalonia 錯誤處理改善，Workspace 診斷已加入 |
| **測試覆蓋** | ⭐ | ⭐ | 測試路徑已修正但仍是參數驗證測試為主 |
| **跨平台一致性** | ⭐⭐⭐ | ⭐⭐⭐⭐ | 三個 UI 篩選邏輯已統一，Avalonia 按鈕 bug 已修 |

---

## 🎯 建議優先處理項目

| 優先級 | 問題 | 影響 | 工作量 |
|--------|------|------|--------|
| **P2** | #7 測試覆蓋率不足 | 回歸風險 | 高（需建立測試 fixture） |
| **P2** | ~~#3 程式碼重複~~ ✅ | ~~可維護性~~ | ~~中（抽取列舉器）~~ |
| **P2** | ~~#1 部分失敗語意不明確~~ ✅ | 呼叫端行為預期 | 低（API 調整或文件補充） |
| **P3** | #4 無 CancellationToken | 大型方案無法取消 | 中（需修改 API 簽名） |
| **P3** | ~~#2 List.Contains O(n)~~ ✅ | ~~大量節點時效能~~ | ~~中（型別變更範圍廣）~~ |
| **P3** | ~~#5 WinForms 語言不一致~~ ✅ | 使用者體驗 | 低 |
| **P3** | #6 TUI 同步阻塞 | 未來擴充性 | 低（現階段可不處理） |
| **P3** | #8 排除清單缺乏擴充性 | 功能彈性 | 低（長期規劃） |

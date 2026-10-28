# ZeroReferences 程式碼檢視報告

日期：2026-10-28

檢視範圍：Core、CLI、TUI、WinForms、Avalonia 與測試。

優先應改善刪除安全性與分析完整性。本次未修改原始碼。以下發現來自程式碼檢視，尚未逐項新增重現測試。P1 為優先修正，P2 為後續修正。

## 發現的問題

### 1. [P1] 掃描後修改檔案，可能刪錯方法

位置：[Core/ReferenceChecker.cs](Core/ReferenceChecker.cs)，第 448 行，`IsSameDeclaration()`。

目前只比對專案、文件路徑與 `SpanStart`，沒有驗證簽名或文件版本。若掃描後重新排列方法，另一方法落在相同位置，就可能被誤刪；新增引用後也不會重新檢查。

建議保存文件 checksum，刪除前驗證版本、符號身分與引用狀態；資料過期時要求重新掃描。

### 2. [P1] 直接刪除介面方法，可能留下無法編譯的 explicit implementation

位置：[Core/ReferenceChecker.cs](Core/ReferenceChecker.cs)，第 464 行，`MarkRelatedMethodsForRemoval()`。

選取介面宣告 `IWorker.Execute()` 時，相關刪除流程沒有呼叫 `FindAllImplementingMethodsForRemoval()`。例如留下 `void IWorker.Execute()`，但介面已不存在該成員，就會破壞編譯。

建議完整計算介面、實作與 override 的影響範圍，先顯示實際變更清單，並驗證修改後沒有新增編譯錯誤再寫入。

### 3. [P1] 專案載入不完整時，仍可能回報可刪除的方法

位置：[Core/ReferenceChecker.cs](Core/ReferenceChecker.cs)，第 532 行，`CreateWorkspace()`。

Workspace 診斷只寫入 `Debug`。若某個引用端專案載入失敗，掃描仍可能繼續，將缺少引用的結果呈現為正常分析結果。

建議回傳結構化診斷與「分析是否完整」狀態；有載入失敗時停用刪除功能，清楚顯示原因。

### 4. [P2] WinForms 非同步事件缺乏例外處理

位置：[GUI/MainForm.cs](GUI/MainForm.cs)，第 159、254 行。

掃描與刪除事件都是 `async void`，只有 `try/finally`。遇到無效方案、檔案權限或寫入錯誤，例外會離開事件處理常式，進入未處理例外流程。

建議參照 Avalonia 版本加入 `catch`，顯示錯誤並恢復一致的畫面狀態。

### 5. [P2] TUI 游標可移到畫面外，但清單不會捲動

位置：[TUI/Program.cs](TUI/Program.cs)，第 274 行。

清單永遠從索引 0 開始繪製，游標卻能移到整份結果的末端。超過一頁後，使用者看不到目前項目，仍可勾選它。

建議加入可視範圍起點，隨游標捲動，並顯示目前位置與總筆數。

### 6. [P2] CLI 執行失敗仍回傳成功退出碼

位置：[CLI/Program.cs](CLI/Program.cs)，第 39 行。

捕捉例外後只輸出文字，`Main` 正常結束。CI 或腳本無法透過退出碼辨識分析失敗。

建議改為 `Task<int>`，錯誤輸出至 stderr，參數錯誤或分析失敗回傳非零值。

## 其他改善建議

- **分析規則：**`Main` 名稱一律排除會漏掉一般同名方法；應辨識真正入口點。Controller／Test 類別名稱包含判斷也應逐步改成可設定的語意規則。
- **效能：**相關方法刪除多次呼叫 `EnumerateMethodsAsync()`，反覆遍歷整個方案。每次操作建立一次索引即可；單筆刪除也可共用批次實作。
- **測試：**優先補上檔案變動後刪除、直接刪除介面宣告、跨專案引用、載入失敗與刪除後編譯驗證。目前部分錯誤測試只排除 `NullReferenceException`，不足以確認預期行為。

## 驗證結果與限制

- 現有 22 個測試全部通過（`dotnet test Tests/Tests.csproj -v minimal`）。
- 全方案建置（`dotnet build ZeroReferences.slnx -v minimal`）在 Avalonia 遙測寫入時遇到沙箱權限錯誤，因此未完成；這不是已確認的專案編譯缺陷。
- 驗證時將 `DOTNET_CLI_HOME` 設為可寫入的暫存目錄，以避開預設使用者目錄的寫入限制。
- 未執行桌面 UI 互動測試。

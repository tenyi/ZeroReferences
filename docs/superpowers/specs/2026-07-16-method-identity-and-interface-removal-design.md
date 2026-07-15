# 方法識別與介面刪除安全性設計

## 目標

修正孤兒方法刪除流程的兩個安全性問題：

1. 不同專案中具有相同完整 namespace、型別與方法簽名的宣告，不得因字串相同而被同時刪除。
2. 刪除介面相關方法時，只能連帶刪除 Roslyn 確認的真正介面實作，不得把同名 helper 當成實作。

`.csproj` 的既有行為維持不變：開啟專案時仍掃描該專案及其相依專案。

## 設計

### 結果與宣告識別

Core 將掃描結果由 `List<string>` 改為公開的結果模型。每筆結果包含：

- `Signature`：現有完整方法簽名，仍包含 namespace。
- `ProjectPath`：方法所屬專案檔案的完整路徑。
- `DocumentPath`：方法所在原始檔的完整路徑。
- `SpanStart`：方法宣告在該文件中的 Roslyn 起始位置。

`ToString()` 只負責提供使用者可讀的簽名；刪除流程使用上述位置欄位定位宣告，不再用簽名字串對整個解決方案做全域比對。相同簽名的結果在 UI 中會附帶專案或檔案資訊，讓使用者能辨識選取項目。

### 刪除流程

`RemoveMethodAsync` 與 `RemoveMethodsAsync` 接收結果模型，依 `ProjectPath`、`DocumentPath` 與 `SpanStart` 找到唯一語法節點，再沿用現有的 Roslyn `TrackNodes` 與單一 `TryApplyChanges` 流程。介面與 override 的連帶刪除仍由語意符號決定，不改變既有使用者確認流程。

### 介面實作判定

`FindAndMarkImplicitInterfaceMethodsForRemoval` 與 `FindAllImplementingMethodsForRemoval` 改用 `ITypeSymbol.FindImplementationForInterfaceMember`。只有回傳符號與待處理方法符號相等時，才視為真正的隱含實作；不再依賴名稱、參數數量與參數型別的近似比對。Explicit implementation 仍以 `ExplicitInterfaceImplementations` 判定。

## 測試設計（先測試）

### 跨專案相同簽名

建立兩個專案，各自宣告相同 namespace、類別與方法簽名。測試應確認：

- `Check` 回傳兩筆可區分的結果，且每筆保留不同專案位置。
- 將其中一筆傳給 `RemoveMethodAsync` 後，該專案的方法被刪除，另一專案的方法仍存在。

### 非實作同名 helper

建立 `IWorker.Execute()`、`BaseWorker : IWorker` 的 `public Execute()`，以及 `DerivedWorker : BaseWorker` 中被另一個方法呼叫的 `private Execute()`。刪除 base 方法後，測試應確認 derived 的 private helper 仍留在原始碼中，避免產生編譯錯誤。

兩個測試都先在既有實作上執行並確認失敗，再加入最小 Core 修正；最後執行完整 `Tests` 測試集。

## 不在範圍內

- 不改變 `.csproj` 對相依專案的全掃描行為。
- 不重新設計孤兒方法的判定規則、Controller/Test 排除規則或 accessibility 篩選規則。
- 不手動修改 `MainForm.Designer.cs`。

# ZeroReferences 專案開發指南 (CLAUDE.md)

## 🎯 專案目標

- 本專案是一個基於 **.NET 10 Windows Forms** 的桌面應用程式。其主要目的是增強軟體開發的「程式碼健康度檢查」能力，具體目標是**分析一個 .NET 解決方案 (.sln/.slnx)，並找出其中所有定義為 `public` 且在整個解決方案中沒有被其他程式碼引用的方法**（即「孤兒方法」）。


## 💻 技術堆疊 (Tech Stack)
*   **語言**: C#
*   **框架**: .NET 10 (Windows Forms)
*   **核心分析引擎**: 使用 `Microsoft.CodeAnalysis` (Roslyn) 組合式 API。
*   **UI**: 傳統 WinForms 介面。

## 🧱 專案架構概覽

### 核心檔案（3 個）

| 檔案 | 職責 | 備註 |
|------|------|------|
| **`MainForm.cs`** | UI 互動、選擇 .sln 檔案、顯示結果 | 目前主要 UI，透過 Designer 檔案掛載控制項 |
| **`ReferenceChecker.cs`** | 核心分析引擎，使用 Roslyn 逐一檢查方法引用次數 | `Check()` 為靜態 async 方法，回傳 `List<string>` |
| **`ModalDialog.cs`** | 長時作業期間的等待提示框 | `ShowInTaskbar = false`，避免在工作列出現 |

### 附屬檔案

- **`Program.cs`**：應用程式 entry point，建立並執行 `MainForm`。
- **`SolutionDialog.cs`**：早期版本的 UI（已被 `MainForm` 取代），現為備用，可刪除。
- **`MainForm.Designer.cs`**：由 WinForms 設計工具產生，定義 UI 控制項配置，**請勿手動編輯**。

### 依賴套件（NuGet）

- `Microsoft.CodeAnalysis`（Roslyn）：用於符號查找與語法樹分析。

## 🔍 分析邏輯（ReferenceChecker.Check）

1. `MSBuildWorkspace.OpenSolutionAsync()` 開啟整個 .sln
2. 遍歷每個專案的文件，收集所有 `MethodDeclarationSyntax` 節點
3. 對每個 `public` 方法，呼叫 `SymbolFinder.FindReferencesAsync()` 計算引用次數
4. 引用數 = 0 且方法名稱不含 `Controller`/`Test` 者，判定為「孤兒方法」

### 排除邏輯
- `Controller`：MVC/Web API 控制器方法，通常由路由呼叫，不計入
- `Test`：測試類別方法，測試本身的引用不視為實際使用

## 🔧 建置與執行指令

```bash
# 建置（使用 slnx 格式）
dotnet build ZeroReferences.slnx /p:Configuration=Debug /p:Platform="Any CPU"

# 清除建置產出
dotnet clean ZeroReferences.slnx /p:Configuration=Debug /p:Platform="Any CPU"

# 執行（位於 obj\Debug\net10.0-windows8.0\ZeroReferences.exe）
dotnet run --project ZeroReferences
```
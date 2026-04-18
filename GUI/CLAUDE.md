# ZeroReferences 專案開發指南 (CLAUDE.md)

## 🎯 專案目標

- 本專案是一個基於 **.NET 10 Windows Forms** 的桌面應用程式。分析 .NET 解決方案 (.sln/.slnx)，找出所有 `public` / `private` / `protected` 且未被引用的方法（孤兒方法）。

## 💻 技術堆疊 (Tech Stack)
*   **語言**: C#
*   **框架**: .NET 10 (Windows Forms)
*   **核心分析引擎**: 引用 `ZeroReferences.Core`（Roslyn 組合式 API）
*   **UI**: 傳統 WinForms 介面，支援存取層級篩選（全部 / public / private / protected）

## 🧱 專案架構概覽

### 原始檔

| 檔案 | 職責 | 備註 |
|------|------|------|
| **`MainForm.cs`** | UI 互動、選擇 .sln 檔案、顯示結果、移除方法 | 主要 UI，透過 Designer 檔案掛載控制項 |
| **`ModalDialog.cs`** | 長時作業期間的等待提示框 | `ShowInTaskbar = false` |
| **`Program.cs`** | 應用程式 entry point | 建立並執行 `MainForm` |
| **`MainForm.Designer.cs`** | WinForms 設計工具產生 | **請勿手動編輯** |

### 依賴

- 引用 `ZeroReferences.Core`（內含 Roslyn 分析引擎 `ReferenceChecker`）
- NuGet: `Humanizer.Core`, `Microsoft.Bcl.AsyncInterfaces`, `Microsoft.Extensions.DependencyInjection`, `Microsoft.Extensions.Logging`, `System.Composition`

## 🔧 建置與執行指令

```bash
# 建置（Linux/macOS 需啟用 Windows Targeting）
dotnet build /p:EnableWindowsTargeting=true

# 執行（僅 Windows）
dotnet run
```

# ZeroReferences 專案開發指南 (CLAUDE.md)

## 🎯 專案目標

- 本專案是一個基於 **.NET 10** 的應用程式。主要目的是**分析 .NET 解決方案 (.sln/.slnx)，找出其中所有 `public`/`private`/`protected` 方法中，在整個解決方案中沒有被其他程式碼引用的方法**（即「孤兒方法」）。

## 🏗️ 專案架構

本專案採用多專案架構，共用核心程式碼：

| 目錄 | 類型 | 說明 |
|------|------|------|
| **`Core/`** | Class Library | 核心分析引擎（ReferenceChecker），所有 UI 專案共用 |
| **`GUI/`** | WinForms App | Windows 桌面版（僅 Windows） |
| **`AvaloniaGUI/`** | Avalonia App | 跨平台桌面版（Windows / macOS / Linux） |
| **`TUI/`** | Console App | 終端機版本（純 Console 實作，無外部 TUI 框架） |
| **`CLI/`** | Console App | 簡化命令列版本 |

### Core 程式庫 (`ZeroReferences.Core`)

**核心類別：** `ZeroReferences.ReferenceChecker`

| 方法 | 說明 |
|------|------|
| `Check(string solutionPath)` | 分析解決方案，回傳未參照方法清單 |
| `RemoveMethodsAsync(string path, List<string> signatures)` | 批次刪除多個方法 |
| `RemoveMethodAsync(string path, string signature)` | 刪除單一方法 |

**依賴：**
- Microsoft.CodeAnalysis 5.3.0 (Roslyn)
- Microsoft.CodeAnalysis.Workspaces.MSBuild 5.3.0

### UI 專案

所有 UI 專案都參考 `Core`，直接使用 `ReferenceChecker` 類別。

## 💻 技術堆疊 (Tech Stack)

- **語言**: C#
- **框架**: .NET 10
- **核心分析引擎**: Microsoft.CodeAnalysis (Roslyn) 組合式 API
- **UI 框架**: WinForms (GUI)、Avalonia (AvaloniaGUI)、純 Console ANSI (TUI)、基本 Console (CLI)

## 🔍 分析邏輯（ReferenceChecker.Check）

1. `MSBuildWorkspace.OpenSolutionAsync()` 開啟整個 .sln
2. 遍歷每個專案的文件，收集所有 `MethodDeclarationSyntax` 節點
3. 對每個 `public`/`private`/`protected` 方法，呼叫 `SymbolFinder.FindReferencesAsync()` 計算引用次數
4. 引用數 = 0 且方法名稱不含 `Controller`/`Test` 者，判定為「孤兒方法」

### 排除邏輯
- `Controller`：MVC/Web API 控制器方法，通常由路由呼叫，不計入
- `Test`：測試類別方法，測試本身的引用不視為實際使用

## 🔧 建置與執行指令

```bash
# 建置整個方案
dotnet build ZeroReferences.sln

# 清除建置產出
dotnet clean ZeroReferences.sln

# 執行 CLI（需指定 solution 路徑）
dotnet run --project CLI <solution_path>

# 執行 AvaloniaGUI（跨平台）
dotnet run --project AvaloniaGUI

# 執行 GUI（僅 Windows）
dotnet run --project GUI

# 執行 TUI（終端機版本，互動式操作）
dotnet run --project TUI
```

### TUI 操作說明

| 按鍵 | 功能 |
|------|------|
| `Enter` | 當路徑為空時進入輸入模式；已有路徑時執行檢查 |
| `1/2/3/4` | 篩選：全部 / Public / Private / Protected |
| `↑/↓` | 移動選取 |
| `Tab` 或 `空白鍵` | 切換選取狀態 |
| `E` 或 `Enter` | 刪除選取的方法 |
| `Q` 或 `Esc` | 離開 |

### 各專案個別建置

```bash
# Core 程式庫
dotnet build Core/ZeroReferences.Core.csproj

# CLI
dotnet build CLI/ZeroReferences.csproj

# AvaloniaGUI
dotnet build AvaloniaGUI/AvaloniaGUI.csproj

# TUI
dotnet build TUI/ZeroReferences.TUI.csproj

# GUI（需要 Windows 或啟用跨平台建置）
dotnet build GUI/ZeroReferences.csproj
```

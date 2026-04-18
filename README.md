# ZeroReferences

> 找出 .NET 解決方案中那些「沒有人在乎」的方法 — 孤兒方法檢測工具

[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/)
[![License: BSD-3-Clause](https://img.shields.io/badge/License-BSD--3--Clause-green.svg)](LICENSE)

## 📖 簡介

ZeroReferences 是一款基於 **Roslyn** (Microsoft.CodeAnalysis) 的 .NET 工具，能夠掃描解決方案 (.sln/.slnx) 或單一專案 (.csproj)，找出所有 `public`/`private`/`protected` 方法中，**在整個方案內部完全沒有被引用**的「孤兒方法」。

所謂「孤兒方法」：

- 沒有其他程式碼呼叫
- 不含 `Controller`（MVC/Web API 控制器方法，由路由呼叫）
- 不含 `Test`（測試類別方法）

---

## ✨ 功能特色

- **多介面選擇**：CLI 模式（命令列）、TUI 模式（互動式終端機）、GUI 模式（WinForms）、AvaloniaGUI（跨平台）
- **完整方案分析**：一次掃描整個 .sln/.slnx 中的所有專案，或直接分析單一 .csproj
- **安全刪除**：可單一或批次移除孤兒方法
- **跨平台支援**：Windows / macOS / Linux 皆可執行

---

## 🏗️ 專案架構

```
┌─────────────────────────────────────────────────────────┐
│                        進入點                            │
│  CLI │ TUI │ GUI (WinForms) │ AvaloniaGUI (跨平台)       │
└──────────────┬──────────────────────────────────────────┘
               │ 參照
               ▼
┌─────────────────────────────────────────────────────────┐
│                    Core (類別庫)                         │
│                 ReferenceChecker                        │
│              核心分析引擎 — 所有 UI 專案共用                │
└─────────────────────────────────────────────────────────┘
```

| 專案 | 類型 | 說明 |
|------|------|------|
| **Core** | Class Library | 核心分析引擎 (`ReferenceChecker`) |
| **CLI** | Console App | 簡化命令列版本 |
| **TUI** | Console App | 互動式終端機版本（ANSI 色彩） |
| **GUI** | WinForms App | Windows 桌面版（僅 Windows） |
| **AvaloniaGUI** | Avalonia App | 跨平台桌面版（Windows/macOS/Linux） |
| **Tests** | Unit Test Project | 單元測試 |

---

## 🔧 安裝與建置

### 前置需求

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) 或更高版本

### 建置指令

```bash
# 建置整個方案
dotnet build 
# 也可以輸入
dotnet build ZeroReferences.slnx
# 在 Windows 若有錯誤訊息，輸入
dotnet build -c Release /p:Platform="Any CPU"
# 清除建置產出
dotnet clean ZeroReferences.slnx
# 精簡輸入
dotnet clean
# 在 Windows 若有錯誤訊息，輸入
dotnet clean -c Release /p:Platform="Any CPU"
```

---

## 🚀 執行方式

### CLI 模式（命令列）

```bash
# 分析整個解決方案
dotnet run --project CLI <solution_path>

# 分析單一專案
dotnet run --project CLI <project_path>
```

### AvaloniaGUI（跨平台桌面版）

```bash
dotnet run --project AvaloniaGUI
```

### GUI（僅 Windows）

```bash
dotnet run --project GUI
```

### TUI（互動式終端機）

```bash
dotnet run --project TUI
```

---

## 🎮 TUI 操作說明

| 按鍵 | 功能 |
|------|------|
| `Enter` | 路徑為空時進入輸入模式；已有路徑時執行檢查 |
| `1/2/3/4` | 篩選：全部 / Public / Private / Protected |
| `↑/↓` | 移動選取 |
| `Tab` 或 `空白鍵` | 切換選取狀態 |
| `E` 或 `Enter` | 刪除選取的方法 |
| `Q` 或 `Esc` | 離開 |

---

## 🔍 分析邏輯

`ReferenceChecker.Check()` 的執行流程：

```
1. 根據副檔名決定開啟方式：
   - .sln/.slnx → MSBuildWorkspace.OpenSolutionAsync()
   - .csproj → MSBuildWorkspace.OpenProjectAsync()
2. 遍歷每個專案的文件
3. 收集所有 MethodDeclarationSyntax 節點
4. 對每個 public/private/protected 方法：
   → SymbolFinder.FindReferencesAsync() 計算引用次數
5. 引用數 = 0 且方法名稱不含 Controller/Test → 判定為孤兒方法
```

### 排除規則

| 關鍵字 | 原因 |
|--------|------|
| `Controller` | MVC/Web API 控制器方法，通常由路由呼叫，不計入 |
| `Test` | 測試類別方法，測試本身的引用不視為實際使用 |

---

## 📦 技術堆疊

| 技術 | 版本 |
|------|------|
| 語言 | C# |
| 框架 | .NET 10 |
| 分析引擎 | Microsoft.CodeAnalysis (Roslyn) |
| UI | WinForms、 Avalonia、Console (ANSI) |

---

## 📄 授權

本專案採用 [BSD 3-Clause License](LICENSE)。


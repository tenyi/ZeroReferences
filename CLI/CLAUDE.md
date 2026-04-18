# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 專案概述

ZeroReferencesCli 是一個**純命令列 (Console)** 工具，使用 Roslyn 引擎分析 `.sln/.slnx` 檔案，找出其中未被引用的 `public` / `private` / `protected` 方法。這是 `ZeroReferences`（WinForms 版）的 CLI 變體。

## 架構

本專案僅有單一原始檔：
- **`Program.cs`**: 包含所有邏輯 — 使用 `MSBuildWorkspace` 載入 solution，透過 `SymbolFinder.FindReferencesAsync` 計算每個 `public` / `private` / `protected` 方法的引用次數，最後輸出引用次數為 0 且名稱不含 "Controller" / "Test" 的方法。

## 與 WinForms 版的差異

| | ZeroReferencesCli (本專案) | ZeroReferences (WinForms) |
|---|---|---|
| OutputType | `Exe` | `WinExe` |
| TargetFramework | `net10.0` | `net10.0-windows8.0` |
| UI | Console 輸出 | Windows Forms |
| Roslyn 版本 | 5.3.0 | 4.14.0 |
| 額外套件 | Humanizer.Core, DI, Logging, Composition | 無 |
| 方法名稱格式 | `SymbolDisplayFormat`（含存取修飾詞、完整限定名稱） | `SymbolDisplayFormat`（含存取修飾詞、完整限定名稱） |

## 建置與執行

```bash
dotnet build
dotnet run
```

目前 `Program.cs` 中的 solution 路徑是硬編碼的，執行前需確認路徑正確。

## 技術棧

- .NET 10.0 (Console App)
- Microsoft.CodeAnalysis 5.3.0 (Roslyn)
- Microsoft.CodeAnalysis.Workspaces.MSBuild 5.3.0

## 🔧 建置指令 (Build Commands)
```bash
# 建置專案
dotnet build ZeroReferences.slnx /p:Configuration=Debug /p:Platform="Any CPU"

# 清除建置產出
dotnet clean ZeroReferences.slnx /p:Configuration=Debug /p:Platform="Any CPU"
```
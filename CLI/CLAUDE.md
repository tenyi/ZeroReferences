# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 專案概述

ZeroReferences CLI 是一個**純命令列 (Console)** 工具，分析 `.sln/.slnx` 檔案中未被引用的 `public` / `private` / `protected` 方法（孤兒方法）。

## 架構

本專案僅有單一原始檔：
- **`Program.cs`**: 進入點，呼叫 Core 程式庫的 `ReferenceChecker.Check()` 進行分析，輸出結果到 Console。

核心分析邏輯全在 `Core/ReferenceChecker.cs`，本專案不直接引用 Roslyn 套件。

## 建置與執行

```bash
# 建置
dotnet build

# 執行（需指定 solution 路徑）
dotnet run -- <solution_path>
```

## 技術棧

- .NET 10.0 (Console App)
- 引用 `ZeroReferences.Core`（內含 Roslyn 分析引擎）

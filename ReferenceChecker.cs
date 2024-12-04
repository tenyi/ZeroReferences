using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;

namespace ZeroReferences
{
    public static class ReferenceChecker
    {
        public static async Task<List<string>> Check(string solutionPath)
        {
            List<string> list = new List<string>();
            if (string.IsNullOrEmpty(solutionPath))
            {
                throw new ArgumentException("Solution path cannot be null or empty.");
            }
            if(!Path.GetExtension(solutionPath).Equals(".sln", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Solution path must have a .sln extension.");
            }
            if(!File.Exists(solutionPath))
            {
                throw new ArgumentException("Solution file does not exist.");
            }
            var workspace = MSBuildWorkspace.Create();
            var solution = await workspace.OpenSolutionAsync(solutionPath);
            Dictionary<string, int> methodReferenceCounts = new Dictionary<string, int>();

            foreach (var project in solution.Projects)
            {
                var compilation = await project.GetCompilationAsync();
                if (compilation == null) { throw new ArgumentException("並沒有任何 compilation"); }
                var methods = new List<MethodDeclarationSyntax>();
               
                foreach (var document in project.Documents)
                {
                    var syntaxTree = await document.GetSyntaxTreeAsync();
                    if (syntaxTree != null)
                    {
                        var root = await syntaxTree.GetRootAsync();
                        methods.AddRange(root.DescendantNodes().OfType<MethodDeclarationSyntax>());
                    }
                }

                foreach (var method in methods)
                {
                    var model = compilation.GetSemanticModel(method.SyntaxTree);
                    var symbol = model.GetDeclaredSymbol(method) as IMethodSymbol;
                    if (symbol == null) { throw new ArgumentException("並沒有任何 symbol"); }
                    // 移除只檢查 public 方法的限制
                    if (symbol != null && symbol.DeclaredAccessibility == Microsoft.CodeAnalysis.Accessibility.Public)
                    {
                        var references = await SymbolFinder.FindReferencesAsync(symbol, solution);
                        // 扣除方法宣告本身的計數
                        var referenceCount = references.Sum(r => r.Locations.Count());// - 1;
                                                                                      // 使用完整的方法識別名稱作為 key
                        string name = $"{symbol.ContainingType.ToDisplayString()}.{symbol.Name}";
                        if(name.Contains("Controller"))
                        {
                            continue;
                        }
                        if(name.Contains("Test"))
                        {
                            continue;
                        }
                        if (methodReferenceCounts.ContainsKey(name))
                        {
                            methodReferenceCounts[name] += referenceCount;
                        }
                        else
                        {
                            methodReferenceCounts[name] = referenceCount;
                        }
                        if (referenceCount == 0)
                        {
                            list.Add(name);
                            Console.WriteLine($"Method '{name}' has no references.");
                        }
                    }
                }
            }

            //foreach (var item in methodReferenceCounts)
            //{
            //    list.Add(item.Key);
            //    if (item.Value == 0 && !item.Key.Contains("Controller"))
            //    {
            //        Console.WriteLine($"Method '{item.Key}' has no references.");
            //    }
            //    //else
            //    //{
            //    //    Console.WriteLine($"Method '{item.Key}' has {item.Value} references.");
            //    //}
            //}

            return list;
        }
    }
}

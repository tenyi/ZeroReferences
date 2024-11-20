using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax; 
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;

namespace ZeroReferences
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var workspace = MSBuildWorkspace.Create();
            var solution = await workspace.OpenSolutionAsync(@"C:\Git\Attendance\Sinotech.Mis.HR.Attendance.AttendanceCard\AttendanceCard.sln");
            Dictionary<string, int> methodReferenceCounts = new Dictionary<string, int>();

            foreach (var project in solution.Projects)
            {
                var compilation = await project.GetCompilationAsync();
                if (compilation == null) { return; }
                var methods = new List<MethodDeclarationSyntax>();
                if (methods == null) { return; }
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

                    if (symbol != null && symbol.DeclaredAccessibility == Accessibility.Public)
                    {
                        var references = await SymbolFinder.FindReferencesAsync(symbol, solution);
                        var referenceCount = references.Sum(r => r.Locations.Count());
                        string name = $"{symbol.ContainingType.Name}.{symbol.Name}";
                        if (methodReferenceCounts.ContainsKey(name))
                        {

                            methodReferenceCounts[name] += referenceCount;
                        }
                        else
                        {
                            methodReferenceCounts[name] = referenceCount;
                            //methodReferenceCounts.Add(symbol.Name,referenceCount);
                        }
                        //if(referenceCount == 0)
                        //{
                        //    Console.WriteLine($"Method '{symbol.Name}' in '{symbol.ContainingType}' has no references.");
                        //}
                        //else
                        //{
                        //    methodReferenceCounts[symbol.Name] = referenceCount;
                        //}
                        //  Console.WriteLine($"Method '{symbol.Name}' in '{symbol.ContainingType}' has {referenceCount} references.");
                    }
                }
            }

            foreach (var item in methodReferenceCounts)
            {
                if (item.Value == 0 && !item.Key.Contains("Controller"))
                {
                    Console.WriteLine($"Method '{item.Key}' has no references.");
                }
                //else
                //{
                //    Console.WriteLine($"Method '{item.Key}' has {item.Value} references.");
                //}
            }
        }
    }
}

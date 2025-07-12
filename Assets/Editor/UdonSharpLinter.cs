using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UdonSharpLinter
{
    public class UdonSharpLinter : AssetPostprocessor
    {
        private class LintError
        {
            public string FilePath { get; set; }
            public int Line { get; set; }
            public int Column { get; set; }
            public string Message { get; set; }
            public DiagnosticSeverity Severity { get; set; }
        }

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (string assetPath in importedAssets)
            {
                if (assetPath.EndsWith(".cs") && IsUdonSharpScript(assetPath))
                {
                    LintUdonSharpFile(assetPath);
                }
            }
        }

        private static bool IsUdonSharpScript(string filePath)
        {
            try
            {
                string content = File.ReadAllText(filePath);
                return content.Contains("UdonSharpBehaviour") && 
                       content.Contains("using UdonSharp;");
            }
            catch
            {
                return false;
            }
        }

        private static void LintUdonSharpFile(string filePath)
        {
            try
            {
                string content = File.ReadAllText(filePath);
                var tree = CSharpSyntaxTree.ParseText(content, path: filePath);
                var root = tree.GetRoot();
                
                var errors = new List<LintError>();
                
                // Check for various UdonSharp restrictions
                CheckTryCatchStatements(root, filePath, errors);
                CheckThrowStatements(root, filePath, errors);
                CheckLocalFunctions(root, filePath, errors);
                CheckObjectInitializers(root, filePath, errors);
                CheckMultidimensionalArrays(root, filePath, errors);
                CheckConstructors(root, filePath, errors);
                CheckGenericMethods(root, filePath, errors);
                CheckStaticFields(root, filePath, errors);
                CheckNestedTypes(root, filePath, errors);
                CheckNetworkCallableMethods(root, filePath, errors);
                
                // Report errors
                foreach (var error in errors)
                {
                    string message = $"UdonSharp Lint: {error.Message}";
                    if (error.Severity == DiagnosticSeverity.Error)
                    {
                        Debug.LogError($"{message}\n{error.FilePath}:{error.Line}", 
                            AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(error.FilePath));
                    }
                    else
                    {
                        Debug.LogWarning($"{message}\n{error.FilePath}:{error.Line}", 
                            AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(error.FilePath));
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error linting UdonSharp file {filePath}: {e.Message}");
            }
        }

        private static void CheckTryCatchStatements(SyntaxNode root, string filePath, List<LintError> errors)
        {
            var tryStatements = root.DescendantNodes().OfType<TryStatementSyntax>();
            foreach (var tryStatement in tryStatements)
            {
                var lineSpan = tryStatement.GetLocation().GetLineSpan();
                errors.Add(new LintError
                {
                    FilePath = filePath,
                    Line = lineSpan.StartLinePosition.Line + 1,
                    Column = lineSpan.StartLinePosition.Character + 1,
                    Message = "Try/Catch/Finally statements are not supported in UdonSharp",
                    Severity = DiagnosticSeverity.Error
                });
            }
        }

        private static void CheckThrowStatements(SyntaxNode root, string filePath, List<LintError> errors)
        {
            var throwStatements = root.DescendantNodes()
                .Where(n => n is ThrowStatementSyntax || n is ThrowExpressionSyntax);
            
            foreach (var throwStatement in throwStatements)
            {
                var lineSpan = throwStatement.GetLocation().GetLineSpan();
                errors.Add(new LintError
                {
                    FilePath = filePath,
                    Line = lineSpan.StartLinePosition.Line + 1,
                    Column = lineSpan.StartLinePosition.Character + 1,
                    Message = "Throw statements are not supported in UdonSharp",
                    Severity = DiagnosticSeverity.Error
                });
            }
        }

        private static void CheckLocalFunctions(SyntaxNode root, string filePath, List<LintError> errors)
        {
            var localFunctions = root.DescendantNodes().OfType<LocalFunctionStatementSyntax>();
            foreach (var localFunction in localFunctions)
            {
                var lineSpan = localFunction.GetLocation().GetLineSpan();
                errors.Add(new LintError
                {
                    FilePath = filePath,
                    Line = lineSpan.StartLinePosition.Line + 1,
                    Column = lineSpan.StartLinePosition.Character + 1,
                    Message = "Local functions are not supported in UdonSharp",
                    Severity = DiagnosticSeverity.Error
                });
            }
        }

        private static void CheckObjectInitializers(SyntaxNode root, string filePath, List<LintError> errors)
        {
            var initializers = root.DescendantNodes()
                .Where(n => n is ObjectCreationExpressionSyntax || 
                           n is ArrayCreationExpressionSyntax ||
                           n is ImplicitArrayCreationExpressionSyntax)
                .Where(n => n.DescendantNodes().Any(child => 
                    child is InitializerExpressionSyntax init && 
                    (init.Kind() == SyntaxKind.ObjectInitializerExpression ||
                     init.Kind() == SyntaxKind.CollectionInitializerExpression)));

            foreach (var initializer in initializers)
            {
                var lineSpan = initializer.GetLocation().GetLineSpan();
                errors.Add(new LintError
                {
                    FilePath = filePath,
                    Line = lineSpan.StartLinePosition.Line + 1,
                    Column = lineSpan.StartLinePosition.Character + 1,
                    Message = "Object/Collection initializers are not supported in UdonSharp",
                    Severity = DiagnosticSeverity.Error
                });
            }
        }

        private static void CheckMultidimensionalArrays(SyntaxNode root, string filePath, List<LintError> errors)
        {
            var arrayTypes = root.DescendantNodes().OfType<ArrayTypeSyntax>();
            foreach (var arrayType in arrayTypes)
            {
                if (arrayType.RankSpecifiers.Any(rs => rs.Sizes.Count > 1))
                {
                    var lineSpan = arrayType.GetLocation().GetLineSpan();
                    errors.Add(new LintError
                    {
                        FilePath = filePath,
                        Line = lineSpan.StartLinePosition.Line + 1,
                        Column = lineSpan.StartLinePosition.Character + 1,
                        Message = "Multidimensional arrays are not supported in UdonSharp. Use jagged arrays instead",
                        Severity = DiagnosticSeverity.Error
                    });
                }
            }
        }

        private static void CheckConstructors(SyntaxNode root, string filePath, List<LintError> errors)
        {
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
                .Where(c => IsUdonSharpBehaviourClass(c));

            foreach (var classDecl in classes)
            {
                var constructors = classDecl.Members.OfType<ConstructorDeclarationSyntax>();
                foreach (var constructor in constructors)
                {
                    var lineSpan = constructor.GetLocation().GetLineSpan();
                    errors.Add(new LintError
                    {
                        FilePath = filePath,
                        Line = lineSpan.StartLinePosition.Line + 1,
                        Column = lineSpan.StartLinePosition.Character + 1,
                        Message = "Constructors are not supported in UdonSharpBehaviour",
                        Severity = DiagnosticSeverity.Error
                    });
                }
            }
        }

        private static void CheckGenericMethods(SyntaxNode root, string filePath, List<LintError> errors)
        {
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
                .Where(c => IsUdonSharpBehaviourClass(c));

            foreach (var classDecl in classes)
            {
                var genericMethods = classDecl.Members.OfType<MethodDeclarationSyntax>()
                    .Where(m => m.TypeParameterList != null);
                
                foreach (var method in genericMethods)
                {
                    var lineSpan = method.GetLocation().GetLineSpan();
                    errors.Add(new LintError
                    {
                        FilePath = filePath,
                        Line = lineSpan.StartLinePosition.Line + 1,
                        Column = lineSpan.StartLinePosition.Character + 1,
                        Message = "Generic methods are not supported in UdonSharpBehaviour",
                        Severity = DiagnosticSeverity.Error
                    });
                }
            }
        }

        private static void CheckStaticFields(SyntaxNode root, string filePath, List<LintError> errors)
        {
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
                .Where(c => IsUdonSharpBehaviourClass(c));

            foreach (var classDecl in classes)
            {
                var staticFields = classDecl.Members.OfType<FieldDeclarationSyntax>()
                    .Where(f => f.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)) &&
                               !f.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword)));
                
                foreach (var field in staticFields)
                {
                    var lineSpan = field.GetLocation().GetLineSpan();
                    errors.Add(new LintError
                    {
                        FilePath = filePath,
                        Line = lineSpan.StartLinePosition.Line + 1,
                        Column = lineSpan.StartLinePosition.Character + 1,
                        Message = "Static fields are not supported in UdonSharpBehaviour (const is allowed)",
                        Severity = DiagnosticSeverity.Error
                    });
                }
            }
        }

        private static void CheckNestedTypes(SyntaxNode root, string filePath, List<LintError> errors)
        {
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
                .Where(c => IsUdonSharpBehaviourClass(c));

            foreach (var classDecl in classes)
            {
                var nestedTypes = classDecl.Members
                    .Where(m => m is TypeDeclarationSyntax);
                
                foreach (var nestedType in nestedTypes)
                {
                    var lineSpan = nestedType.GetLocation().GetLineSpan();
                    errors.Add(new LintError
                    {
                        FilePath = filePath,
                        Line = lineSpan.StartLinePosition.Line + 1,
                        Column = lineSpan.StartLinePosition.Character + 1,
                        Message = "Nested types are not supported in UdonSharpBehaviour",
                        Severity = DiagnosticSeverity.Error
                    });
                }
            }
        }

        private static void CheckNetworkCallableMethods(SyntaxNode root, string filePath, List<LintError> errors)
        {
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
                .Where(m => m.AttributeLists.Any(al => 
                    al.Attributes.Any(a => a.Name.ToString().Contains("NetworkCallable"))));

            foreach (var method in methods)
            {
                // Check return type
                if (method.ReturnType.ToString() != "void")
                {
                    var lineSpan = method.ReturnType.GetLocation().GetLineSpan();
                    errors.Add(new LintError
                    {
                        FilePath = filePath,
                        Line = lineSpan.StartLinePosition.Line + 1,
                        Column = lineSpan.StartLinePosition.Character + 1,
                        Message = "NetworkCallable methods must return void",
                        Severity = DiagnosticSeverity.Error
                    });
                }

                // Check parameter count
                if (method.ParameterList.Parameters.Count > 8)
                {
                    var lineSpan = method.ParameterList.GetLocation().GetLineSpan();
                    errors.Add(new LintError
                    {
                        FilePath = filePath,
                        Line = lineSpan.StartLinePosition.Line + 1,
                        Column = lineSpan.StartLinePosition.Character + 1,
                        Message = "NetworkCallable methods cannot have more than 8 parameters",
                        Severity = DiagnosticSeverity.Error
                    });
                }

                // Check for ref/out parameters
                var refOutParams = method.ParameterList.Parameters
                    .Where(p => p.Modifiers.Any(m => 
                        m.IsKind(SyntaxKind.RefKeyword) || m.IsKind(SyntaxKind.OutKeyword)));
                
                foreach (var param in refOutParams)
                {
                    var lineSpan = param.GetLocation().GetLineSpan();
                    errors.Add(new LintError
                    {
                        FilePath = filePath,
                        Line = lineSpan.StartLinePosition.Line + 1,
                        Column = lineSpan.StartLinePosition.Character + 1,
                        Message = "NetworkCallable methods cannot have ref/out parameters",
                        Severity = DiagnosticSeverity.Error
                    });
                }

                // Check for params
                var paramsParams = method.ParameterList.Parameters
                    .Where(p => p.Modifiers.Any(m => m.IsKind(SyntaxKind.ParamsKeyword)));
                
                foreach (var param in paramsParams)
                {
                    var lineSpan = param.GetLocation().GetLineSpan();
                    errors.Add(new LintError
                    {
                        FilePath = filePath,
                        Line = lineSpan.StartLinePosition.Line + 1,
                        Column = lineSpan.StartLinePosition.Character + 1,
                        Message = "NetworkCallable methods cannot have params parameters",
                        Severity = DiagnosticSeverity.Error
                    });
                }

                // Check for default values
                var defaultParams = method.ParameterList.Parameters
                    .Where(p => p.Default != null);
                
                foreach (var param in defaultParams)
                {
                    var lineSpan = param.GetLocation().GetLineSpan();
                    errors.Add(new LintError
                    {
                        FilePath = filePath,
                        Line = lineSpan.StartLinePosition.Line + 1,
                        Column = lineSpan.StartLinePosition.Character + 1,
                        Message = "NetworkCallable methods cannot have parameters with default values",
                        Severity = DiagnosticSeverity.Error
                    });
                }

                // Check modifiers
                var invalidModifiers = new[] 
                {
                    SyntaxKind.StaticKeyword, SyntaxKind.AbstractKeyword, 
                    SyntaxKind.VirtualKeyword, SyntaxKind.OverrideKeyword, 
                    SyntaxKind.SealedKeyword
                };

                foreach (var modifier in method.Modifiers.Where(m => invalidModifiers.Contains(m.Kind())))
                {
                    var lineSpan = modifier.GetLocation().GetLineSpan();
                    errors.Add(new LintError
                    {
                        FilePath = filePath,
                        Line = lineSpan.StartLinePosition.Line + 1,
                        Column = lineSpan.StartLinePosition.Character + 1,
                        Message = "NetworkCallable methods cannot be static, abstract, virtual, override, or sealed",
                        Severity = DiagnosticSeverity.Error
                    });
                }
            }
        }

        private static bool IsUdonSharpBehaviourClass(ClassDeclarationSyntax classDecl)
        {
            // Check if the class inherits from UdonSharpBehaviour
            if (classDecl.BaseList != null)
            {
                return classDecl.BaseList.Types
                    .Any(t => t.Type.ToString().Contains("UdonSharpBehaviour"));
            }
            return false;
        }
    }

    [InitializeOnLoad]
    public static class UdonSharpLinterMenu
    {
        static UdonSharpLinterMenu()
        {
            EditorApplication.delayCall += () =>
            {
                Debug.Log("UdonSharp Linter initialized. It will automatically check UdonSharp scripts when they are imported.");
            };
        }

        [MenuItem("Tools/UdonSharp/Lint All Scripts")]
        public static void LintAllUdonSharpScripts()
        {
            var guids = AssetDatabase.FindAssets("t:Script", new[] { "Assets" });
            int lintedCount = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".cs"))
                {
                    var content = File.ReadAllText(path);
                    if (content.Contains("UdonSharpBehaviour") && content.Contains("using UdonSharp;"))
                    {
                        UdonSharpLinter.OnPostprocessAllAssets(
                            new[] { path }, 
                            new string[0], 
                            new string[0], 
                            new string[0]);
                        lintedCount++;
                    }
                }
            }

            Debug.Log($"Linted {lintedCount} UdonSharp scripts.");
        }
    }
}
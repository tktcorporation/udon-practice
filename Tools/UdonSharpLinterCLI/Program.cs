using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UdonSharpLinterCLI
{
    class Program
    {
        private static int _errorCount = 0;
        private static int _warningCount = 0;
        private static bool _hasErrors = false;

        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: UdonSharpLinterCLI <directory_path> [--exclude-test-scripts]");
                return 1;
            }

            string directoryPath = args[0];
            if (!Directory.Exists(directoryPath))
            {
                Console.Error.WriteLine($"Error: Directory '{directoryPath}' does not exist.");
                return 1;
            }

            bool excludeTestScripts = args.Length > 1 && args[1] == "--exclude-test-scripts";

            Console.WriteLine($"[UdonSharp Linter] Scanning directory: {directoryPath}");
            
            var csFiles = Directory.GetFiles(directoryPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\Temp\\") && !f.Contains("\\Library\\") && !f.Contains("\\obj\\") && !f.Contains("\\bin\\"))
                .Where(f => !f.Contains("\\Editor\\") && !f.Contains("\\editor\\")) // Exclude Editor scripts
                .Where(f => !excludeTestScripts || (!f.Contains("\\TestScripts\\") && !f.Contains("\\Tests\\") && !f.Contains("\\Test\\"))) // Optionally exclude test scripts
                .Where(IsUdonSharpScript)
                .ToList();

            if (!csFiles.Any())
            {
                Console.WriteLine("[UdonSharp Linter] No UdonSharp scripts found.");
                return 0;
            }

            Console.WriteLine($"[UdonSharp Linter] Found {csFiles.Count} UdonSharp scripts to check.");

            foreach (var file in csFiles)
            {
                LintFile(file);
            }

            Console.WriteLine($"\n[UdonSharp Linter] Summary: {_errorCount} errors, {_warningCount} warnings");
            
            return _hasErrors ? 1 : 0;
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

        private static void LintFile(string filePath)
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
                CheckUnexposedAPIs(root, filePath, errors);
                
                // Report errors
                foreach (var error in errors)
                {
                    string severityPrefix = error.Severity == DiagnosticSeverity.Error ? "error" : "warning";
                    string relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), error.FilePath).Replace('\\', '/');
                    Console.WriteLine($"{relativePath}({error.Line},{error.Column}): {severityPrefix} UDON{error.Code:D3}: {error.Message}");
                    
                    if (error.Severity == DiagnosticSeverity.Error)
                    {
                        _errorCount++;
                        _hasErrors = true;
                    }
                    else
                    {
                        _warningCount++;
                    }
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error processing file {filePath}: {e.Message}");
                _hasErrors = true;
            }
        }

        private class LintError
        {
            public string FilePath { get; set; } = "";
            public int Line { get; set; }
            public int Column { get; set; }
            public string Message { get; set; } = "";
            public DiagnosticSeverity Severity { get; set; }
            public int Code { get; set; }
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
                    Severity = DiagnosticSeverity.Error,
                    Code = 1
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
                    Severity = DiagnosticSeverity.Error,
                    Code = 2
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
                    Severity = DiagnosticSeverity.Error,
                    Code = 3
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
                    Severity = DiagnosticSeverity.Error,
                    Code = 7
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
                        Severity = DiagnosticSeverity.Error,
                        Code = 8
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
                        Severity = DiagnosticSeverity.Error,
                        Code = 5
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
                        Severity = DiagnosticSeverity.Error,
                        Code = 6
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
                        Severity = DiagnosticSeverity.Error,
                        Code = 11
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
                        Severity = DiagnosticSeverity.Error,
                        Code = 12
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
                        Severity = DiagnosticSeverity.Error,
                        Code = 13
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
                        Severity = DiagnosticSeverity.Error,
                        Code = 13
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
                        Severity = DiagnosticSeverity.Error,
                        Code = 13
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
                        Severity = DiagnosticSeverity.Error,
                        Code = 13
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
                        Severity = DiagnosticSeverity.Error,
                        Code = 13
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
                        Severity = DiagnosticSeverity.Error,
                        Code = 13
                    });
                }
            }
        }

        private static void CheckUnexposedAPIs(SyntaxNode root, string filePath, List<LintError> errors)
        {
            // TextMeshPro未公開APIのリスト
            var unexposedTextMeshProAPIs = new HashSet<string>
            {
                "fontSize", "fontSizeMin", "fontSizeMax", "fontStyle", "fontWeight",
                "enableAutoSizing", "fontSharedMaterial", "fontSharedMaterials", 
                "fontMaterial", "fontMaterials", "maskable", "isVolumetricText",
                "margin", "textBounds", "preferredWidth", "preferredHeight",
                "flexibleWidth", "flexibleHeight", "minWidth", "minHeight",
                "maxWidth", "maxHeight", "layoutPriority", "isUsingLegacyAnimationComponent",
                "isVolumetricText", "onCullStateChanged", "maskOffset", "renderMode",
                "geometrySortingOrder", "vertexBufferAutoSizeReduction", "firstVisibleCharacter",
                "maxVisibleCharacters", "maxVisibleWords", "maxVisibleLines", "useMaxVisibleDescender",
                "pageToDisplay", "linkedTextComponent", "isTextOverflowing", "firstOverflowCharacterIndex",
                "isTextTruncated", "parseCtrlCharacters", "isOrthographic", "enableCulling",
                "ignoreVisibility", "horizontalMapping", "verticalMapping", "mappingUvLineOffset",
                "enableWordWrapping", "wordWrapingRatios", "overflowMode", "isTextOverflowing",
                "textInfo", "havePropertiesChanged", "isUsingBold", "spriteAnimator",
                "layoutElement", "ignoreRectMaskCulling", "isOverlay"
            };

            // メンバーアクセス式を検出
            var memberAccesses = root.DescendantNodes().OfType<MemberAccessExpressionSyntax>();
            
            foreach (var memberAccess in memberAccesses)
            {
                // TextMeshProまたはTextMeshProUGUIのインスタンスへのアクセスをチェック
                var memberName = memberAccess.Name.ToString();
                
                if (unexposedTextMeshProAPIs.Contains(memberName))
                {
                    // 親の型がTextMeshProかどうかを確認（簡易的なチェック）
                    var expression = memberAccess.Expression.ToString();
                    
                    // TextMeshProの変数への可能性が高い場合
                    if (expression.ToLower().Contains("textmeshpro") || 
                        expression.ToLower().Contains("tmp") ||
                        expression.ToLower().Contains("text"))
                    {
                        var lineSpan = memberAccess.GetLocation().GetLineSpan();
                        errors.Add(new LintError
                        {
                            FilePath = filePath,
                            Line = lineSpan.StartLinePosition.Line + 1,
                            Column = lineSpan.StartLinePosition.Character + 1,
                            Message = $"Method is not exposed to Udon: '{expression}.{memberName}'",
                            Severity = DiagnosticSeverity.Error,
                            Code = 14
                        });
                    }
                }
            }

            // その他の一般的な未公開APIもチェック
            CheckGeneralUnexposedAPIs(root, filePath, errors);
        }

        private static void CheckGeneralUnexposedAPIs(SyntaxNode root, string filePath, List<LintError> errors)
        {
            // 一般的な未公開メソッド/プロパティのチェック
            var bannedMethods = new Dictionary<string, string>
            {
                { "System.Reflection", "Reflection APIs are not exposed to Udon" },
                { "System.Threading", "Threading APIs are not exposed to Udon" },
                { "System.IO.File", "File I/O APIs are not exposed to Udon" },
                { "System.Net", "Networking APIs are not exposed to Udon" },
                { "UnityEngine.Application.OpenURL", "Application.OpenURL is not exposed to Udon" },
                { "UnityEngine.Application.Quit", "Application.Quit is not exposed to Udon" }
            };

            var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>();
            
            foreach (var invocation in invocations)
            {
                var invocationString = invocation.ToString();
                
                foreach (var bannedMethod in bannedMethods)
                {
                    if (invocationString.Contains(bannedMethod.Key))
                    {
                        var lineSpan = invocation.GetLocation().GetLineSpan();
                        errors.Add(new LintError
                        {
                            FilePath = filePath,
                            Line = lineSpan.StartLinePosition.Line + 1,
                            Column = lineSpan.StartLinePosition.Character + 1,
                            Message = bannedMethod.Value,
                            Severity = DiagnosticSeverity.Error,
                            Code = 14
                        });
                    }
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
}
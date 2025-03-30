using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Xml.Linq;

namespace FindClassesThatDoNotDisposeResource.ValidationException
{
    public class ValidationExceptionDerived1 : ValidationException
    {
        private static readonly List<KeyResult> ExceptionsByContext =
            LoadExceptionsByContextFromJson("ValidationException.json");
        public ValidationExceptionDerived1() 
        {
        }

        public override bool IgnoreException(KeyResult keyResult, SyntaxNode syntaxNode, SemanticModel semanticModel,
            Solution solution)
        {
            if (ExceptionsByContext.Contains(keyResult)) return true;

            if (syntaxNode is InvocationExpressionSyntax)
            {
                InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)syntaxNode;
                
                var symbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;

                if (symbol != null)
                {
                    //var completeSignature = symbol.ToDisplayString(); // Full name of the method
                    //var declarantType = symbol.ContainingType.ToDisplayString(); // Class where it is defined
                    //var reducedSignature = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

                    //Console.WriteLine("Método invocado: " + completeSignature);
                    //Console.WriteLine("Clase: " + declarantType);
                    //Console.WriteLine("Firma reducida: " + reducedSignature);

                    string methodSignature = string.Empty;
                    MethodDeclarationSyntax methodDeclarationSyntax;
                    ConstructorDeclarationSyntax constructorDeclarationSyntax;
                    //LocalFunctionStatementSyntax localFunctionStatementSyntax;
                    PropertyDeclarationSyntax propertyDeclarationSyntax;
                    var originalSyntaxNode = symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();

                    if (originalSyntaxNode == null)
                    {
                        return false;
                    }

                    SyntaxTree syntaxTreeOriginalSyntaxNode = null; 
                    if (symbol is IMethodSymbol)
                    {
                        IMethodSymbol methodSymbol = (IMethodSymbol)symbol;
                        var declaration = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();

                        if (declaration == null)
                        {
                            return false;
                        }

                        syntaxTreeOriginalSyntaxNode = declaration.SyntaxTree;

                        Program.GetMethodSignature(declaration.DescendantNodes().FirstOrDefault(), ref methodSignature, out methodDeclarationSyntax,
                            out constructorDeclarationSyntax, out propertyDeclarationSyntax);

                    }
                    else if (symbol is IPropertySymbol)
                    {
                        IPropertySymbol propertySymbol = (IPropertySymbol)symbol;

                        var declaration = propertySymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
                        if (declaration == null)
                        {
                            return false;
                        }

                        syntaxTreeOriginalSyntaxNode = declaration.SyntaxTree;

                        Program.GetMethodSignature(declaration.DescendantNodes().FirstOrDefault(), ref methodSignature, out methodDeclarationSyntax,
                            out constructorDeclarationSyntax, out propertyDeclarationSyntax);
                    }
                    else if (symbol is ILocalSymbol)
                    {
                        ILocalSymbol localSymbol = (ILocalSymbol)symbol;
                        var decl = localSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
                        if (decl is VariableDeclaratorSyntax)
                        {
                            VariableDeclaratorSyntax variable = (VariableDeclaratorSyntax)decl;
                            var localStatement = variable.Parent?.Parent as LocalDeclarationStatementSyntax;

                            if (localStatement == null)
                            {
                                return false;
                            }

                            syntaxTreeOriginalSyntaxNode = localStatement.SyntaxTree;

                            Program.GetMethodSignature(localStatement.DescendantNodes().FirstOrDefault(), ref methodSignature, out methodDeclarationSyntax,
                                out constructorDeclarationSyntax, out propertyDeclarationSyntax);
                        }
                    }


                    if (string.IsNullOrEmpty(methodSignature))
                    {
                        return false;
                    }

                    if (syntaxTreeOriginalSyntaxNode == null)
                    {
                        return false;
                    }

                    var document = solution.GetDocument(syntaxTreeOriginalSyntaxNode);
                    if (document == null)
                    {
                        return false;
                    }

                    string file = document.FilePath;
                    var symbolProject = document.Project;

                    int lineNumber = syntaxTreeOriginalSyntaxNode.GetLineSpan(originalSyntaxNode.Span).StartLinePosition.Line + 1;

                    var nameSyntax = originalSyntaxNode.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault()?.Name;
                    string className = originalSyntaxNode.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault()?.Identifier
                        .Text;
                    className = (nameSyntax as IdentifierNameSyntax)?.Identifier.Text + "." + className;
                    KeyResult keyResultToCompare =
                        new KeyResult(symbolProject.Name, file, lineNumber, className, methodSignature);

                    bool aux = ExceptionsByContext.Contains(keyResultToCompare);
                    
                    return ExceptionsByContext.Contains(keyResultToCompare);
                }
            }

            return false;
        }

        private static List<KeyResult> LoadExceptionsByContextFromJson(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return new List<KeyResult>();
                }

                var json = File.ReadAllText(path);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<List<KeyResult>>(json, options) ?? new List<KeyResult>();
            }
            catch
            {
                return new List<KeyResult>();
            }
        }

        public override bool IgnoreExceptionForReturnStatement(IMethodSymbol methodSymbol, Solution solution)
        {
            var syntaxNode = methodSymbol.DeclaringSyntaxReferences.Select(syntaxRef => syntaxRef.GetSyntax())
                .FirstOrDefault();
            if (syntaxNode == null)
            {
                return false;
            }

            var document = solution.GetDocument(syntaxNode.SyntaxTree);
            if (document == null)
            {
                return false;
            }

            var syntaxTree = syntaxNode.SyntaxTree;

            int lineNumber = syntaxTree.GetLineSpan(syntaxNode.Span).StartLinePosition.Line + 1;

            string methodSignature = string.Empty;
            MethodDeclarationSyntax methodDeclarationSyntax;
            ConstructorDeclarationSyntax constructorDeclarationSyntax;
            LocalFunctionStatementSyntax localFunctionStatementSyntax;
            PropertyDeclarationSyntax propertyDeclarationSyntax;
            Program.GetMethodSignature(syntaxNode.DescendantNodes().FirstOrDefault(), ref methodSignature, out methodDeclarationSyntax,
                out constructorDeclarationSyntax, out propertyDeclarationSyntax);

            string file = document.FilePath;
            var symbolProject = document.Project;
            
            var nameSyntax = syntaxNode.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault()?.Name;
            string className = syntaxNode.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault()?.Identifier
                .Text;
            className = (nameSyntax as IdentifierNameSyntax)?.Identifier.Text + "." + className;
            KeyResult keyResultToCompare = 
                new KeyResult(symbolProject.Name, file, lineNumber, className, methodSignature);

            return ExceptionsByContext.Contains(keyResultToCompare);
        }
    }
}

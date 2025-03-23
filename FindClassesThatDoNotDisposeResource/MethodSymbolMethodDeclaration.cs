using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindClassesThatDoNotDisposeResource
{
    class MethodSymbolMethodDeclaration
    {
        private IMethodSymbol MethodSymbol { get; set; }
        public MethodDeclarationSyntax MethodDeclarationSyntax { get; set; }

        public PropertyDeclarationSyntax PropertyDeclarationSyntax { get; set; }

        public string FilePath { get; set; }

        public MethodSymbolMethodDeclaration(IMethodSymbol methodSymbol,
            MethodDeclarationSyntax methodDeclarationSyntax, PropertyDeclarationSyntax propertyDeclarationSyntax, string filePath)
        {
            this.MethodSymbol = methodSymbol;
            this.MethodDeclarationSyntax = methodDeclarationSyntax;
            this.PropertyDeclarationSyntax = propertyDeclarationSyntax;
            this.FilePath = filePath;
        }
    }
}

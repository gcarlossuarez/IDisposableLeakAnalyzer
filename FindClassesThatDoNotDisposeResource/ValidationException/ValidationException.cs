using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FindClassesThatDoNotDisposeResource.ValidationException
{
    public class ValidationException
    {
        public virtual bool IgnoreException(KeyResult keyResult, SyntaxNode syntaxNode, SemanticModel semanticModel,
            Solution solution)
        {
            return false;
        }

        public virtual bool IgnoreExceptionForReturnStatement(IMethodSymbol methodSymbol, Solution solution)
        {
            return false;
        }
    }
}

using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Trident.CodeGeneration.Roslyn
{
    internal class ARMMethodReceiver : ISyntaxReceiver
    {
        internal List<MethodDeclarationSyntax> CandidateMethods { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode node)
        {
            if (node is MethodDeclarationSyntax method &&
                method.Parent is ClassDeclarationSyntax cls &&
                cls.Identifier.Text == "ARM7TDMI" &&
                HasAttribute(method, "ARMParameter") &&
                HasAttribute(method, "ARMGroup"))
            {
                CandidateMethods.Add(method);
            }
        }

        private bool HasAttribute(MethodDeclarationSyntax method, string attributeName)
        {
            foreach (var attrList in method.AttributeLists)
                foreach (var attr in attrList.Attributes)
                    if (attr.Name.ToString().Contains(attributeName))
                        return true;

            return false;
        }
    }
}
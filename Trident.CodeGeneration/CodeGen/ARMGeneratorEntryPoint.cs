using System.Text;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;
using Trident.CodeGeneration.Roslyn;
using Trident.CodeGeneration.Decoding;
using Trident.CodeGeneration.Attributes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Trident.CodeGeneration.CodeGen
{
    [Generator]
    public class ARMGeneratorEntryPoint : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
            => context.RegisterForSyntaxNotifications(() => new ARMMethodReceiver());

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not ARMMethodReceiver receiver)
                return;

            Compilation compilation = context.Compilation;
            var methodContracts = new Dictionary<string, (IMethodSymbol Method, StringBuilder Builder)>();

            foreach (MethodDeclarationSyntax methodDecl in receiver.CandidateMethods)
            {
                SemanticModel semanticModel = compilation.GetSemanticModel(methodDecl.SyntaxTree);
                IMethodSymbol methodSymbol = semanticModel.GetDeclaredSymbol(methodDecl) as IMethodSymbol;
                string methodName = methodSymbol.Name;

                StringBuilder contractBuilder = new();
                contractBuilder.AppendLine(ARMInterfaceGenerator.Generate(methodSymbol));
                methodContracts[methodName] = (methodSymbol, contractBuilder);
            }

            StringBuilder tableBuilder = new();
            tableBuilder.AppendLine(@"namespace Trident.Core.CPU.Decoding.ARM
{
    internal partial class ARMDispatcher<TBus> where TBus : struct, Trident.Core.Bus.IDataBus
    {
        private void InitGeneratedHandlers()
        {");

            HashSet<string> emittedStructs = new HashSet<string>();
            for (uint opcode = 0; opcode < 4096; opcode++)
            {
                uint expandedOpcode = (opcode & 0xFF0) << 16 | (opcode & 0x00F) << 4;

                ARMDecoder.ARMGroup group = ARMDecoder.DetermineARMGroup(expandedOpcode);
                IMethodSymbol method = ARMDecoder.FindGroupMethod(context.Compilation, group);

                if (method is null)
                    continue;

                var values = TraitDecoder.DecodeTraitValues(expandedOpcode, method);
                string structName = ARMTraitStructGenerator.GetPermutationKey(method, values);

                tableBuilder.AppendLine($"            _instructionHandlers[{opcode}] = _cpu.{method.Name}<{structName}>;");

                if (!emittedStructs.Add(structName))
                    continue;

                string structBody = ARMTraitStructGenerator.GenerateTraitStruct(structName, method.Name, ARMAttributeParser.Parse(method), values);
                methodContracts[method.Name].Builder.AppendLine(structBody);
            }

            foreach (var kvp in methodContracts)
            {
                string methodName = kvp.Key;
                StringBuilder builder = kvp.Value.Builder;

                context.AddSource($"{methodName}.g.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
            }

            tableBuilder.AppendLine("        }\n    }\n}");
            SourceText dispatcherLookUpText = SourceText.From(tableBuilder.ToString(), Encoding.UTF8);
            context.AddSource("ARMDispatcher.g.cs", dispatcherLookUpText);
        }
    }
}
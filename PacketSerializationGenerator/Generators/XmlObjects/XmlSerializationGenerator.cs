using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using PacketSerializationGenerator.Objects;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

namespace PacketSerializationGenerator.Generators.XmlObjects;

[Generator]
public class XmlSerializationGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider
            (
                static (s, _) => IsSyntaxTargetForGeneration(s), // Selects classes with attributes
                static (ctx, _) => GetSemanticTargetForGeneration(ctx) // Selects classes with the [XmlSerializable] attribute
            )
            .Where(static m => m is not null)!;

        // Combine the selected classes with the `Compilation`
        IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax>)> compilationAndClasses
            = context.CompilationProvider.Combine(classDeclarations.Collect());

        // Generate the source using the compilation and classes
        context.RegisterSourceOutput(compilationAndClasses,
            static (spc, source) => Execute(source.Item1, source.Item2, spc));
    }

    /// <summary>
    /// Determines whether a syntax node represents a class with at least one attribute.
    /// </summary>
    /// <param name="node">The node to investigate.</param>
    /// <returns>A value indicating whether the node met the requirements.</returns>
    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
        => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 };

    /// <summary>
    /// Determines whether the current node represents a class with the [XmlSerializable] attribute.
    /// </summary>
    /// <param name="context">The generator context.</param>
    /// <returns>The class syntax, or null if the node did not meet the requirements.</returns>
    private static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        // We know the node is a ClassDeclarationSyntax thanks to IsSyntaxTargetForGeneration
        ClassDeclarationSyntax classDeclaration = (ClassDeclarationSyntax)context.Node;

        // Now we check each attribute, looking for a [XmlSerializable] attribute
        foreach (AttributeListSyntax attributeListSyntax in classDeclaration.AttributeLists)
        {
            foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
            {
                if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                    continue;

                INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                string fullName = attributeContainingTypeSymbol.ToDisplayString();

                if (fullName == XmlObjectConstants.XmlSerializableAttributeTypeName)
                    return classDeclaration;
            }
        }

        return null;
    }

    private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes, SourceProductionContext context)
    {
        if (classes.IsDefaultOrEmpty)
            return;

        IEnumerable<ClassDeclarationSyntax> distinctClasses = classes.Distinct();

        List<ClassToAugment> classesToGenerate = GetTypesToGenerate(compilation, distinctClasses, context.CancellationToken);
        foreach (ClassToAugment @class in classesToGenerate)
        {
            string result = XmlSerializationStrings.GenerateXmlSerializationLogic(@class);
            context.AddSource($"{@class.Name}.g.cs", SourceText.From(result, Encoding.UTF8));
        }
    }

    private static List<ClassToAugment> GetTypesToGenerate(Compilation compilation, IEnumerable<ClassDeclarationSyntax> classes, CancellationToken ct)
    {
        List<ClassToAugment> classesToGenerate = new();

        // Get the semantic representation of our marker attribute
        INamedTypeSymbol? xmlSerializableAttribute = compilation.GetTypeByMetadataName(XmlObjectConstants.XmlSerializableAttributeTypeName);

        if (xmlSerializableAttribute == null)
        {
            // If this is null, the compilation couldn't find the marker attribute type
            // which suggests there's something very wrong! Bail out..
            return classesToGenerate;
        }

        foreach (ClassDeclarationSyntax classDeclaration in classes)
        {
            ct.ThrowIfCancellationRequested();

            // Get the semantic representation of the class syntax
            SemanticModel semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol classSymbol)
                continue;

            classesToGenerate.Add(ClassToAugment.FromNamedTypeSymbol(classSymbol));
        }

        return classesToGenerate;
    }
}

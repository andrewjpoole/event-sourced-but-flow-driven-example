using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;

#pragma warning disable RS1038 // Compiler extensions should be implemented in assemblies with compiler-provided references
[Generator]
public class AutoGenerateImmutableDictionyFromConstantsGenerator : IIncrementalGenerator
{

private const string AttributeName = "AutoGenerateImmutableDictionyFromConstantsAttribute";

public const string Attribute = @"
namespace WeatherApp.SourceGenerators;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AutoGenerateImmutableDictionyFromConstantsAttribute : Attribute
{
}
";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Add the marker attribute to the compilation
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "AutoGenerateImmutableDictionyFromConstantsAttribute.g.cs", 
            SourceText.From(Attribute, Encoding.UTF8)));

        // Register a syntax provider to find suitable classes
        IncrementalValuesProvider<ClassInfo?> targets = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsSyntaxTargetForGeneration(node),
                transform: static (context, _) => GetClassInfo(context))
            .Where(static classInfo => classInfo is not null);
       
        // Register the source output
        context.RegisterSourceOutput(targets,
            static (spc, source) => Execute(source, spc));
    }

    private static void Execute(ClassInfo? classInfo, SourceProductionContext context)
    {
        if(classInfo is null)
            return;

        var source = GeneratePartialClass(classInfo);
        context.AddSource($"{classInfo.ClassName}_Generated.g.cs", SourceText.From(source, Encoding.UTF8));
    }

    // Needs to be very fast, no linq.
    static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    => node is ClassDeclarationSyntax m && m.AttributeLists.Count > 0;
    

    private static ClassInfo? GetClassInfo(GeneratorSyntaxContext context)
    {
        // Ensure the symbol is a class and has the desired attribute...
        if (context.Node is ClassDeclarationSyntax classDeclaration &&
            context.SemanticModel.GetDeclaredSymbol(classDeclaration) is INamedTypeSymbol classSymbol &&
            classSymbol.GetAttributes().Any(attr =>
                attr.AttributeClass?.Name == AttributeName))
        {
            // Get the constants and build the dictionary...
            var constants = classSymbol.GetMembers()
                .OfType<IFieldSymbol>()
                .Where(f => f.IsConst && f.Type.SpecialType == SpecialType.System_String)
                .ToImmutableDictionary(f => f.Name, f => f.ConstantValue?.ToString() ?? string.Empty);

            return new ClassInfo
            {
                ClassName = classSymbol.Name,
                NamespaceName = classSymbol.ContainingNamespace.ToDisplayString(),
                Constants = constants
            };
        }

        return null;
    }

    private static string GeneratePartialClass(ClassInfo classInfo)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"using System.Collections.Immutable;");
        sb.AppendLine();
        sb.AppendLine($"namespace {classInfo.NamespaceName};");
        sb.AppendLine();
        sb.AppendLine($"public static partial class {classInfo.ClassName}");
        sb.AppendLine("{");
        sb.AppendLine("    public static readonly IReadOnlyDictionary<string, string> ConstantsDictionary = new Dictionary<string, string>");
        sb.AppendLine("    {");

        foreach (var constant in classInfo.Constants)
        {
            sb.AppendLine($"        {{ \"{constant.Key}\", \"{constant.Value}\" }},");
        }

        sb.AppendLine("    }.ToImmutableDictionary();");
        sb.AppendLine("}");
        return sb.ToString();
    }    

    private class ClassInfo
    {
        public string ClassName { get; set; } = string.Empty;
        public string NamespaceName { get; set; } = string.Empty;
        public ImmutableDictionary<string, string> Constants { get; set; } = ImmutableDictionary<string, string>.Empty;
    }
}
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using System.Linq;

[Generator]
public class EntityNamesSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Register a syntax provider to find the EntityNames class
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsEntityNamesClass(node),
                transform: static (context, _) => GetClassInfo(context))
            .Where(static classInfo => classInfo is not null);

        // Combine all class declarations into a single collection
        var combinedClassDeclarations = classDeclarations.Collect();

        // Register the source output
        context.RegisterSourceOutput(combinedClassDeclarations, static (context, classes) =>
        {
            foreach (var classInfo in classes)
            {
                if (classInfo is not null)
                {
                    var source = GeneratePartialClass(classInfo);
                    context.AddSource($"{classInfo.ClassName}_Generated.cs", SourceText.From(source, Encoding.UTF8));
                }
            }
        });
    }

    private static bool IsEntityNamesClass(SyntaxNode node)
    {
        // Check if the node is a class declaration
        return node is ClassDeclarationSyntax classDeclaration &&
               classDeclaration.Identifier.Text == "EntityNames";
    }

    private static ClassInfo? GetClassInfo(GeneratorSyntaxContext context)
    {
        // Ensure the symbol is a class and matches the desired namespace
        if (context.Node is ClassDeclarationSyntax classDeclaration &&
            context.SemanticModel.GetDeclaredSymbol(classDeclaration) is INamedTypeSymbol classSymbol &&
            classSymbol.Name == "EntityNames" &&
            classSymbol.ContainingNamespace.ToDisplayString() == "WeatherApp.Infrastructure.Messaging")
        {
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
        sb.AppendLine("    public static readonly IReadOnlyDictionary<string, string> EntityNameDictionary = new Dictionary<string, string>");
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
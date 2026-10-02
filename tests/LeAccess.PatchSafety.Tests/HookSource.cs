using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal sealed record CallbackParameter(string Type, string Name);
internal sealed record Hook(string Location, string TypeName, string MethodName, string[] ParameterTypes,
    string[] Namespaces, Dictionary<string, string> Aliases, CallbackParameter[] CallbackParameters);

internal static class HookSource
{
    internal static List<Hook> ReadRepository(string repository)
    {
        var hooks = new List<Hook>();
        foreach (string file in Directory.EnumerateFiles(Path.Combine(repository, "LeAccess"), "*.cs", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(repository, file);
            if (relative.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj")) continue;
            hooks.AddRange(Parse(File.ReadAllText(file), relative));
        }
        if (hooks.Count == 0) throw new InvalidOperationException("No Harmony targets found in production sources.");
        return hooks;
    }

    internal static List<Hook> Parse(string source, string file)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();
        var error = root.GetDiagnostics().FirstOrDefault(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        if (error != null) throw new InvalidOperationException($"{file}: Cannot audit invalid C# syntax: {error.GetMessage()}");
        var hooks = new List<Hook>();
        var namespaces = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Where(node => node.Alias == null && node.StaticKeyword.IsKind(SyntaxKind.None))
            .Select(node => node.Name.ToString()).ToArray();
        var aliases = root.DescendantNodes().OfType<UsingDirectiveSyntax>().Where(node => node.Alias != null)
            .ToDictionary(node => node.Alias.Name.Identifier.Text, node => node.Name.ToString());
        foreach (var attribute in root.DescendantNodes().OfType<AttributeSyntax>().Where(IsPatch))
        {
            if (attribute.Parent?.Parent is not ClassDeclarationSyntax patch)
                throw Invalid(file, attribute, "Only class-level HarmonyPatch declarations are supported.");
            if (patch.AttributeLists.SelectMany(list => list.Attributes).Count(IsPatch) != 1)
                throw Invalid(file, attribute, "Split HarmonyPatch declarations require explicit parser support.");
            var callbacks = patch.Members.OfType<MethodDeclarationSyntax>()
                .Where(method => method.Identifier.Text is "Prefix" or "Postfix").ToArray();
            if (callbacks.Length == 0 || patch.Members.OfType<MethodDeclarationSyntax>()
                .Any(method => method.Identifier.Text is "Transpiler" or "Finalizer"))
                throw Invalid(file, attribute, "Unrecognized patch callback; extend validation explicitly.");
            var parameters = callbacks.SelectMany(method => method.ParameterList.Parameters)
                .Select(parameter => new CallbackParameter(parameter.Type.ToString(), parameter.Identifier.Text)).ToArray();
            var arguments = attribute.ArgumentList?.Arguments.Select(argument => argument.Expression).ToArray();
            if (arguments is { Length: > 0 }) Add(arguments, attribute);
            else
            {
                var providers = patch.Members.OfType<MethodDeclarationSyntax>()
                    .Where(method => method.Identifier.Text is "TargetMethod" or "TargetMethods").ToArray();
                if (providers.Length != 1 || providers[0].Body == null || providers[0].Body.Statements.Count == 0)
                    throw Invalid(file, attribute, "Expected one explicit target provider.");
                foreach (var statement in providers[0].Body.Statements)
                {
                    ExpressionSyntax expression = statement switch
                    {
                        YieldStatementSyntax yield when yield.IsKind(SyntaxKind.YieldReturnStatement) => yield.Expression,
                        ReturnStatementSyntax returned => returned.Expression,
                        _ => null
                    };
                    if (expression is not InvocationExpressionSyntax invocation ||
                        invocation.Expression.ToString() != "AccessTools.Method")
                        throw Invalid(file, statement, "Unrecognized dynamic target; only explicit AccessTools.Method targets are accepted.");
                    Add(invocation.ArgumentList.Arguments.Select(argument => argument.Expression).ToArray(), invocation);
                }
            }

            void Add(ExpressionSyntax[] args, SyntaxNode location)
            {
                if (args.Length < 2 || args[0] is not TypeOfExpressionSyntax type)
                    throw Invalid(file, location, "Target requires typeof and a method name.");
                string name = args[1] switch
                {
                    InvocationExpressionSyntax invoke when invoke.Expression.ToString() == "nameof" &&
                        invoke.ArgumentList.Arguments.Count == 1 &&
                        invoke.ArgumentList.Arguments[0].Expression is MemberAccessExpressionSyntax member => member.Name.Identifier.Text,
                    LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression) => literal.Token.ValueText,
                    _ => throw Invalid(file, location, "Unsupported target method name.")
                };
                List<string> types = null;
                foreach (var arg in args.Skip(2))
                {
                    if (arg is TypeOfExpressionSyntax parameter) (types ??= new()).Add(parameter.Type.ToString());
                    else if (arg.ToString() is "Type.EmptyTypes" or "System.Type.EmptyTypes") types ??= new();
                    else if (arg is MemberAccessExpressionSyntax methodType && methodType.Expression.ToString().EndsWith("MethodType"))
                        name = methodType.Name.Identifier.Text switch
                        {
                            "Setter" => "set_" + name, "Getter" => "get_" + name,
                            _ => throw Invalid(file, arg, "Unsupported Harmony method kind.")
                        };
                    else if (arg is ImplicitArrayCreationExpressionSyntax implicitArray) AddArray(implicitArray.Initializer);
                    else if (arg is ArrayCreationExpressionSyntax array)
                    {
                        if (array.Initializer != null) AddArray(array.Initializer);
                        else if (array.Type.RankSpecifiers.Count == 1 && array.Type.RankSpecifiers[0].Sizes.Count == 1 &&
                            array.Type.RankSpecifiers[0].Sizes[0].ToString() == "0") types ??= new();
                        else throw Invalid(file, arg, "Only explicit parameter arrays or Type[0] are supported.");
                    }
                    else throw Invalid(file, arg, "Unknown target argument; extend validation explicitly.");
                }
                hooks.Add(new Hook(Location(file, location), type.Type.ToString(), name, types?.ToArray(),
                    namespaces, aliases, parameters));

                void AddArray(InitializerExpressionSyntax initializer)
                {
                    types ??= new();
                    foreach (var expression in initializer.Expressions)
                        if (expression is TypeOfExpressionSyntax parameter) types.Add(parameter.Type.ToString());
                        else throw Invalid(file, expression, "Non-type target parameter.");
                }
            }
        }
        // A manual detour must not silently escape the attribute-based audit.
        if (root.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(invocation =>
            invocation.Expression is MemberAccessExpressionSyntax member && member.Name.Identifier.Text == "Patch"))
            throw new InvalidOperationException($"{file}: manual Patch call requires explicit safety audit support.");
        return hooks;
    }

    private static bool IsPatch(AttributeSyntax attribute) =>
        attribute.Name.ToString().Split('.').Last() is "HarmonyPatch" or "HarmonyPatchAttribute";

    internal static void CheckStateTypes(Hook hook)
    {
        var states = hook.CallbackParameters.Where(parameter => parameter.Name == "__state")
            .Select(parameter => StateTypeName(parameter.Type, hook)).Distinct().ToArray();
        if (states.Length > 1) throw new InvalidOperationException("Prefix/Postfix __state types disagree.");
    }

    private static string StateTypeName(string name, Hook hook)
    {
        name = name.Replace("global::", "");
        if (hook.Aliases.TryGetValue(name, out string alias)) name = alias.Replace("global::", "");
        return name switch
        {
            "bool" => "System.Boolean", "byte" => "System.Byte", "sbyte" => "System.SByte",
            "short" => "System.Int16", "ushort" => "System.UInt16", "int" => "System.Int32",
            "uint" => "System.UInt32", "long" => "System.Int64", "ulong" => "System.UInt64",
            "float" => "System.Single", "double" => "System.Double", "decimal" => "System.Decimal",
            "char" => "System.Char", "string" => "System.String", "object" => "System.Object", _ => name
        };
    }

    private static string Location(string file, SyntaxNode node) => $"{file}:{node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}";
    private static Exception Invalid(string file, SyntaxNode node, string reason) =>
        new InvalidOperationException($"{Location(file, node)}: {reason}");
}

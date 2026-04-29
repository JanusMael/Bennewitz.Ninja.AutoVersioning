using System;
using System.Diagnostics.CodeAnalysis;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bennewitz.Ninja.AutoVersioning.SourceGenerators;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public static class AttributeSyntaxFactory
{
    /// <inheritdoc cref="GeneratedCodeAttribute" />
    public static AttributeListSyntax GeneratedCode(string? tool = null, Version? version = null, bool isAssemblyAttribute = false)
    {
        var arguments = new List<AttributeArgumentSyntax>();

        if (tool != null)
        {
            arguments.Add(SyntaxFactory.AttributeArgument(
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(tool))));
        }

        if (version != null)
        {
            arguments.Add(SyntaxFactory.AttributeArgument(
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(version.ToString()))));
        }

        return Attribute<CompilerGeneratedAttribute>(arguments.ToArray(), isAssemblyAttribute);
    }

    /// <inheritdoc cref="CompilerGeneratedAttribute" />
    public static AttributeListSyntax CompilerGenerated(bool isAssemblyAttribute = false) =>
        Attribute<CompilerGeneratedAttribute>(isAssemblyAttribute);

    public static AttributeListSyntax Attribute<T>(bool isAssemblyAttribute)
        where T : Attribute =>
        Attribute<T>((AttributeArgumentSyntax?) null, isAssemblyAttribute);

    public static AttributeListSyntax Attribute<T>(string[] arguments, bool isAssemblyAttribute)
        where T : Attribute =>
        Attribute<T>(
            arguments.Select(x => SyntaxFactory.AttributeArgument(
                SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(x)))).ToArray(),
            isAssemblyAttribute
        );

    public static AttributeListSyntax Attribute<T>(string argument, bool isAssemblyAttribute)
        where T : Attribute =>
        Attribute<T>(
            SyntaxFactory.AttributeArgument(
                SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal(argument))),
            isAssemblyAttribute
        );

    public static AttributeListSyntax Attribute<T>(AttributeArgumentSyntax? argumentSyntax, bool isAssemblyAttribute)
        where T : Attribute =>
        Attribute<T>(
            argumentSyntax == null ? null :
            new[]
            {
                argumentSyntax
            }, isAssemblyAttribute);

    private static AttributeListSyntax Attribute<T>(AttributeArgumentSyntax[]? argumentSyntaxArray, bool isAssemblyAttribute)
        where T : Attribute
    {
        var attributeSyntax = SyntaxFactory.Attribute(
            SyntaxFactory.IdentifierName(typeof(T).Name
                .RemoveSuffix()));

        if (argumentSyntaxArray != null)
        {
            AttributeArgumentListSyntax attributeArgumentListSyntax;

            if (argumentSyntaxArray.Length == 1)
            {
                var argumentSyntax = argumentSyntaxArray[0];
                attributeArgumentListSyntax = SyntaxFactory.AttributeArgumentList(
                    argumentSyntax.ToSeparatedSyntaxList());
            }
            else
            {
                var arguments = new List<SyntaxNodeOrToken>();
                for (int i = 0; i < argumentSyntaxArray.Length; i++)
                {
                    if (i > 0)
                    {
                        arguments.Add(SyntaxFactory.Token(SyntaxKind.CommaToken));
                    }
                    arguments.Add(argumentSyntaxArray[i]);
                }

                attributeArgumentListSyntax = SyntaxFactory.AttributeArgumentList(
                    SyntaxFactory.SeparatedList<AttributeArgumentSyntax>(
                        arguments.ToArray()));
            }

            attributeSyntax = attributeSyntax.WithArgumentList(
                attributeArgumentListSyntax
            );
        }

        var attributeListSyntax = SyntaxFactory.AttributeList(
            attributeSyntax.ToSeparatedSyntaxList());

        if (isAssemblyAttribute)
        {
            attributeListSyntax = attributeListSyntax.WithTarget(
                SyntaxFactory.AttributeTargetSpecifier(
                    SyntaxFactory.Token(SyntaxKind.AssemblyKeyword)
                )
            );
        }

        return attributeListSyntax;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string RemoveSuffix(this string s, string suffix = nameof(Attribute)) =>
        s.EndsWith(suffix, StringComparison.Ordinal) ? s.Remove(s.Length - suffix.Length) : s;
}
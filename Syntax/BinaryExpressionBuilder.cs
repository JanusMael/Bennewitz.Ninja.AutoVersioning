using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bennewitz.Ninja.AutoVersioning.SourceGenerators;
public static class BinaryExpressionBuilder
{
    public static BinaryExpressionSyntax Build(SyntaxKind binaryExpressionSyntaxKind, params ExpressionSyntax[] expressions)
    {
        return (BinaryExpressionSyntax) BuildRecurse(expressions.Reverse().ToArray(), 0, binaryExpressionSyntaxKind);
    }

    private static ExpressionSyntax BuildRecurse(ExpressionSyntax[] expressions, int index, SyntaxKind binaryExpressionOperatorToken)
    {
        if (!binaryExpressionOperatorToken.IsBinaryExpression())
        {
            throw new ArgumentException($"{binaryExpressionOperatorToken} must represent a kind of {nameof(BinaryExpressionSyntax)}",
                nameof(binaryExpressionOperatorToken));
        }
        if (index < 0 || index >= expressions.Length - 1) return expressions[expressions.Length - 1];

        var rightSyntax = expressions[index];

        var leftSyntax = BuildRecurse(expressions, index + 1, binaryExpressionOperatorToken);

        var binaryExpressionSyntax = SyntaxFactory.BinaryExpression(
            binaryExpressionOperatorToken.GetBinaryExpression(),
            leftSyntax,
            rightSyntax);

        return binaryExpressionSyntax;
    }
}
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;

namespace Bennewitz.Ninja.AutoVersioning.SourceGenerators;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public static class SyntaxKindExtensions
{
    public static bool IsKeywordKind(this SyntaxKind kind) =>
        SyntaxFacts.IsKeywordKind(kind);

    public static IEnumerable<SyntaxKind> GetReservedKeywordKinds() =>
        SyntaxFacts.GetReservedKeywordKinds();

    public static IEnumerable<SyntaxKind> GetKeywordKinds() =>
        SyntaxFacts.GetKeywordKinds();

    public static bool IsReservedKeyword(this SyntaxKind kind) =>
        SyntaxFacts.IsReservedKeyword(kind);

    public static bool IsAttributeTargetSpecifier(this SyntaxKind kind) =>
        SyntaxFacts.IsAttributeTargetSpecifier(kind);

    public static bool IsAccessibilityModifier(this SyntaxKind kind) =>
        SyntaxFacts.IsAccessibilityModifier(kind);

    public static bool IsPreprocessorKeyword(this SyntaxKind kind) =>
        SyntaxFacts.IsPreprocessorKeyword(kind);

    public static IEnumerable<SyntaxKind> GetPreprocessorKeywordKinds() =>
        SyntaxFacts.GetPreprocessorKeywordKinds();

    public static bool IsPunctuation(this SyntaxKind kind) =>
        SyntaxFacts.IsPunctuation(kind);

    public static bool IsLanguagePunctuation(this SyntaxKind kind) =>
        SyntaxFacts.IsLanguagePunctuation(kind);

    public static bool IsPreprocessorPunctuation(this SyntaxKind kind) =>
        SyntaxFacts.IsPreprocessorPunctuation(kind);

    public static IEnumerable<SyntaxKind> GetPunctuationKinds() =>
        SyntaxFacts.GetPunctuationKinds();

    public static bool IsPunctuationOrKeyword(this SyntaxKind kind) =>
        SyntaxFacts.IsPunctuationOrKeyword(kind);

    public static bool IsAnyToken(this SyntaxKind kind) =>
        SyntaxFacts.IsAnyToken(kind);

    public static bool IsTrivia(this SyntaxKind kind) =>
        SyntaxFacts.IsTrivia(kind);

    public static bool IsPreprocessorDirective(this SyntaxKind kind) =>
        SyntaxFacts.IsPreprocessorDirective(kind);

    public static bool IsName(this SyntaxKind kind) =>
        SyntaxFacts.IsName(kind);

    public static bool IsPredefinedType(this SyntaxKind kind) =>
        SyntaxFacts.IsPredefinedType(kind);

    public static bool IsTypeSyntax(this SyntaxKind kind) =>
        SyntaxFacts.IsTypeSyntax(kind);

    /// <inheritdoc cref="SyntaxFacts.IsGlobalMemberDeclaration"/>
    public static bool IsGlobalMemberDeclaration(this SyntaxKind kind) =>
        SyntaxFacts.IsGlobalMemberDeclaration(kind);

    public static bool IsTypeDeclaration(this SyntaxKind kind) =>
        SyntaxFacts.IsTypeDeclaration(kind);

    public static bool IsNamespaceMemberDeclaration(this SyntaxKind kind) =>
        SyntaxFacts.IsNamespaceMemberDeclaration(kind);

    public static bool IsAnyUnaryExpression(this SyntaxKind token) =>
        SyntaxFacts.IsAnyUnaryExpression(token);

    public static bool IsPrefixUnaryExpression(this SyntaxKind token) =>
        SyntaxFacts.IsPrefixUnaryExpression(token);

    public static bool IsPrefixUnaryExpressionOperatorToken(this SyntaxKind token) =>
        SyntaxFacts.IsPrefixUnaryExpressionOperatorToken(token);

    public static SyntaxKind GetPrefixUnaryExpression(this SyntaxKind token) =>
        SyntaxFacts.GetPrefixUnaryExpression(token);

    public static bool IsPostfixUnaryExpression(this SyntaxKind token) =>
        SyntaxFacts.IsPostfixUnaryExpression(token);

    public static bool IsPostfixUnaryExpressionToken(this SyntaxKind token) =>
        SyntaxFacts.IsPostfixUnaryExpressionToken(token);

    public static SyntaxKind GetPostfixUnaryExpression(this SyntaxKind token) =>
        SyntaxFacts.GetPostfixUnaryExpression(token);

    public static bool IsUnaryOperatorDeclarationToken(this SyntaxKind token) =>
        SyntaxFacts.IsUnaryOperatorDeclarationToken(token);

    public static bool IsAnyOverloadableOperator(this SyntaxKind kind) =>
        SyntaxFacts.IsAnyOverloadableOperator(kind);

    public static bool IsOverloadableBinaryOperator(this SyntaxKind kind) =>
        SyntaxFacts.IsOverloadableBinaryOperator(kind);

    public static bool IsOverloadableUnaryOperator(this SyntaxKind kind) =>
        SyntaxFacts.IsOverloadableUnaryOperator(kind);

    public static bool IsPrimaryFunction(this SyntaxKind keyword) =>
        SyntaxFacts.IsPrimaryFunction(keyword);

    public static SyntaxKind GetPrimaryFunction(this SyntaxKind keyword) =>
        SyntaxFacts.GetPrimaryFunction(keyword);

    public static bool IsLiteralExpression(this SyntaxKind token) =>
        SyntaxFacts.IsLiteralExpression(token);

    public static SyntaxKind GetLiteralExpression(this SyntaxKind token) =>
        SyntaxFacts.GetLiteralExpression(token);

    public static bool IsInstanceExpression(this SyntaxKind token) =>
        SyntaxFacts.IsInstanceExpression(token);

    public static SyntaxKind GetInstanceExpression(this SyntaxKind token) =>
        SyntaxFacts.GetInstanceExpression(token);

    public static bool IsBinaryExpression(this SyntaxKind token) =>
        SyntaxFacts.IsBinaryExpression(token);

    public static bool IsBinaryExpressionOperatorToken(this SyntaxKind token) =>
        SyntaxFacts.IsBinaryExpressionOperatorToken(token);

    public static SyntaxKind GetBinaryExpression(this SyntaxKind token) =>
        SyntaxFacts.GetBinaryExpression(token);

    public static bool IsAssignmentExpression(this SyntaxKind kind) =>
        SyntaxFacts.IsAssignmentExpression(kind);

    public static bool IsAssignmentExpressionOperatorToken(this SyntaxKind token) =>
        SyntaxFacts.IsAssignmentExpressionOperatorToken(token);

    public static SyntaxKind GetAssignmentExpression(this SyntaxKind token) =>
        SyntaxFacts.GetAssignmentExpression(token);

    public static SyntaxKind GetCheckStatement(this SyntaxKind keyword) =>
        SyntaxFacts.GetCheckStatement(keyword);

    public static SyntaxKind GetAccessorDeclarationKind(this SyntaxKind keyword) =>
        SyntaxFacts.GetAccessorDeclarationKind(keyword);

    public static bool IsAccessorDeclaration(this SyntaxKind kind) =>
        SyntaxFacts.IsAccessorDeclaration(kind);

    public static bool IsAccessorDeclarationKeyword(this SyntaxKind keyword) =>
        SyntaxFacts.IsAccessorDeclarationKeyword(keyword);

    public static SyntaxKind GetSwitchLabelKind(this SyntaxKind keyword) =>
        SyntaxFacts.GetSwitchLabelKind(keyword);

    public static SyntaxKind GetBaseTypeDeclarationKind(this SyntaxKind kind) =>
        SyntaxFacts.GetBaseTypeDeclarationKind(kind);

    public static SyntaxKind GetTypeDeclarationKind(this SyntaxKind kind) =>
        SyntaxFacts.GetTypeDeclarationKind(kind);

    public static SyntaxKind GetKeywordKind(string text) =>
        SyntaxFacts.GetKeywordKind(text);

    public static SyntaxKind GetOperatorKind(string operatorMetadataName) =>
        SyntaxFacts.GetOperatorKind(operatorMetadataName);

    public static bool IsCheckedOperator(string operatorMetadataName) =>
        SyntaxFacts.IsCheckedOperator(operatorMetadataName);

    public static SyntaxKind GetPreprocessorKeywordKind(string text) =>
        SyntaxFacts.GetPreprocessorKeywordKind(text);

    public static IEnumerable<SyntaxKind> GetContextualKeywordKinds() =>
        SyntaxFacts.GetContextualKeywordKinds();

    public static bool IsContextualKeyword(this SyntaxKind kind) =>
        SyntaxFacts.IsContextualKeyword(kind);

    public static bool IsQueryContextualKeyword(this SyntaxKind kind) =>
        SyntaxFacts.IsQueryContextualKeyword(kind);

    public static SyntaxKind GetContextualKeywordKind(string text) =>
        SyntaxFacts.GetContextualKeywordKind(text);

    public static string GetText(this SyntaxKind kind) =>
        SyntaxFacts.GetText(kind);

    public static bool IsTypeParameterVarianceKeyword(this SyntaxKind kind) =>
        SyntaxFacts.IsTypeParameterVarianceKeyword(kind);

    public static bool IsDocumentationCommentTrivia(this SyntaxKind kind) =>
        SyntaxFacts.IsDocumentationCommentTrivia(kind);
}
# AGENTS.md — `Syntax/`

Builds the consumer's `AutoVersionedAssemblyInfo.g.cs` as a Roslyn syntax tree. Compiled into the
analyzer assembly; what it emits is compiled into the consumer's assembly.

| File | What it holds |
|---|---|
| `AssemblyInfoTemplate.cs` | `AssemblyInfoTemplate.Generate`: the usings, the attribute list, and `GetBuildName` for the informational version |
| `AssemblyInfoSyntaxFactory.cs` | One builder per assembly attribute, grouped as `Identity`, `Informational`, `Manifest`, `Behavioral` |
| `AttributeSyntaxFactory.cs` | `Attribute<T>`: an attribute list named after `T` without its `Attribute` suffix |
| `SyntaxExtensions.cs` | `ToSyntaxList`, `ToSeparatedSyntaxList`, `UsingNamespaces`, `GeneratedCodeHeaderComment` |
| `BinaryExpressionBuilder.cs` | Folds expressions into one binary expression; used for `[AssemblyFlags]` |
| `SyntaxKindExtensions.cs` | Extension-method wrappers over `SyntaxFacts` |

## Rules

| Rule | Why | Guarded by |
|---|---|---|
| Build lists with `SyntaxExtensions.ToSyntaxList` / `ToSeparatedSyntaxList`, and render with `ToFullString()`. `RS0030` is suppressed only in `ToSyntaxListImpl` and `ToSeparatedSyntaxListImpl` | `BannedSymbols.txt` bans `SyntaxFactory.SingletonList`, `List`, `SingletonSeparatedList`, `SeparatedList` and `SyntaxNode.ToString` | `RS0030` from Microsoft.CodeAnalysis.BannedApiAnalyzers |
| A `BannedSymbols.txt` entry is a full documentation-comment id, derived from Roslyn's `DocumentationCommentId` | A malformed entry matches nothing and fails silently | Nothing automated |
| Emitted text is printable ASCII plus exactly one U+2665, in `GetBuildName` | These strings surface in consumers' version resources and diagnostic endpoints; anything else is a typo or mojibake | `AssemblyInfoTemplateTests` |
| An attribute added to `GetAttributeList` that the SDK also generates is suppressed in `Build.targets` in the same change | The consumer gets `CS0579` duplicate attributes | Nothing automated |
| An attribute from a namespace other than `System.Reflection`, `System.CodeDom.Compiler` or `System.Runtime.CompilerServices` adds its namespace to the usings in `Generate` | `Attribute<T>` emits the bare type name, so the consumer's compilation cannot resolve it | Nothing automated |
| `CommitSha` blank or whitespace takes the same branch in `GetBuildName` and the `CommitSha` / `GITHUB_SHA` metadata | A CI variable that expanded to blanks would otherwise emit a heart followed by the blanks | `InformationalVersion_WithoutUsableCommitSha_IsBuiltWithHeart` |

# AGENTS.md — `scripts/`

File-based C# apps. Run every one with `dotnet run --file`: from this root, a bare
`dotnet run <file.cs>` binds to `Bennewitz.Ninja.AutoVersioning.csproj` instead.

| Script | What it does |
|---|---|
| `repo-conventions.cs` | Checks, and applies, the family's repository conventions: the documentation in every top-level directory, `.github/repository.json`, and the GitHub settings and rulesets. **A copy**: the canonical file is `templates/bbpkg/scripts/repo-conventions.cs` in Bennewitz.Ninja.Templates |

```bash
dotnet run --file scripts/repo-conventions.cs -- check
dotnet run --file scripts/repo-conventions.cs -- check --admin
dotnet run --file scripts/repo-conventions.cs -- apply --dry-run
```

`check` is what CI's `conventions` job runs; `check --admin` and `apply` need the maintainer's `gh`
login.

## Rules

| Rule | Why | Guarded by |
|---|---|---|
| **`repo-conventions.cs` is never edited here.** It changes in Bennewitz.Ninja.Templates and is copied over whole | Every family repository runs the same enforcer; a local edit is drift | `check --repo` run from Bennewitz.Ninja.Templates |
| This directory stays in `DefaultItemExcludes` in the root csproj | The root project's glob would compile the script into the analyzer, and its `#!` line fails the build with `CS9314` | CI's `build` job |
| A script run from a workflow passes `--file` | The bare form binds to the root project | `ci.yml` |

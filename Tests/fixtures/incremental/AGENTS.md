# AGENTS.md — `Tests/fixtures/incremental/`

A four-project consumer of this package, of the shape `plans/00001` measured: `Core` ← `Model` ←
`App`, and `Leaf` referencing `Model` and `Core`. `IncrementalBuildTests` copies it out of the
repository, completes it, builds it and counts each project's `CoreCompile` decision.

| Rule | Why |
|---|---|
| **Every project file carries a `.template` suffix**, renamed on copy | As `.csproj` they would be found by `scripts/repo-conventions.cs`, which evaluates the repository's projects and would hold these to the family's build properties, and by any IDE or stray restore. The repository already uses this suffix for files MSBuild must not pick up |
| **No `Directory.Build.props` or `Directory.Build.targets` lives here**; the test writes both into the copy | They need the absolute path of this repository's `Build.props` and `Build.targets`, and of the generator assembly the test run built. A committed one would also be imported by these projects while they sit in the repository |
| **The copy goes outside the repository**, under the platform's temp location | Otherwise the root `Directory.Build.props`, `Directory.Packages.props` and `Directory.Build.targets` reach it, and the fixture would measure this repository's build rather than a consumer's |
| `Core` is the project a body edit must recompile alone, and `Leaf` the one nothing references | They are the two cases that separate a held stamp from a per-build one |

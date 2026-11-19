# AGENTS.md — `Tests/fixtures/consumer/`

A one-project consumer that installs `Bennewitz.Ninja.AutoVersioning.Abstractions` **as a package**
and reads its own build's stamp back with `BuildVersion.TryGetFromFile`.
`Packaging/PackagedConsumerTests.cs` packs the project into a local feed, copies this fixture out of
the repository, restores it against that feed alone, builds it and runs it.

⛔ **This fixture exists to be wired the hard way.** `plans/00002` step 4: *"It must consume the
`.nupkg`, not the built DLL."* The defect the plan exists to fix is a packaging defect — the code was
always correct and only the package failed to deliver it — so a `<Reference Include="…dll" />` or a
`ProjectReference` here would pass while proving nothing. The one path reference the fixture does
carry is the **generator**, as `<Analyzer Include="…" />`, which contributes no reference to the
compilation and is how the incremental fixture loads it too.

| Rule | Why |
|---|---|
| **`Consumer.csproj` carries the `.template` suffix**, renamed on copy | As a `.csproj` it would be found by `scripts/repo-conventions.cs`, which evaluates the repository's projects and would hold it to the family's build properties, and by any IDE or stray restore. Its `PackageReference` would also fail to restore in place, the feed existing only during a test run |
| **No `Directory.Build.props`, `Directory.Build.targets` or `NuGet.config` lives here**; the test writes all three into the copy | Each needs an absolute path that exists only during the run: this repository's `Build.props` and `Build.targets`, the generator assembly the test run built, and the feed the package was packed into |
| **The package version is `$(AbstractionsPackageVersion)`**, written into the generated `Directory.Build.props` | It is unique per run, so NuGet's cache can never serve an earlier run's package in place of the one under test — the failure that would make a mutation look green |
| **The copy goes outside the repository**, under the platform's temp location | Otherwise the root `Directory.Build.props`, `Directory.Packages.props` and `Directory.Build.targets` reach it, and the fixture would measure this repository's build rather than a consumer's |
| `Program.cs` returns a non-zero exit code when the stamp does not read back | The test reads both the exit code and the `CONSUMER-*` lines, so a silent empty answer cannot pass for success |

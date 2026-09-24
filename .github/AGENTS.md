# AGENTS.md — `.github/`

This repository's workflows and settings.

| File | What it is |
|---|---|
| `workflows/ci.yml` | On push and pull request to `main`. `build`: `dotnet build` at the root, then `dotnet test` on the test project. `conventions`: `repo-conventions check` |
| `workflows/release.yml` | `publish`, on a `v*.*.*` tag or a dispatch with a `version`: build and pack with that version, log in to nuget.org, push, then `gh release create` with the `.nupkg` attached |
| `repository.json` | This repository's description, topics and required checks, read by `scripts/repo-conventions.cs` |
| `copilot-instructions.md` | A pointer to the root `AGENTS.md` |

## Rules

| Rule | Why |
|---|---|
| **The job names `build` and `conventions` are required checks, listed in `repository.json` under `requiredChecks`.** Renaming one means updating that list and running `repo-conventions apply` in the same change | A required check that no job reports blocks every pull request, and GitHub never says why |
| **`release.yml` keeps its file name** | The release authenticates by trusted publishing: `NuGet/login` exchanges the job's OIDC token (`id-token: write`) for a short-lived API key, under a nuget.org policy that names the owner, the repository and this workflow file. There is no long-lived API key; the `NUGET_USER` secret is the nuget.org account the policy belongs to |
| **Dispatching `release.yml` publishes.** `version` is required and there is no dry run | A version on nuget.org can never be replaced |
| The release runs no tests; tag only a commit on which `build` passed | `publish` goes from build to push with nothing in between |
| `dotnet test` is not passed `--nologo` | Microsoft.Testing.Platform rejects it and reports zero tests run |
| Every `dotnet run` of a script passes `--file` | From this root the bare form binds to the generator project |

# AGENTS.md — `.github/`

This repository's workflows and settings.

| File | What it is |
|---|---|
| `workflows/ci.yml` | On push and pull request to `main`. `build`: `dotnet build` at the root, `dotnet test` on the test project, then `dotnet pack` and `assert-packages.cs` against `packages.push` / `packages.local`. `conventions`: `repo-conventions check` |
| `workflows/release.yml` | `publish`, on a `v*.*.*` tag or a dispatch with a `version`: build and pack with that version, assert the packed ids against `packages.push` / `packages.local`, log in to nuget.org, push each id in `packages.push` by name, then `gh release create` with those `.nupkg` attached. Dispatched with `version` blank, it logs in and lists the ids a tag would publish, and publishes nothing |
| `repository.json` | This repository's description, topics and required checks, read by `scripts/repo-conventions.cs` |
| `copilot-instructions.md` | A pointer to the root `AGENTS.md` |

## Rules

| Rule | Why |
|---|---|
| **The job names `build` and `conventions` are required checks, listed in `repository.json` under `requiredChecks`.** Renaming one means updating that list and running `repo-conventions apply` in the same change | A required check that no job reports blocks every pull request, and GitHub never says why |
| **`release.yml` keeps its file name** | The release authenticates by trusted publishing: `NuGet/login` exchanges the job's OIDC token (`id-token: write`) for a short-lived API key, under a nuget.org policy that names the owner, the repository and this workflow file. There is no long-lived API key; the `NUGET_USER` secret is the nuget.org account the policy belongs to |
| **Dispatching `release.yml` with a `version` publishes.** Leave it blank for the credential preflight, which publishes nothing | A version on nuget.org can never be replaced |
| **`dotnet nuget push` names each package; it never takes a `*.nupkg` glob.** The ids come from `packages.push` at the root, and `assert-packages.cs` fails the run on a packed id that file and `packages.local` do not declare | A glob publishes whatever pack happened to produce, permanently |
| The release runs no tests; tag only a commit on which `build` passed | `publish` goes from build to push with nothing in between |
| `dotnet test` is not passed `--nologo` | Microsoft.Testing.Platform rejects it and reports zero tests run |
| Every `dotnet run` of a script passes `--file` | From this root the bare form binds to the generator project |

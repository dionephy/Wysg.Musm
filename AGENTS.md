# Repository Guidelines

## Project Structure & Module Organization
Solution code is grouped by runtime. `apps/` contains runnable hosts such as the ASP.NET API (`Wysg.Musm.Api`), editor playgrounds, and service shims; run them via their `.csproj`. Shared business logic lives under `src/` (Domain, UseCases, Infrastructure, RuleEngine, and Windows UI automation helpers). `.NET` tests live in `tests/Wysg.Musm.Tests`, while the native integration shim sits in `cpp/Wysg.Musm.Llama`. Data seeds and tooling scripts are under `db/`, `legacy/`, and `workers/embeddings/` (Python-based embedding job).

## Build, Test, and Development Commands
Use `dotnet restore Wysg.Musm.sln` after pulling new dependencies. `dotnet build Wysg.Musm.sln -c Release` validates the full solution; prefer `-c Debug` for local iteration. Run API locally with `dotnet run --project apps/Wysg.Musm.Api`. Execute managed tests using `dotnet test Wysg.Musm.sln`. The embedding worker requires `python -m venv .venv && .venv/Scripts/activate && pip install -r workers/embeddings/requirements.txt`, then `python workers/embeddings/embed_phrases.py`.

## Coding Style & Naming Conventions
Follow standard .NET conventions: 4-space indentation, `PascalCase` for classes, records, and public members, `camelCase` for locals and parameters, and suffix async operations with `Async`. Keep files nullable-enabled (`<Nullable>enable</Nullable>`) and lean on dependency injection over singletons. For Python, stick to PEP 8 and guard entry-points with `if __name__ == "__main__":`.

## Testing Guidelines
Tests live alongside the solution in the `tests` folder and use xUnit with FluentAssertions. Name test methods `MethodUnderTest_ExpectedResult_Context` to clarify intent. Ensure new features include unit coverage, especially around text parsing helpers. Prefer `dotnet test --filter Category=Fast` (add `[Trait]` attributes) during iteration, and run the full `dotnet test Wysg.Musm.sln` before opening a PR.

## Commit & Pull Request Guidelines
Recent commits are short, action-focused phrases (e.g., `ongoing on completion window down key issue`); continue using concise present-tense summaries under ~60 characters. Group related changes per commit, and include co-authors or ticket numbers when relevant. Pull requests should describe the user-facing impact, note any migrations or environment variable changes, and attach screenshots for UI updates. Cross-link tracking issues and confirm tests (`dotnet test`, worker smoke checks) in the checklist.

## Environment & Configuration Tips
Application settings live in `apps/*/appsettings*.json`; avoid committing secrets and override via user secrets or environment variables. Sample database snapshots and `.sql` seeds under `db/` and project root support local restore; document versions when updating them. The embedding worker ships with `.env.example`—copy to `.env` and fill API keys before running.

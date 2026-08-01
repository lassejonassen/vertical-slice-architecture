# Contributing

Thanks for your interest in contributing! This is a small, single-maintainer
template project, so the process is kept lightweight.

By participating, you're expected to follow the [Code of Conduct](CODE_OF_CONDUCT.md).
Please report security vulnerabilities per [SECURITY.md](SECURITY.md) instead
of opening a public issue.

## Getting started

See the [README](README.md) for prerequisites, running the app, and the
project structure. In short:

```bash
docker compose up postgres -d
dotnet run --project src/VerticalSliceArchitecture.Api
dotnet test
```

## Before opening a PR

- Keep changes focused — one logical change per PR is easier to review than
  a large mixed one.
- Follow the existing vertical-slice pattern for new features (see
  [README § Adding a new vertical slice](README.md#adding-a-new-vertical-slice)):
  a command/query, handler, optional validator, and `IEndpoint`, all under
  `Features/<Area>/<Feature>/`. Avoid introducing horizontal layers
  (controllers/services/repositories) that cut across slices.
- Add or update tests for behavior you change — unit tests in
  `VerticalSliceArchitecture.Api.Tests` for logic, integration tests in
  `VerticalSliceArchitecture.Api.IntegrationTests` for endpoint/DB behavior.
- Run `dotnet test` locally; CI (`.github/workflows/ci.yml`) runs the same
  build and both test projects on every push and PR, and must be green
  before merging.
- If your change touches the EF Core model, include the generated migration
  (see [README § Database](README.md#database)) rather than leaving the
  model and migrations out of sync.
- Match the existing code style in the file(s) you're touching. There's no
  enforced formatter yet, so consistency with surrounding code is the bar.

## Commit messages

Write commit messages that explain *why*, not just *what* — the diff already
shows what changed. No strict format is enforced, but dependency bumps from
Dependabot use a `chore(deps): ...` prefix; feel free to follow that
convention for similar changes.

## Opening a PR

- Describe what the change does and why, and link any relevant issue.
- Keep PRs rebased on the latest `master` if conflicts come up.
- Since this is maintained by one person, review may take a few days —
  thanks for your patience.

## Reporting bugs / suggesting features

Open a GitHub issue with steps to reproduce (for bugs) or the motivating use
case (for feature requests). For security issues, use
[SECURITY.md](SECURITY.md) instead.

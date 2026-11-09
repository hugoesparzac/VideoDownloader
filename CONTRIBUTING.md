# Contributing to Video Downloader

Thanks for your interest in improving Video Downloader! This guide explains how to set up the project, how the code is organized, and how to get a change merged.

## Ways to contribute

- **Report a bug** or a site that stopped working.
- **Suggest a feature** or an improvement.
- **Improve the docs** (README, this guide, translations of the UI).
- **Send a pull request** with a fix or a new feature.

For anything larger than a small fix, please open an issue first so we can agree on the approach before you spend time on it.

## Reporting bugs

Before opening an issue:

1. Press **Update tools** in the app. yt-dlp and gallery-dl break regularly when sites change, and an update fixes most "it stopped working" reports.
2. Check that the problem is not the sign-in requirement. Some content needs browser cookies (see the README). On Windows, Chrome, Edge and Brave cannot be read, so use Firefox.

Then include in your issue:

- App version (shown in the window title) and operating system.
- What you did, what you expected and what happened.
- The URL, if it is public. Do not share private links or anything that includes your account information.
- The error message shown in the app.
- If the app closed on its own, the crash log:
  - Windows: `%LOCALAPPDATA%\VideoDownloader\logs\crash.log`
  - Linux and macOS: `~/.local/share/VideoDownloader/logs/crash.log`

Please review logs before posting them and remove personal data such as your user name in file paths.

## Suggesting features

Open an issue describing the problem you want to solve, not only the solution. Keep in mind the scope of the project: it is a friendly front end for yt-dlp and gallery-dl for personal use. Changes meant to bypass DRM or access controls will not be accepted.

## Development setup

### Requirements

- The .NET SDK version specified in `global.json`.
- Git.
- Linux, Windows or macOS.

### Getting the code

```bash
git clone https://github.com/hugoesparzac/VideoDownloader.git
cd VideoDownloader
dotnet tool restore
dotnet husky install
dotnet build
dotnet test
dotnet run --project src/VideoDownloader.App
```

`dotnet husky install` enables the git hooks described below. Please do not skip it, because the same checks run in CI and your pull request will fail without them.

The first time the app runs it downloads yt-dlp and ffmpeg (and gallery-dl when needed) into your local data folder.

## Project structure

```
src/
  VideoDownloader.Core/   Download logic, settings, no UI dependency
  VideoDownloader.App/    Avalonia UI (MVVM)
tests/
  VideoDownloader.Core.Tests/
scripts/                  Packaging script (publish.sh)
packaging/linux/          Linux installer and desktop entry
docs/                     Screenshots and documentation assets
```

### Architecture

- **Core** contains everything that is not UI: calling yt-dlp (through YoutubeDLSharp) and gallery-dl, file naming, settings, error hints. Services are exposed through interfaces (`IDownloadService`, `IImageDownloadService`, `ISettingsService`).
- **App** is an Avalonia application using MVVM with CommunityToolkit.Mvvm. Dependencies are wired with `Microsoft.Extensions.DependencyInjection` in `App.axaml.cs`.
- Anything that touches Avalonia types (folder picker, theme) lives behind an interface in `App/Services`, so view models stay free of UI types.
- Put logic in **Core** whenever possible. It is covered by unit tests; the view models are not.

## Code guidelines

- All code, identifiers, comments and documentation are written in **English**.
- Follow the `.editorconfig`. Analyzers are strict (`TreatWarningsAsErrors`), so warnings fail the build. Fix them instead of suppressing them. If a rule really does not make sense, discuss it in the pull request.
- Nullable reference types are enabled. Avoid the null-forgiving operator (`!`).
- Use `[ObservableProperty]` with **partial properties** and `[RelayCommand]` in view models.
- Bindings in XAML are **compiled**: set `x:DataType` on views and data templates.
- Always pass the `CancellationToken` through to long operations.
- Run external tools with `ProcessStartInfo.ArgumentList`, never by building a command-line string. URLs come from users and must not be able to inject arguments.
- Do not write empty `catch` blocks. Catch specific exceptions and explain in a comment why swallowing one is safe.
- Downloads work in a temporary `.vd-tmp-*` folder inside the destination and move the finished file at the end. Keep that pattern so cancelling or failing never leaves partial files.
- `settings.json` must stay backward compatible: when adding a setting, give it a default value so existing files keep working.

## Tests

```bash
dotnet test
```

- Add unit tests for any new logic in Core. Test names may use underscores (`Method_Scenario_Result`).
- Tests that need the network use `[ManualFact]` and only run when you ask for them:

```bash
RUN_MANUAL_TESTS=1 dotnet test --filter <TestName>
```

### Manual checks

UI and download changes also need a quick manual check. Depending on your change, try:

- A single video, an audio-only download and a playlist (including a private or deleted video, which must be skipped and reported).
- An image post (for example a TikTok photo post).
- Cancelling a download: no `.vd-tmp-*` folder or partial files should remain.
- Downloading the same item twice: the second file must not overwrite the first.
- Both themes (light and dark) if you touched the UI.

File and process code behaves differently on Windows (locked files, path handling), so if you change it, please test on Windows as well as Linux if you can. macOS has not been tested by the maintainer yet; reports from macOS users are very welcome.

To check the release packaging locally:

```bash
RIDS=linux-x64 ./scripts/publish.sh 0.0.0-dev
```

## Git workflow

- `main` is stable and only receives releases.
- `develop` is the integration branch.
- Create your branch from `develop`, named after the type of change: `feat/short-name`, `fix/short-name`, `docs/short-name`, `refactor/short-name`, `chore/short-name`.
- Open pull requests **against `develop`**.
- Keep pull requests focused: one change per pull request.

### Commit messages

This project uses [Conventional Commits](https://www.conventionalcommits.org/), enforced by a git hook and checked in CI:

```
<type>(<optional-scope>): <short description>
```

| Type | Use for |
|---|---|
| `feat` | A new feature |
| `fix` | A bug fix |
| `docs` | Documentation only |
| `style` | Formatting or visual changes that do not alter logic |
| `refactor` | Code changes that neither fix a bug nor add a feature |
| `perf` | Performance improvements |
| `test` | Adding or fixing tests |
| `chore` | Maintenance (versions, dependencies, configuration) |
| `ci` | CI and workflows |
| `build` | Build system, compilation, packaging, and distribution changes |

Keep the title under 72 characters. Examples:

```
feat(core): add playlist download that skips failed items
fix(app): show file errors instead of closing silently
docs: add contributing guide
ci: add release workflow for version tags
```

### Git hooks

Husky.Net runs these checks automatically:

- **pre-commit**: formats the staged `.cs` files with `dotnet format` (whitespace and style).
- **commit-msg**: rejects messages that do not follow Conventional Commits.

## Pull requests

Before opening a pull request, make sure that:

- [ ] `dotnet build` finishes without warnings.
- [ ] `dotnet test` passes.
- [ ] `dotnet format --verify-no-changes` reports nothing.
- [ ] The **pull request title** follows Conventional Commits (it is checked automatically).
- [ ] The description explains what changed and why, and links the related issue.
- [ ] UI changes include a screenshot (light and dark if relevant).
- [ ] User-visible changes are reflected in the README.

Merged pull requests are the source of the release notes, so clear titles matter.

## Releases (maintainers)

1. Update `<Version>` in `Directory.Build.props` in a `chore: bump version to X.Y.Z` pull request into `develop`.
2. Open a pull request from `develop` into `main` titled `chore: release X.Y.Z`.
3. After it is merged, tag the commit on `main` and push the tag:

```bash
git checkout main && git pull
git tag vX.Y.Z
git push origin vX.Y.Z
```

The release workflow runs the tests, builds the packages for Linux, Windows and macOS, and creates the GitHub release with generated notes. A tag with a suffix (for example `v1.0.0-rc1`) is published as a pre-release.

## Security

If you find a security problem, please do not open a public issue. Use the repository's **Security** tab to report it privately, or contact the maintainer directly.

## License

By contributing, you agree that your contributions will be licensed under the project's license (see [LICENSE](LICENSE)).

Hay tres cosas que debes revisar antes de subirlo:
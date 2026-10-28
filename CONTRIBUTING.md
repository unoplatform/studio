# Contributing

This repository ships the [`uno-platform-studio` agent plugin](plugins/uno-platform-studio): the `uno-dev` agent, the Uno Platform skills and the Claude Code hooks. This file covers how to change them, check them and release them. For what the plugin does and how to install it, see the [plugin README](plugins/uno-platform-studio/README.md).

## Layout

| Path | What it holds |
|---|---|
| `plugins/uno-platform-studio/` | The plugin and the source of truth for everything below: `agents/`, `codex/`, `hooks/`, `skills/` and the two plugin manifests. |
| `skills/` | A mirror of `plugins/uno-platform-studio/skills/` for agents without plugin support. Never edit it directly. |
| `.claude-plugin/`, `.github/plugin/`, `.agents/plugins/` | The marketplaces for Claude Code, GitHub Copilot and Codex. Each pins the plugin to a release tag. |
| `specs/` | Design specs for larger behavior changes, such as [`hot-reload-over-rebuild.md`](specs/hot-reload-over-rebuild.md). |
| `.github/scripts/` | The repo checks CI runs (see [Checks](#checks)) and `Set-PluginVersion.ps1`, which the release workflow uses. |

## Skills

### Why hubs instead of one skill per topic

Agents decide whether to load a skill from its `name` and `description` alone, and every host caps how much of that listing it will show. Claude Code caps the listing through its `skillListingBudgetFraction` setting and each description through `skillListingMaxDescChars` (1,536 characters), and once the listing budget is exceeded it drops descriptions, leaving bare names; Codex gives the catalog at most 2% of the model's context window (or 8,000 characters when the window is unknown), shortens descriptions to fit, and may omit skills from the list entirely; the [Agent Skills spec](https://agentskills.io/specification) limits each description to 1,024 characters. Sixty-two separate skills produced roughly 27,000 characters of listing text, so most of them were shown to the model as bare names and never triggered. Six hubs fit inside every budget, and the per-topic detail is still one `Read` away through the topic map.

### Adding a topic

Add a `references/<topic>.md` under the matching hub and a row in its topic map rather than a new skill; every new skill costs listing budget in every host. `Test-Plugin.ps1` fails when a hub's `SKILL.md` does not route to every file in its `references/` folder, and warns when a reference grows past 6,000 characters: split it by task.

### Writing a skill description

The `description` is the only field every host reads when choosing a skill (`when_to_use` is honored by Claude Code alone). Put both what the skill does and when to use it there: lead with the domain and the API or control names, then list the ways a user phrases the task without naming the technology. Keep it under 1,024 characters. Do not add a `when_to_use` field.

### Syncing the mirror

Edit skills under `plugins/uno-platform-studio/skills/`, then replace the mirror in the same commit:

```bash
rm -rf skills/uno-*
cp -r plugins/uno-platform-studio/skills/* skills/
```

`skills/README.md` is the only file the mirror does not share with the plugin.

## The `uno-dev` agent

The agent exists twice: `plugins/uno-platform-studio/agents/uno-dev.agent.md` for Claude Code and GitHub Copilot, and `plugins/uno-platform-studio/codex/uno-dev.toml` for Codex. Change both in the same commit. `Test-Plugin.ps1` fails when their instructions or metadata differ.

## Hooks

The hooks in `plugins/uno-platform-studio/hooks/` are POSIX `sh`, and they run on the user's own `sed` and `grep`: GNU on Linux and on Windows through Git for Windows, BSD on macOS. BSD `sed` rejects GNU-only constructs such as `\|` alternation without reporting an error, which empties the hook's output, so CI runs `Test-Hooks.ps1` on both Linux and macOS. Keep `hooks/hooks.json`'s `description` in step with what the scripts do.

## Checks

CI runs these on every pull request to `main` and `release/stable/**`. Run them locally with PowerShell 7 before you push:

| Command | What it checks | Needs |
|---|---|---|
| `pwsh .github/scripts/Test-Plugin.ps1` | The mirror matches the plugin; hubs route to every reference; relative links resolve; the two agent files agree; versions and `source.ref` agree. | — |
| `pwsh .github/scripts/Test-Hooks.ps1` | Both hooks against canned hook input, a stand-in DevServer CLI, and Uno and non-Uno folders. | `sh`, `chmod`, `sed`; on Windows run it from Git Bash. |
| `pwsh .github/scripts/Test-AgentInstall.ps1` | The plugin and each marketplace install in Copilot CLI and Codex, with every skill listed. | `npm install -g @github/copilot @openai/codex` |
| `claude plugin validate . --strict` and `claude plugin validate plugins/uno-platform-studio --strict` | The marketplaces, manifests and skill frontmatter, as Claude Code parses them. | Claude Code |
| `agentskills validate plugins/uno-platform-studio/skills/<skill>` | Each skill against the [Agent Skills spec](https://agentskills.io/specification). | `pipx install skills-ref` |

## Commits and pull requests

Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/) (`type(scope): summary`, for example `fix(skills): ...` or `docs(plugin): ...`). CI checks every commit of a pull request to `main` or a `release/**` branch. Fill in the pull request template.

The marketplaces and the plugin manifests carry the released version. Changes to them need a review from the release approvers listed in [`.github/CODEOWNERS`](.github/CODEOWNERS); the release workflow is normally the only thing that edits them.

## Releasing

`main` is staging. Every marketplace pins the plugin to a release tag through its `source.ref`, so agents only install what has been released.

The plugin version tracks the Uno.Sdk it is built for: plugin `<major>.<minor>.<patch>` targets Uno.Sdk `<major>.<minor>`, and the `release/stable/<major>.<minor>` branch is that line. `main` targets the next Uno.Sdk minor, which may only have `-dev` builds yet, so skills can cover APIs that haven't shipped. CI checks that the `major.minor` in `version.json` is an Uno.Sdk line published on NuGet, and the **Release** workflow refuses to ship until that line has a stable release, so a release never targets an SDK users can't install. The patch number is the plugin's own; it doesn't track Uno.Sdk patches.

Releases ship from `release/stable/<major>.<minor>` branches, like the other Uno Platform repos:

1. Comment `/unobot prepare-release --commit-message-pattern "ci: Set version to '{0}'"` on a merged PR. The bot cuts `release/stable/<major>.<minor>` from `main`, using the version in `version.json`, and opens a PR bumping `main` to the next `-dev` version. For a patch release, merge fixes into an existing release branch instead, for example with `/unobot backport release/stable/<major>.<minor>`.
2. Every push to the release branch runs the **Release** workflow. It validates the branch, then waits for approval on the `plugin-release` environment.
3. Once approved, it releases the next `<major>.<minor>.<patch>` (starting at `<major>.<minor>.0`). It stamps that version into every manifest on the release branch, tags the commit, publishes a GitHub Release, and opens a PR that moves the marketplaces on `main` to the new tag.
4. Merge that PR. Agents start installing the release once `main` points at it.

Don't edit versions or `source.ref` by hand. On `main` they always show the latest release, and `version.json` holds the next one. The workflow sets them with `.github/scripts/Set-PluginVersion.ps1`, and CI checks that they all agree.

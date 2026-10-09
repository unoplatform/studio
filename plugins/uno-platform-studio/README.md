# `uno-platform-studio` Plugin

Uno Platform development skills. Installs in **Claude Code**, **GitHub Copilot CLI**, **Copilot in VS Code**, and **OpenAI Codex CLI**.

## Contents

| Asset | Purpose |
|---|---|
| `agents/` | The `uno-dev` agent: builds Uno apps and features from a brief. Loaded by Claude Code and GitHub Copilot CLI. |
| `skills/` | The `uno-build-app` workflow skill and six domain skills. Each domain `SKILL.md` is a hub (when to use it, a topic map, and the critical rules) and its `references/` folder holds one short guide per topic. |
| `codex/` | The same agent as a Codex custom-agent file, installed by hand (see [Use the `uno-dev` agent](#use-the-uno-dev-agent)). |
| `hooks/` | Claude Code hooks. After an edit to a `.xaml` or `.cs` file, `PostToolUse` reminds the agent that the running app picks the change up through Hot Reload, so it asserts instead of restarting. |

| Skill | Covers | References |
|---|---|---|
| `uno-build-app` | Workflow for building an app or a feature from a brief: discovery, blueprint, build, verification, report. Loads the domain skills as each step needs them. | 8 |
| `uno-platform` | Entry point: routes any Uno Platform request to the domain skills and sets project-wide rules (UnoFeatures, WinUI dialect, docs grounding). | — |
| `uno-mvux` | MVUX state management: feeds, states, list feeds/states, FeedView, commands, selection, pagination, messaging, records. | 11 |
| `uno-navigation` | Uno.Extensions.Navigation: setup, routes, regions, code and XAML navigation, data passing, dialogs, qualifiers, TabBar/NavigationView/responsive shells, troubleshooting. | 14 topics + 4 shell templates |
| `uno-toolkit` | Uno Toolkit controls (TabBar, NavigationBar, CardContentControl, Chip, Drawer, SafeArea, AutoLayout, LoadingView, ShadowContainer, …) and attached-property extensions, plus setup, theming, lightweight styling, and C# Markup. | 32 |
| `uno-themes` | Uno Material (MD3), Simple theme, and the shared semantic colors, brushes, and typography. | 3 |
| `uno-testing` | UI testing of a running app through the Uno App MCP: visual tree, interaction, screenshots, assertions. | 4 |

### Why hubs instead of one skill per topic

Agents decide whether to load a skill from its `name` and `description` alone, and every host caps how much of that listing it will show. Claude Code caps the listing through its `skillListingBudgetFraction` setting and each description through `skillListingMaxDescChars` (1,536 characters), and once the listing budget is exceeded it drops descriptions, leaving bare names; Codex gives the catalog at most 2% of the model's context window (or 8,000 characters when the window is unknown), shortens descriptions to fit, and may omit skills from the list entirely; the [Agent Skills spec](https://agentskills.io/specification) limits each description to 1,024 characters. Sixty-two separate skills produced roughly 27,000 characters of listing text, so most of them were shown to the model as bare names and never triggered. Six hubs fit inside every budget, and the per-topic detail is still one `Read` away through the topic map.

### Writing a skill description

The `description` is the only field every host reads when choosing a skill (`when_to_use` is honored by Claude Code alone). Put both what the skill does and when to use it there: lead with the domain and the API or control names, then list the ways a user phrases the task without naming the technology. Keep it under 1,024 characters. Do not add a `when_to_use` field. To add a topic, add a `references/<topic>.md` under the matching hub and a row in its topic map rather than a new skill; every new skill costs listing budget in every host.

## Install

### Claude Code

```text
/plugin marketplace add unoplatform/studio
/plugin install uno-platform-studio@uno-platform
```

If prompted, run `/reload-plugins` (or restart the session) so the skills become available, then verify with `/skills`. To update later, open the plugin on the **Installed** tab of `/plugin` and select **Update now**, or run this in your shell:

```bash
claude plugin update uno-platform-studio@uno-platform
```

### GitHub Copilot CLI

```text
copilot plugin marketplace add unoplatform/studio
copilot plugin install uno-platform-studio@uno-platform
```

### GitHub Copilot in VS Code

Add `unoplatform/studio` to the `chat.plugins.marketplaces` setting, then install **uno-platform-studio** from the agent-plugins picker.

### OpenAI Codex CLI

```text
codex plugin marketplace add unoplatform/studio
codex plugin add uno-platform-studio@uno-platform
```

### Agents without plugin support

If your agent does not support a plugin format (Cursor, Gemini CLI, Windsurf, Cline, and others), the same skills are published as standalone folders in the repository's top-level [`skills/`](https://github.com/unoplatform/studio/tree/main/skills) directory. See its [README](https://github.com/unoplatform/studio/blob/main/skills/README.md) for copy instructions.

## Versions

Marketplace installs use the release tag pinned by `source.ref`, not the latest code on `main`. The agent and `uno-build-app` workflow described below are available from this checkout until a release containing them is published. See [Releasing](#releasing).

To try this checkout before that release, use `claude --plugin-dir ./plugins/uno-platform-studio --agent uno-platform-studio:uno-dev`, or `copilot plugin install ./plugins/uno-platform-studio`. For Codex, copy all seven folders under `skills/` into your project's `.agents/skills/`, then install the custom agent below. Avoid enabling an older plugin copy of the same skills in that session.

## Use the `uno-dev` agent

Start here rather than with individual skills. Select `uno-dev` and describe what you want in plain language: a new app from a brief, a feature for an existing Uno app, or a binding, state or navigation defect. It loads the `uno-build-app` workflow skill, which pulls in the focused skills as each step needs them, builds and exercises the first feature before repeating its pattern, and ends with a report that states which checks ran, which failed and which could not run.

### Claude Code

```text
claude --agent uno-platform-studio:uno-dev
```

### GitHub Copilot CLI

```text
copilot --agent uno-dev
```

Or pick **uno-dev** with `/agent` inside a session.

### OpenAI Codex CLI

Codex plugins cannot bundle agents yet ([openai/codex#18988](https://github.com/openai/codex/issues/18988)), so install the [custom agent](https://developers.openai.com/codex/subagents) by hand after installing the matching skills. Copy [`codex/uno-dev.toml`](codex/uno-dev.toml) to one of:

- `.codex/agents/uno-dev.toml` in your project, for that project only;
- `~/.codex/agents/uno-dev.toml`, for every project.

Then name it in your prompt, for example: *Have uno-dev build a reading tracker with a yearly goal setting.* Delete the file to remove the agent.

### Without the agent

The workflow lives in the `uno-build-app` skill, so every client can invoke it directly, the same way it invokes any skill. In Claude Code:

```text
/uno-platform-studio:uno-build-app Build me a reading tracker with a yearly goal setting.
```

The workflow uses the Uno documentation MCP when it is available, and the Uno App MCP or other available UI automation for runtime checks. It reports any runtime checks it cannot perform as not run.

## Uninstall

### Claude Code

```text
/plugin uninstall uno-platform-studio@uno-platform
/plugin marketplace remove uno-platform
```

(Removing the marketplace also uninstalls any plugins installed from it, so the first line is optional.)

### GitHub Copilot CLI

```text
copilot plugin uninstall uno-platform-studio
copilot plugin marketplace remove uno-platform
```

### GitHub Copilot in VS Code

Right-click **uno-platform-studio** in the **Agent Plugins - Installed** view and select **Uninstall**, then remove the `unoplatform/studio` entry from the `chat.plugins.marketplaces` setting.

### OpenAI Codex CLI

```text
codex plugin remove uno-platform-studio@uno-platform
codex plugin marketplace remove uno-platform
```

## Documentation Grounding

The skills use the Uno documentation MCP (`UnoDocs`) to search and fetch official Uno Platform documentation. It is not bundled in this plugin; it comes with the **Uno tooling**, which most Studio users already have installed.

If you don't have the Uno tooling:

- **claude.ai / Claude Code**: install it from the [connector catalog](https://claude.ai/connectors).
- **Other agents**: register it manually in your agent's MCP configuration. It is a remote server requiring no authentication: `https://mcp.platform.uno/v1` (HTTP transport).

## UI Testing Requirement

The `uno-testing` skill additionally requires the **Uno App MCP**, which drives a running app for visual-tree inspection, interaction, and screenshots (the `uno_app_*` tools). Like the documentation MCP, it is provided by the **Uno tooling**, not bundled in this plugin. Agents that have only the documentation MCP cannot run this skill; its `compatibility:` frontmatter notes the same requirement.

## Migrating from the per-topic skills

Versions before 2.0 shipped one skill per topic (`uno-mvux-feed-basics`, `uno-toolkit-card`, and so on). Every one of them still exists as a reference file inside its hub, at `skills/uno-<domain>/references/<topic>.md`; only the `uno-<domain>-` prefix was dropped. For example `uno-toolkit-card` is now `skills/uno-toolkit/references/card.md`, `uno-navigation-tabbar` is `skills/uno-navigation/references/tabbar.md`, and `uno-mvux-overview` is `skills/uno-mvux/references/overview.md`. The two testing skills became `uno-testing/references/ui.md` and `uno-testing/references/assertions.md`. If you copied individual skill folders into an agent that does not use plugins, replace them with the hub folder.

## Manifests

| File | Consumer |
|---|---|
| `.claude-plugin/plugin.json` | Claude Code, GitHub Copilot CLI, Copilot in VS Code |
| `.codex-plugin/plugin.json` | OpenAI Codex CLI |
| `hooks/hooks.json` | Claude Code (hooks only) |

## Releasing

`main` is staging. Every marketplace pins the plugin to a release tag through its `source.ref`, so agents only install what has been released.

Releases ship from `release/stable/<major>.<minor>` branches, like the other Uno Platform repos:

1. Comment `/unobot prepare-release --commit-message-pattern "ci: Set version to '{0}'"` on a merged PR. The bot cuts `release/stable/<major>.<minor>` from `main`, using the version in `version.json`, and opens a PR bumping `main` to the next `-dev` version. For a patch release, merge fixes into an existing release branch instead, for example with `/unobot backport release/stable/1.2`.
2. The **Release** workflow validates the branch, then waits for approval on the `plugin-release` environment.
3. Once approved, it releases the next `1.2.<patch>` (starting at `1.2.0`). It stamps that version into every manifest on the release branch, tags the commit, publishes a GitHub Release, and opens a PR that moves the marketplaces on `main` to the new tag.
4. Merge that PR. Agents start installing the release once `main` points at it.

Don't edit versions or `source.ref` by hand. On `main` they always show the latest release, and `version.json` holds the next one. The workflow sets them with `.github/scripts/Set-PluginVersion.ps1`, and CI checks that they all agree.

Run the repo checks locally with `pwsh .github/scripts/Test-Plugin.ps1`.

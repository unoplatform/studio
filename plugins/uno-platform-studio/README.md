# `uno-platform-studio` Plugin

Uno Platform development skills. Installs in **Claude Code**, **GitHub Copilot CLI**, **Copilot in VS Code**, and **OpenAI Codex CLI**.

## Contents

| Asset | Purpose |
|---|---|
| `agents/` | The `uno-dev` agent: builds Uno apps and features from a brief. Loaded by Claude Code and GitHub Copilot CLI. |
| `skills/` | The `uno-build-app` workflow skill and six domain skills. Each domain `SKILL.md` is a hub (when to use it, a topic map, and the critical rules) and its `references/` folder holds one short guide per topic. |
| `codex/` | The same agent as a Codex custom-agent file, installed by hand (see [Use the `uno-dev` agent](#use-the-uno-dev-agent)). |
| `hooks/` | Claude Code hooks that keep the agent on Hot Reload instead of rebuilding a running app (see [Hooks](#hooks)). |

| Skill | Covers | References |
|---|---|---|
| `uno-build-app` | Workflow for building an app or a feature from a brief: discovery, blueprint, build, verification, report. Loads the domain skills as each step needs them. | 8 |
| `uno-platform` | Entry point: routes any Uno Platform request to the domain skills and sets project-wide rules (UnoFeatures, WinUI dialect, docs grounding), plus Hot Design previews. | 1 |
| `uno-mvux` | MVUX state management: feeds, states, list feeds/states, FeedView, commands, selection, pagination, messaging, records, mocking. | 12 |
| `uno-navigation` | Uno.Extensions.Navigation: setup, routes, regions, code and XAML navigation, data passing, dialogs, qualifiers, TabBar/NavigationView/responsive shells, troubleshooting. | 14 topics + 4 shell templates |
| `uno-toolkit` | Uno Toolkit controls (TabBar, NavigationBar, CardContentControl, Chip, Drawer, SafeArea, AutoLayout, LoadingView, ShadowContainer, …) and attached-property extensions, plus setup, theming, lightweight styling, and C# Markup. | 32 |
| `uno-themes` | Uno Material (MD3), Simple theme, and the shared semantic colors, brushes, and typography. | 5 |
| `uno-testing` | UI testing of a running app through the Uno App MCP: visual tree, interaction, screenshots, assertions. Release builds on Windows and Android through the platform accessibility tree. | 6 |

To change a skill or add a topic, see [CONTRIBUTING.md](https://github.com/unoplatform/studio/blob/main/CONTRIBUTING.md).

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

Marketplace installs use the release tag pinned by `source.ref`, not the latest code on `main`. The plugin version tracks the Uno.Sdk it is built for: plugin `<major>.<minor>.<patch>` targets Uno.Sdk `<major>.<minor>`, and the patch number is the plugin's own. `main` targets the next Uno.Sdk minor, so it can describe APIs that have only `-dev` builds yet.

To try unreleased changes from this checkout, use `claude --plugin-dir ./plugins/uno-platform-studio --agent uno-platform-studio:uno-dev`, or `copilot plugin install ./plugins/uno-platform-studio`. For Codex, copy all seven folders under `skills/` into your project's `.agents/skills/`, then install the custom agent below. Avoid enabling an older plugin copy of the same skills in that session.

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

## Hooks

Claude Code loads two hooks from `hooks/hooks.json`. GitHub Copilot and Codex do not load them; there the skills carry the same rule as text.

- **`SessionStart`** (on startup, resume, `/clear` and after a compaction): in an Uno app project, it tells the agent that a running app is edited through Hot Reload, not rebuilt, and to look for a running app before the first build. In any other folder it prints nothing.
- **`PostToolUse`** (after an `Edit`, `Write` or `MultiEdit` of a `.xaml` or `.cs` file): it asks the DevServer whether the running app applied the edit (`uno-devserver app hot-reload-status --no-start`) and tells the agent the result: assert on the change, fix the compile errors it reports, or restart only after a rude edit or a DevServer internal error. With no app connected yet, it prints nothing. Without the `uno-devserver` tool, or without a `.sln` or `.slnx` above the edited file, it only reminds the agent that Hot Reload applies the edit.

Both hooks are POSIX `sh` scripts using `sed`, `grep` and `find`, run as `sh "<script>"`, so a POSIX `sh` must be on the `PATH`; on Windows, Git for Windows provides one. Set `UNO_DEVSERVER_CLI` to use a `uno-devserver` that is not on the `PATH`. To turn them off without uninstalling the plugin, set `"disableAllHooks": true` in your Claude Code settings; that turns off every other hook too.

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

The `uno-testing` skill drives a running Debug build through the **Uno App MCP** (the `uno_app_*` tools): visual-tree inspection, interaction, and screenshots. Like the documentation MCP, it is provided by the **Uno tooling**, not bundled in this plugin.

Without the App MCP, and for Release builds, the skill verifies the app through the platform accessibility tree instead: Windows UI Automation on Windows, `uiautomator` on Android. On other targets without the App MCP, it falls back to a static review of the XAML and code and reports those checks as unverified. Its `compatibility:` frontmatter says the same.

## Migrating from the per-topic skills

Versions before 6.7 (the 1.x releases) shipped one skill per topic (`uno-mvux-feed-basics`, `uno-toolkit-card`, and so on). Every one of them still exists as a reference file inside its hub, at `skills/uno-<domain>/references/<topic>.md`; only the `uno-<domain>-` prefix was dropped. For example `uno-toolkit-card` is now `skills/uno-toolkit/references/card.md`, `uno-navigation-tabbar` is `skills/uno-navigation/references/tabbar.md`, and `uno-mvux-overview` is `skills/uno-mvux/references/overview.md`. The two testing skills became `uno-testing/references/ui.md` and `uno-testing/references/assertions.md`. If you copied individual skill folders into an agent that does not use plugins, replace them with the hub folder.

## Manifests

| File | Consumer |
|---|---|
| `.claude-plugin/plugin.json` | Claude Code, GitHub Copilot CLI, Copilot in VS Code |
| `.codex-plugin/plugin.json` | OpenAI Codex CLI |
| `hooks/hooks.json` | Claude Code (hooks only) |

To build, test or release the plugin, see [CONTRIBUTING.md](https://github.com/unoplatform/studio/blob/main/CONTRIBUTING.md).

# `uno-platform-studio` Plugin

Uno Platform development skills. Installs in **Claude Code**, **GitHub Copilot CLI**, **Copilot in VS Code**, and **OpenAI Codex CLI**.

## Contents

| Asset | Purpose |
|---|---|
| `agents/` | The `uno-dev` agent: builds Uno apps and features from a brief. Loaded by Claude Code and GitHub Copilot CLI. |
| `skills/` | The `uno-build-app` workflow skill, plus SKILL.md files covering MVUX (feeds, state, list state, selection, pagination, messaging), navigation (routes, regions, dialogs, tab/navigation shells), Uno Toolkit controls and helpers, theming (Material, Simple, shared semantic), and UI testing. |
| `codex/` | The same agent as a Codex custom-agent file, installed by hand (see [Use the `uno-dev` agent](#use-the-uno-dev-agent)). |

## Install

### Claude Code

```text
/plugin marketplace add unoplatform/studio
/plugin install uno-platform-studio@uno-platform
```

If prompted, run `/reload-plugins` (or restart the session) so the skills become available, then verify with `/skills`. To update later:

```text
/plugin update uno-platform-studio@uno-platform
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

Installing from the marketplace tracks the repository's default branch. Studio does not publish release tags yet; once it does, you will be able to pin a release.

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

Codex plugins cannot bundle agents yet ([openai/codex#18988](https://github.com/openai/codex/issues/18988)), so install the agent file by hand after installing the plugin. Copy [`codex/uno-dev.toml`](codex/uno-dev.toml) to one of:

- `.codex/agents/uno-dev.toml` in your project, for that project only;
- `~/.codex/agents/uno-dev.toml`, for every project.

Then name it in your prompt, for example: *Have uno-dev build a reading tracker with a yearly goal setting.* Delete the file to remove the agent.

### Without the agent

The workflow lives in the `uno-build-app` skill, so every client can invoke it directly, the same way it invokes any skill. In Claude Code:

```text
/uno-platform-studio:uno-build-app Build me a reading tracker with a yearly goal setting.
```

The workflow uses the Uno documentation MCP when it is available, and the Uno App MCP for runtime checks. Without the App MCP it still builds and tests, and reports runtime checks as not run.

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

The two UI-testing skills (`uno-testing-ui`, `uno-testing-assertions`) additionally require the **Uno App MCP**, which drives a running app for visual-tree inspection, interaction, and screenshots (the `uno_app_*` tools). Like the documentation MCP, it is provided by the **Uno tooling**, not bundled in this plugin. Agents that have only the documentation MCP cannot run these two skills; their `compatibility:` frontmatter notes the same requirement.

## Manifests

| File | Consumer |
|---|---|
| `.claude-plugin/plugin.json` | Claude Code |
| `.github/plugin.json` | GitHub Copilot CLI + Copilot in VS Code |
| `.codex-plugin/plugin.json` | OpenAI Codex CLI |

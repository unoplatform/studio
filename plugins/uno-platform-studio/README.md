# `uno-platform-studio` Plugin

Uno Platform development skills. Installs in **Claude Code**, **GitHub Copilot CLI**, **Copilot in VS Code**, and **OpenAI Codex CLI**.

## Contents

| Asset | Purpose |
|---|---|
| `skills/` | SKILL.md files covering MVUX (feeds, state, list state, selection, pagination, messaging), navigation (routes, regions, dialogs, tab/navigation shells), Uno Toolkit controls and helpers, theming (Material, Simple, shared semantic), and UI testing. |

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

# Uno Platform Studio agent plugin

This repository ships **`uno-platform-studio`**, the agent plugin of [Uno Platform Studio](https://aka.platform.uno/studio). It teaches AI coding agents to build cross-platform .NET apps with [Uno Platform](https://platform.uno): MVUX state, Uno.Extensions navigation, Uno Toolkit controls, Material and Simple themes, and UI checks against the running app.

It installs in **Claude Code**, **GitHub Copilot CLI**, **GitHub Copilot in VS Code** and **OpenAI Codex CLI**. Agents without plugin support (Cursor, Gemini CLI, Windsurf, Cline and others) can copy the same skills from [`skills/`](skills).

## What's in the plugin

- **The `uno-dev` agent.** Describe what you want in plain language: a new app from a brief, a feature for an existing Uno app, or a binding, state or navigation defect. It plans the work, builds and runs the first feature before repeating its pattern, and ends with a report of which checks ran, which failed and which could not run.
- **Nine skills.** `uno-build-app` is the workflow the agent follows; `uno-platform` routes any Uno request; `uno-mvux`, `uno-navigation`, `uno-toolkit`, `uno-themes`, `uno-authentication`, `uno-kiota` and `uno-testing` hold one short reference per topic. Any agent can use the skills without the `uno-dev` agent.
- **Claude Code hooks.** While an app is running, they keep the agent on Hot Reload instead of rebuilding and restarting it after each edit.

The [plugin README](plugins/uno-platform-studio/README.md) describes each skill and hook in detail.

## Install

```text
# Claude Code
/plugin marketplace add unoplatform/studio
/plugin install uno-platform-studio@uno-platform

# GitHub Copilot CLI
copilot plugin marketplace add unoplatform/studio
copilot plugin install uno-platform-studio@uno-platform

# OpenAI Codex CLI
codex plugin marketplace add unoplatform/studio
codex plugin add uno-platform-studio@uno-platform
```

Then start the agent, for example with `claude --agent uno-platform-studio:uno-dev` or `copilot --agent uno-dev`. Codex needs the agent file installed by hand. The plugin README covers that, [Copilot in VS Code](plugins/uno-platform-studio/README.md#github-copilot-in-vs-code), updating and uninstalling.

## Requirements

The skills work best with the two MCP servers that come with the Uno tooling:

- the **Uno documentation MCP**, which grounds answers in the official Uno Platform documentation;
- the **Uno App MCP**, which lets the agent start the app, read its visual tree, click, type and take screenshots to check its own work.

Neither is bundled in the plugin. See [Documentation Grounding](plugins/uno-platform-studio/README.md#documentation-grounding) and [UI Testing Requirement](plugins/uno-platform-studio/README.md#ui-testing-requirement) for how to get them.

The plugin version follows the Uno.Sdk line it was verified against: plugin `6.7.x` targets Uno.Sdk 6.7. Marketplace installs use the latest [release](https://github.com/unoplatform/studio/releases); `main` can hold unreleased changes for the next Uno.Sdk.

## Feedback

[Open an issue](https://github.com/unoplatform/studio/issues/new/choose) to report a bug, a wrong or missing piece of guidance in a skill, or to suggest an enhancement. Ask questions in [Discussions](https://github.com/unoplatform/studio/discussions) or on our [Discord server](https://platform.uno/uno-discord). Report security vulnerabilities through the [security policy](SECURITY.md). For support beyond the community, [contact us](https://platform.uno/contact).

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for how the repository is laid out, how to change a skill, the agent or the hooks, the checks to run, and how releases ship.

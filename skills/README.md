# Uno Platform Agent Skills

Standalone copies of the Uno Platform skills for AI coding agents, covering MVUX, navigation, Uno Toolkit, theming (Material, Simple, semantic colors), and UI testing.

This folder is for agents that **don't use the plugin system**: Cursor, Gemini CLI, Windsurf, Cline, and others. If your agent supports plugins (Claude Code, GitHub Copilot CLI, Copilot in VS Code, OpenAI Codex CLI), prefer installing the [`uno-platform-studio` plugin](../plugins/uno-platform-studio) instead, which bundles these same skills with proper metadata.

## Install all skills

Each skill is a self-contained folder with a `SKILL.md` (and optional `references/`). All skills are named `uno-<category>-<topic>`, so a `uno-*` glob copies the full catalog.

Cursor example:

```bash
git clone https://github.com/unoplatform/studio.git
mkdir -p ~/.cursor/skills
cp -r studio/skills/uno-* ~/.cursor/skills/
```

```powershell
git clone https://github.com/unoplatform/studio.git
New-Item -ItemType Directory -Force "$HOME/.cursor/skills" | Out-Null
Copy-Item studio/skills/uno-* "$HOME/.cursor/skills/" -Recurse
```

For other agents, use the same commands with your agent's skills directory as the target.

## Install individual skills

Copy just the folder(s) you need:

```bash
cp -r studio/skills/uno-mvux-feed-basics ~/.cursor/skills/
```

Skills can also be installed per project instead of per user. For example, an agent that reads project-level skills from a `skills/` convention folder:

```
your-project/
└── <agent-folder>/
    └── skills/
        └── uno-mvux-feed-basics/
            └── SKILL.md
```

## Maintenance note

This folder is a **mirror** of [`plugins/uno-platform-studio/skills/`](../plugins/uno-platform-studio/skills). The plugin copy is the source of truth: make edits there and sync them here. The two trees must stay identical (except this README).

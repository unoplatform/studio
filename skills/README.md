# Uno Platform Agent Skills

Standalone copies of eight Uno Platform skills for AI coding agents: the `uno-build-app` workflow, the `uno-platform` router and six domain hubs (`uno-mvux`, `uno-navigation`, `uno-toolkit`, `uno-themes`, `uno-authentication`, `uno-testing`) whose `references/` folders hold one short guide per topic.

This folder is for agents that **don't use the plugin system**: Cursor, Gemini CLI, Windsurf, Cline, and others. If your agent supports plugins (Claude Code, GitHub Copilot CLI, Copilot in VS Code, OpenAI Codex CLI), prefer installing the [`uno-platform-studio` plugin](../plugins/uno-platform-studio) instead, which bundles these same skills with proper metadata.

## Install all skills

Each skill is a self-contained folder: the six domain hubs carry a `references/` folder that their `SKILL.md` routes to, `uno-build-app` also carries output templates in `assets/`, and `uno-platform` has only its `SKILL.md`. Copy the whole folder, never `SKILL.md` alone. A `uno-*` glob copies the full catalog.

`main` can contain unreleased changes. For the version the plugin ships, clone the [latest release](https://github.com/unoplatform/studio/releases/latest) tag instead, e.g. `git clone --branch <tag> https://github.com/unoplatform/studio.git`.

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

Copy just the domain folder(s) you need:

```bash
cp -r studio/skills/uno-mvux ~/.cursor/skills/
```

Per-topic skills from earlier versions (for example `uno-mvux-feed-basics` or `uno-toolkit-card`) now live inside their hub as `uno-mvux/references/feed-basics.md` and `uno-toolkit/references/card.md`. Install the hub folder instead of the old per-topic folder.

Skills can also be installed per project instead of per user. For example, an agent that reads project-level skills from a `skills/` convention folder:

```
your-project/
└── <agent-folder>/
    └── skills/
        └── uno-mvux/
            ├── SKILL.md
            └── references/
```

## Maintenance note

This folder is a **mirror** of [`plugins/uno-platform-studio/skills/`](../plugins/uno-platform-studio/skills). The plugin copy is the source of truth: make edits there and sync them here. The two trees must stay identical (except this README).

# Waza (GitHub Copilot CLI)

The same cases as `evals/`, run through GitHub Copilot CLI with [Waza](https://github.com/microsoft/waza) instead of
Claude Code. `ConvertTo-WazaEval.ps1` generates the Waza suites into `out/` (gitignored), so the cases in `evals/` stay
the only source; regenerate after changing them.

```sh
pwsh ./evals/waza/ConvertTo-WazaEval.ps1                                           # every case
pwsh ./evals/waza/ConvertTo-WazaEval.ps1 -Case uno-mvux/remove-item-from-list,routing/maui-question
```

## Suites

- `out/<skill>/eval.yaml`: the regex and rubric graders on the reply. Run with `--baseline`: Waza runs each task with
  the plugin's skills and again with skills disabled, and reports the difference.
- `out/routing/eval.yaml`: which skills Copilot loads (each case's `loads-skill.md`, and the routing cases' checks that
  no Uno skill loads). Run without `--baseline`. These live apart because Waza scores every grader: a skill-loaded
  check in the baseline suite would fail every no-skills run and overstate the lift.

Copilot discovers the skills from `plugins/uno-platform-studio/skills` and loads them itself, as with the installed
plugin; Waza's default of injecting the target skill into the system prompt is turned off.

## Run

Needs the [Waza CLI](https://github.com/microsoft/waza#installation) and a signed-in Copilot CLI (`waza models` lists
the models your account can use). Real runs use Copilot premium requests: three trials per task, twice with
`--baseline`, plus a judge call per rubric.

```sh
waza run evals/waza/out/uno-mvux/eval.yaml --baseline -o evals/results/waza-uno-mvux.json
waza run evals/waza/out/routing/eval.yaml --trials 1 -o evals/results/waza-routing.json
waza run evals/waza/out/uno-mvux/eval.yaml --baseline --model gpt-5.3-codex       # another model
```

`waza preflight <eval.yaml>` checks a suite offline, without model calls.

The rubric judge defaults to `claude-sonnet-4.6`, the closest Copilot offers to the Claude Code suite's judge. When
comparing scores across the two harnesses, keep in mind that a rubric difference can come from the judge as well as
the agent.

Not carried over: `max_turns` and `allowed_tools`. Waza has no per-task equivalent for skill evals, so the Copilot
agent can also use its other tools.

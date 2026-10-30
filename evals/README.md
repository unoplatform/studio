# Skill evals

Behavior tests for the `uno-platform-studio` skills, run with [`claude plugin eval`](https://code.claude.com/docs/en/plugin-evals.md)
and modeled on [dotnet/skills](https://github.com/dotnet/skills/tree/main/tests) and
[dotnet/maui-labs](https://github.com/dotnet/maui-labs/tree/main/tests). CI checks that the plugin is well formed
(`validate-plugin.yml`); these check what an agent answers with it: a realistic developer prompt, graders on the reply,
and the same prompt run without the plugin as a baseline.

They live at the repo root, not under `plugins/uno-platform-studio/`, so they don't ship in the installed plugin.
Each case's `prompt.md` names the plugin with `plugins: ["../../../plugins/uno-platform-studio"]`.

## Layout

```text
evals/
  <skill>/<case>/        one prompt per case, grouped by the skill it targets
    prompt.md            frontmatter (runs, tags, tools) + the developer's prompt
    graders/
      loads-skill.md     the skill loaded (with-plugin arm only, reported but not scored)
      *.md               regex graders on the reply: required APIs, and anti-patterns that must not appear
      rubric.md          LLM judge: PASS/FAIL criteria
  routing/<case>/        prompts outside Uno (MAUI, ASP.NET Core) that must load no Uno skill
```

## Writing a case

From the dotnet/skills eval-quality rules:

- **Natural prompt.** Write it the way an Uno developer would ask. Don't name the skill, quote its vocabulary or hint
  at the answer.
- **Test what the skill adds.** Aim at the pitfalls and Uno-specific APIs the skill teaches, where a model without it
  goes wrong. A case both arms pass shows no lift and only guards against regressions.
- **Grade Uno correctness, not the skill's phrasing.** Every graded API is checked against the references or the Uno
  docs, and each regex accepts every correct variant, so a right answer can't fail.
- **Anti-patterns match code, not mentions.** A `not_contains` regex matches the anti-pattern's code shape (a
  declaration, a call, an override), so a reply that warns against it still passes.
- **Cover different ground.** Each case targets a different capability or risk in the skill.

## Run

```sh
claude plugin eval . --judge-model claude-sonnet-5-5 --trust-plugin --no-publish        # everything, with baseline
claude plugin eval . --tag uno-navigation --runs 1 --trust-plugin --no-publish           # one skill while iterating
claude plugin eval . --tag routing --ablation none --trust-plugin --no-publish           # routing boundaries
```

The report gives each case's score with and without the plugin and the difference between them. A skill whose cases
show no lift on a new model is a candidate to trim.

To run the same cases through GitHub Copilot CLI, see [`waza/`](waza/README.md).

## Reports and the CI gate

CI holds no model credentials, so it doesn't run the evals. Instead, a change that touches a suite carries a fresh
report of it: `evals/reports/<suite>.json`, written by a local run.

```sh
pwsh .github/scripts/Update-EvalReport.ps1                    # every suite whose report is stale or missing
pwsh .github/scripts/Update-EvalReport.ps1 -Suite uno-mvux    # one suite
```

It runs the suite with the plugin only (`--ablation none`, so the "skill loaded" graders count), with Sonnet 5.5 as
agent and judge, and records each case's score and run count with a hash of the files the suite depends on: its
cases, its skill (for `routing`, every `SKILL.md`), and the plugin's hooks. Commit the report with the change.

`Test-EvalReports.ps1`, in the required "Validate plugin and skills" check, fails the build when a report is missing,
was run against different files, skipped a case or runs, or has a case scoring below 0.8 (the mean over its runs of
the fraction of graders passed). It makes no model calls, so it also runs on fork PRs. It checks every report on
every build, so a report left stale on `main` fails later PRs until someone refreshes it.

A case that fails because a new model answers differently, not because of a change here, fails the next report of its
suite too: fix the skill or the case, or change the case's expectations in the same PR.

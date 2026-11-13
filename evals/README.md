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
      loads-skill.md     the skill loaded (with-plugin arm only)
      *.md               regex graders on the reply: required APIs, and anti-patterns that must not appear
      rubric-*.md        LLM judge, one criterion per file: one PASS condition and the FAIL conditions it rules out
  routing/<case>/        prompts outside Uno that must load no Uno skill: MAUI, ASP.NET Core, and near misses on a
                         skill's topic (Entra ID tokens in an API, Kiota in a console app)
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
- **One criterion per judge.** A judge asked whether several things all hold rates the same reply differently from run
  to run when one of them is missed, so each `rubric-<criterion>.md` holds one `PASS if` and no list, and carries only
  the facts that criterion needs. A criterion a regex can check (an API name, a property value) is a regex grader
  instead: it costs nothing and gives the same answer every run.

`Test-EvalCases.ps1`, in the "Validate plugin and skills" check, enforces the file rules without running anything:
every prompt is non-empty and no two cases share one, and every `llm` grader is named `rubric-*.md` (and only those)
and has one `PASS if`, no list, and no `(and ` or `; ` in its PASS condition outside inline code. Those are the two
phrasings a second requirement has hidden in ("PASS if it locates elements by id (and suggests adding ids)"); the
check is a tripwire for them, not proof that a rubric judges one thing, so read every PASS condition for an "and" that
joins two requirements. A clarification of the one criterion, such as which answers pass trivially, goes in the "Facts
the judge needs" line instead.

## Run

```sh
claude plugin eval . --model claude-sonnet-5-5 --judge-model claude-sonnet-5-5 --trust-plugin --no-publish  # both arms
claude plugin eval . --tag uno-navigation --runs 1 --trust-plugin --no-publish           # one skill while iterating
claude plugin eval . --tag routing --ablation none --trust-plugin --no-publish           # routing boundaries
```

The report gives each case's score with and without the plugin and the difference between them. A skill whose cases
show no lift on a new model is a candidate to trim.

## CI

CI doesn't run the evals: it holds no model credentials. `Test-EvalCases.ps1` checks only that the cases are well
formed. When you change a skill or a case, run that skill's suite locally (see [Run](#run)) and say in the pull request
whether you did. The full result of each run, with every judge's evidence, stays in the git-ignored
`evals/results/` folder.

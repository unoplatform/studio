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
      loads-skill.md     the skill loaded (with-plugin arm only; scored in CI, which runs that arm alone)
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
pwsh .github/scripts/Invoke-Evals.ps1                    # the suites your branch changes, as CI runs them
pwsh .github/scripts/Invoke-Evals.ps1 -Suite uno-mvux    # one suite (or -Suite all)
claude plugin eval . --tag uno-navigation --runs 1 --trust-plugin --no-publish           # one skill while iterating
claude plugin eval . --model claude-sonnet-5-5 --judge-model claude-sonnet-5-5 --trust-plugin --no-publish  # both arms
```

`Invoke-Evals.ps1` runs the suites the way CI does (below), with your Claude Code login, and costs what CI would: at
API prices $0.50 to $3.20 a suite, about $19 for all ten. With no `-Suite`, it runs the suites whose inputs differ from
where your branch left `origin/main` (`-Base` names another branch), uncommitted changes included. It prints the same
summary as CI, and each run's result and HTML report, with every judge's reasoning, stay in the git-ignored
`evals/results/` folder.

Running both arms gives each case's score with and without the plugin, and the difference between them. A skill whose
cases show no lift on a new model is a candidate to trim.

## CI

The "Evals" workflow (`.github/workflows/evals.yml`) runs the evals with the repository's API key, so contributors
need no Claude credentials and pay nothing.

- **On a pull request** to `main` or `release/stable/**`, it runs the suites whose inputs the pull request changes
  (`Get-EvalInputPaths` in `Evals.Common.ps1`):
  - A skill suite depends on its cases, its skill, and the `SKILL.md` of `uno-platform` and `uno-build-app`. Agents
    load `uno-platform` first and follow its routing table, which sends feature work to `uno-build-app` before any
    domain skill, so an edit to either file runs every skill suite: about $19 and 30 minutes.
  - `routing` depends on its cases and the frontmatter of every `SKILL.md`: an agent picks a skill from its name and
    description alone, and a routing case passes only when no skill loads.
  - Every suite depends on `hooks/hooks.json`, the SessionStart hook, and how the evals run: `Evals.Common.ps1` (the
    models and threshold), `Invoke-Evals.ps1`, and the workflow (the Claude Code version).

  A pull request that changes none of these runs nothing and passes. A suite that already passed on the same pull
  request with the same inputs isn't rerun, so a push that changes something else, or a rerun, costs only what changed
  or failed. A new push cancels the run in progress.
- **By hand**, from the Actions tab, a maintainer runs any suites (the `suites` input, `all` by default) on any branch.
- It runs with the plugin only (`--ablation none`, so the "skill loaded" graders count), with Sonnet 5.5 as agent and
  judge. It fails when a case scores below 0.8 (the mean over its runs of the fraction of graders passed), when a case
  doesn't run (each case's `tags` must include its suite, which `Test-EvalCases.ps1` checks), or when the run stops at
  its cost ceiling of $5 a suite.
- The job summary lists each case's score and the graders it failed, with how many runs each failed in
  (`info-plist 3/3`), so a reviewer sees why a case lost points. The `eval-results` artifact holds the HTML report and
  the full result for 14 days.
- Pull requests from forks get no secrets, so the job says so and passes without running: the author can run
  `Invoke-Evals.ps1` locally, or a maintainer runs the workflow after merging. Drafts run when marked ready for review.

**A case that fails by chance.** A case near 0.8 can fail on an unlucky run. "Re-run failed jobs" reruns only the
suites that failed. A case that keeps failing because a new model answers differently needs its skill or its
expectations changed.

**The key** is the `ANTHROPIC_EVALS_API_KEY` secret. Anyone who can push a branch to this repository can run code with
it, and the per-run ceiling is set in files a pull request can change, so the key comes from an Anthropic Console
workspace of its own with a monthly spend limit: that limit is the real cap. When it's reached, every run errors and the
summary says so. Setting the `EVALS_ENABLED` repository variable to `false` turns the evals off without a pull request.

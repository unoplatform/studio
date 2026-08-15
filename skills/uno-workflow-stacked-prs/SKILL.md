---
name: uno-workflow-stacked-prs
description: "Splits a large or sequentially dependent change into a stack of small, dependent, independently reviewable GitHub pull requests using the gh-stack CLI extension, with a plain git + gh fallback."
when_to_use: "Use this skill when a change is too big to review as one PR, when new work must build on a branch that hasn't merged yet, when asked to split an oversized (often AI-generated) PR or branch into reviewable pieces, or when asked to create, restack, rebase, reorder, sync, review, or merge a stack of pull requests. Also covers designing layer boundaries, creating one sub-issue per layer, and how reviewers should navigate a stack."
compatibility: "Works with any GitHub repository and any language or framework — nothing in this skill is specific to a particular stack. Stacked pull requests are a GitHub feature in public preview. The primary path requires GitHub CLI 2.90.0+ with the github/gh-stack extension; a fallback using plain git and gh works everywhere. Cross-fork and cross-repository stacks are not supported by GitHub."
metadata:
  author: uno-platform
  version: "1.0"
  category: workflow
---

# Stacked Pull Requests

Stacked PRs break one large change into a **chain of small, dependent PRs** that are reviewed and
merged independently. Each PR targets the branch of the PR below it; the bottom PR targets the
trunk (usually `main`).

```text
   ┌── feat-3-ui      → PR #3 (base: feat-2-api)     ← top
  ┌── feat-2-api      → PR #2 (base: feat-1-model)
 ┌── feat-1-model     → PR #1 (base: main)           ← bottom
main (trunk)
```

**Terminology:** *trunk* = the stack's base branch (usually `main`) · *layer* = one PR in the
stack, a discrete reviewable change of one or more commits · *bottom* = targets trunk · *top* =
furthest from trunk · *mid-stack* = anything in between.

> Stacked PRs are in **public preview** on GitHub and subject to change.

---

## 1. Why stack — and when not to

AI coding agents are productive but tend to produce **monolithic pull requests** — one branch,
one PR, a thousand-plus changed lines spanning models, services, wiring, and UI. That leaves
reviewers a false choice: rubber-stamp a diff nobody can hold in their head, or send it back and
lose the work's momentum. A stack removes the choice: the same total change ships as a chain of
PRs where each layer is a **single concern, small enough to hold in a reviewer's head**, reviewed
and merged on its own.

**Stack when:**

- The change is too large to review as one PR, but the parts are **sequentially dependent** —
  you can't just open N parallel PRs off trunk.
- Foundational work must land before dependent work — shared types, a schema, an abstraction —
  and the follow-up can't wait for the merge.
- An agent completes one task and immediately starts the next task that builds on it.
- You want smaller diffs so review is faster and CODEOWNERS see only their layer's change.

**Do not stack when:**

- The parts are genuinely independent — open separate PRs off trunk instead; a stack adds
  ordering constraints for nothing.
- Branches would span **multiple repositories or forks** — cross-fork stacks are unsupported.
- The work is done from **GitHub Desktop** — stacks aren't supported there.
- You need a **branching (tree-shaped) stack** — only a single linear chain is supported.

---

## 2. Prerequisites — check these first

| Requirement | Needed | Verify |
|---|---|---|
| GitHub CLI | **2.90.0+** | `gh --version` |
| Git | **2.20+** | `git --version` |
| `gh-stack` extension | installed | `gh extension list` |
| Auth | logged in | `gh auth status` |

```shell
gh extension install github/gh-stack
```

⚠️ **Run the version check first.** Below `gh` 2.90.0, `gh extension install github/gh-stack`
still reports success but leaves you without a working `gh stack`, and every command in sections
5–9 fails. Either upgrade `gh` for your platform — `winget upgrade GitHub.cli`, `brew upgrade gh`,
your distro's package manager, or the [releases page](https://github.com/cli/cli/releases) — or
use the **manual fallback in section 10**, which needs no extension at all. Never skip the version
check and then report a stack as created.

Optional, for AI agents: `gh skill install github/gh-stack` (or `npx skills add github/gh-stack`)
installs GitHub's own agent skill for the extension.

---

## 3. Design the layers

The one rule that decides every boundary:

> **If code in one layer depends on code in another, the dependency must live in the same branch
> or a lower one.** Dependencies only point downward.

Cut a new layer when you switch concerns, cross a project or package boundary, move from core
logic to tests or docs, or the current branch has reached a reviewable size. A typical cut,
bottom → top:

1. **Contracts** — data models, schemas, DTOs, interfaces, validation, seed data. No behavior.
2. **Core logic** — the service or feature implementation, with its unit tests.
3. **Wiring** — dependency injection, configuration, API endpoints, client integration.
4. **Presentation** — UI, styling, templates.
5. **Polish** — end-to-end tests, docs, telemetry.

Adapt the archetype to the change; three layers are often enough. What never changes:

- **Each layer must build, pass its tests, and review on its own.** A layer that only compiles
  once the layer *above* lands is a badly cut layer — re-cut it, moving the dependency down.
- **Plan the order up front.** A stack cannot be reordered from the web UI — only via
  `gh stack modify` in the CLI.
- **Keep the stack short.** Beyond ~5 layers, rebase churn and CI cost outgrow the review
  benefit. If a single layer grows past a few hundred lines, look for a seam in it.

---

## 4. Split the problem too — one sub-issue per layer

A stack splits the *code*. If the work is tracked by an issue, split the *problem* as well —
otherwise the split is only half done: a reviewer opening layer 3 has to reconstruct why it
exists from the parent issue, which describes the whole feature, and from the sibling PRs, which
is exactly the context the stack was meant to spare them.

> **Rule: before `gh stack submit`, create one sub-issue per layer under the parent issue.** Each
> layer's PR carries `Closes` on **its own sub-issue**. Every layer references the parent issue;
> only the last one closes it.

The reviewer then starts at the sub-issue — the problem that one layer solves — and reviews that
layer's diff in isolation of both the parent and the PR's siblings. The parent's sub-issue
progress bar doubles as stack progress, ticking up as layers merge bottom-up.

Write each sub-issue so it stands on its own:

- what this layer changes, in its own terms — not "part 3 of the refactor",
- why it is a separate layer (the dependency that forced the cut),
- what it deliberately defers upward ("no UI in this layer — see #<parent>").

Keep sub-issue order matching stack order, so the two read bottom-up together.

`gh` has no sub-issue subcommand, so link through the REST API — note the POST takes the
sub-issue's **database id**, not its number:

```shell
PARENT=42

# create the layer's issue, capture its number from the returned URL
URL=$(gh issue create --title "Payment descriptor model" \
                      --body "Layer 1/4 of #$PARENT. Adds the model only; no service or UI changes.")
NUM=${URL##*/}

# resolve number -> database id, then attach it to the parent
ID=$(gh api "repos/{owner}/{repo}/issues/$NUM" --jq .id)
gh api "repos/{owner}/{repo}/issues/$PARENT/sub_issues" -F sub_issue_id="$ID"

# verify the parent now lists every layer
gh api "repos/{owner}/{repo}/issues/$PARENT/sub_issues" --jq '.[] | "\(.number)\t\(.title)"'
```

Sub-issues can equally be created from the parent issue's **Create sub-issue** button in the
web UI.

---

## 5. Build a stack from scratch

```shell
gh stack init feat-1-model      # names the bottom branch; base defaults to the repo default branch
# ...write code...
git add .
git commit -m "add payment descriptor model"

gh stack add feat-2-api         # next layer, on top of the current one
# ...write code...
git add .
git commit -m "resolve payment descriptors from the service"

gh stack submit                 # push every branch + open/update a PR per layer
```

`gh stack submit` sets each PR's base to the branch below it automatically, so each reviewer sees
only that layer's diff.

Useful flags and shortcuts:

- `gh stack init -b <branch>` — stack on a trunk other than the repo default.
- `gh stack add -Am "MESSAGE"` — stage all, commit, and create the next branch in one step
  (`-A`/`--all` and `-u`/`--update` are mutually exclusive).
- `gh stack submit --auto` — skip the editor; `--open` — mark PRs ready for review.
- `gh stack view` (`-s` short, `--json`) — branch order, PR links, latest commit per layer.

**Branch naming:** keep whatever naming the repo already uses, and suffix it per layer so the
order is obvious — `<prefix>-1-model`, `<prefix>-2-api`, `<prefix>-3-ui`. Commit messages and PR
bodies follow the repo's existing conventions; a stack changes nothing there.

---

## 6. Decompose an existing oversized branch or PR

The commonest real-world entry point: the giant change **already exists** on one branch (often
agent-generated) and needs to become a stack after the fact.

```shell
# 0. inventory what the branch touches, then plan layers on paper (section 3)
git fetch origin
git diff --stat main...big-feature

# 1. bottom layer: start the stack from trunk, pull in only that layer's files
gh stack init feat-1-model
git restore --source=big-feature -- src/models/ tests/models/
git add -A
git commit -m "add payment models"
# build + test this layer NOW — a failure means the cut is wrong; move the dependency down

# 2. repeat for each middle layer
gh stack add feat-2-service
git restore --source=big-feature -- src/services/ tests/services/
git add -A
git commit -m "add payment service"

# 3. top layer: apply everything that remains, deletions included
gh stack add feat-3-ui
git diff HEAD big-feature --binary | git apply --index
git commit -m "wire payment UI"

# 4. prove the decomposition is lossless — this MUST print nothing
git diff big-feature --exit-code

gh stack submit
```

- `git restore --source` copies **whole files**. A file that mixes two layers' concerns has to be
  split by hand: commit the lower layer's portion first, add the rest when its layer comes up.
- `git restore` cannot delete. If a lower layer removes files, `git rm` them in that layer; any
  deletion left for the top layer is covered by the `git diff | git apply --index` remainder.
- Build and test **every layer before adding the next** — that's what makes each PR
  independently mergeable, and it validates the layer boundaries while they're still cheap to fix.
- Step 4 is the safety net: an empty diff against the original branch proves no line was lost or
  invented during the split. Only delete the original branch after this passes.
- If the oversized PR is already open, close it after `gh stack submit` with a comment linking
  the bottom PR of the stack. If reviewable pieces already exist as separate open PRs, link them
  into a stack instead: `gh stack link [--base <branch>] <branch-or-pr> <branch-or-pr> ...`.

---

## 7. Navigate, update, and restack

Navigation: `gh stack up [n]` · `gh stack down [n]` · `gh stack top` · `gh stack bottom` ·
`gh stack trunk` · `gh stack switch` (interactive) ·
`gh stack checkout <stack-number | pr-number | pr-url | branch>`.

**Amend a mid-stack layer after review feedback** — the core loop:

```shell
gh stack down                     # or: gh stack checkout feat-1-model
git add .
git commit -m "guard against a null descriptor id"
gh stack rebase --upstack         # cascade the change into every branch above
gh stack push
gh stack top
```

**Rebase scopes:** bare `gh stack rebase` cascades the whole stack starting from trunk;
`--downstack` rebases from the bottom up to the current branch; `--upstack` from the current
branch to the top. Other flags: `--no-trunk`, `--remote <name>`,
`--committer-date-is-author-date`.

**Conflicts:** resolve the markers, `git add` the files, then `gh stack rebase --continue` — or
`gh stack rebase --abort` to restore the pre-rebase state. `gh stack init` enables `git rerere`,
so a conflict you resolve once is replayed automatically on later rebases.

**Restructure** (`gh stack modify`, interactive): `x` drop · `d` fold down · `u` fold up · `i`
insert below · `I` insert above · `Shift+↑`/`Shift+↓` move · `r` rename · `z` undo. Apply with
`Ctrl/Cmd+S`, then `--continue` / `--abort` for conflicts, then `gh stack submit`.

**After a lower PR merges**, resync local state:

```shell
gh stack sync --prune   # fetch, fast-forward trunk, rebase the rest, push, sync PR state, delete merged branches
```

If local and remote diverge, `gh stack sync` prompts: take remote as source of truth, delete the
stack on GitHub, or cancel. **In a non-interactive/CI shell divergence just aborts the sync** —
don't script `gh stack sync` and assume it succeeded; check the exit code.

⚠️ **Prefer the CLI over the web "Rebase stack" button.** The web rebase rewrites commits
server-side, which resets committer attribution and strips commit signatures — on repos whose
branch protection requires signed commits, that breaks every layer. `gh stack rebase` keeps
authorship and signing local and intact.

---

## 8. Review a stack

What reviewers (human or agent) should know — put this in the PR bodies if reviewers are new to
stacks:

- GitHub links the PRs of a stack with a **stack map** — a navigation panel between the layers.
  Use it as the compass: it shows where the current PR sits and what it builds on.
- **Read top-down, review bottom-up.** Skim from the top PR downward to load context on where
  the feature ends up; then review and approve starting at the bottom, since every upper layer
  builds on the lower ones being right.
- **Judge each layer only on its own concern.** Reviewing a contracts layer asks "are the types
  right, is input validated, is the query safe?" — not "where is the UI?" The UI is a higher
  layer; the sub-issue (section 4) says so.
- **Feedback on a lower layer** is fixed with the amend loop in section 7; the cascade updates
  every PR above automatically. Don't ask the author to re-open or manually rebase siblings.
- Merging strictly bottom-up (section 9) keeps every open diff scoped to exactly one layer.

---

## 9. Merge

```shell
gh stack merge [<stack-number> | <pr-number>] [--squash | --merge | --rebase | --merge-method <method>] [-y]
```

- PRs merge **bottom-up**. Merging the top PR merges the whole stack; merging a mid-stack PR
  merges it plus everything below it.
- PRs above a merged one **stay open and automatically retarget** the stack's base branch —
  GitHub performs the cascading rebase server-side (or run it locally with `gh stack rebase`).
- `gh stack merge` is **all-or-nothing**.
- Resulting history is identical to merging each PR individually from the bottom.
- Merge commit, squash, rebase, and **merge queue** are all supported.

---

## 10. Constraints, gotchas, and automation impact

- **Branch protection and CODEOWNERS are evaluated against the stack's base (trunk), not against
  the branch a PR directly targets.** Required reviews and status checks therefore apply
  uniformly to *every* layer — a two-line layer still needs its CODEOWNER approval and green CI.
- **CI runs for every PR in the stack.** A workflow triggered on `pull_request` targeting trunk
  fires for all layers, so a 5-layer stack is ~5× the Actions minutes of one PR. Existing
  workflows need no change, but you can skip redundant jobs using the stack metadata at
  `github.event.pull_request.stack`.
- **The legacy PR merge endpoints cannot merge a stack** — automation must use the Stacks API.
  Any bot, dashboard, or workflow that merges PRs programmatically will fail on stacked PRs
  until it is updated.
- REST API responses and webhooks carry a `stack` object per PR, and a `stacked` webhook action
  fires when a stack forms.
- **Stacks are single-use:** once every PR merges, the stack closes permanently. Adding branches
  on top and running `gh stack submit` starts a *new* stack on the same base.
- Same repository only — **no forks, no cross-repo, no tree-shaped stacks**.
- Reordering is **CLI-only** (`gh stack modify`); not available on the website.
- Not supported in GitHub Desktop.
- `gh stack unstack` (alias `gh stack delete`) removes a stack from tracking and unstacks it on
  GitHub; `--local` limits it to local tracking only.
- `gh stack link [--base <branch>] <stack-number | branch-or-pr> <branch-or-pr> ...` links
  already-open PRs into a stack **without** creating local tracking state.
- `GH_STACK_THEME=auto|light|dark` controls interactive screen colors; `gh stack alias` creates
  a short alias (default `gs`).

### Exit codes

| Code | Meaning | Code | Meaning |
|---|---|---|---|
| 0 | Success | 6 | Disambiguation required (branch in multiple stacks) |
| 1 | Generic error | 7 | Rebase already in progress |
| 2 | Not in a stack / stack not found | 8 | Stack locked by another process |
| 3 | Rebase conflict | 9 | Stacked PRs not enabled |
| 4 | GitHub API failure | 10 | Modify session interrupted; recovery needed |
| 5 | Invalid arguments or flags | | |

---

## 11. Fallback — stacking without the extension

Works on any `gh`/`git`, and is the path to use while `gh` is below 2.90.0. You lose cascading
rebase, `modify`, `sync`, and `gh stack merge`, but the PR chain and review benefit are
identical.

```shell
# bottom layer
git switch -c feat-1-model main
git commit -am "add payment descriptor model"
git push -u origin feat-1-model
gh pr create --base main --head feat-1-model --fill

# next layer, based on the previous branch
git switch -c feat-2-api feat-1-model
git commit -am "resolve payment descriptors from the service"
git push -u origin feat-2-api
gh pr create --base feat-1-model --head feat-2-api --fill
```

- After amending a lower branch, rebase each higher branch manually, bottom-up:
  `git rebase --onto feat-1-model <old-base> feat-2-api`, then
  `git push --force-with-lease` (**never bare `--force`**).
- After the bottom PR merges, retarget the next PR: `gh pr edit <n> --base main`, then rebase
  its branch onto the updated trunk.
- On the website you can also create the second PR with its base set to the first PR's branch
  and choose **Create stack** to link them; GitHub also shows a banner offering to link
  already-aligned PRs, and any PR in a stack has a stack icon → **Add to stack**.

---

## 12. Working agreements for agents

An agent building a stack works **one layer at a time, bottom-up**:

```text
for each layer, bottom → top:
  1. implement ONLY this layer's concern
  2. run the repo's build, tests, and linters for this layer
  3. green?  commit, `gh stack add` the next layer
     red?    iterate HERE — never start the next layer on a failing one
```

- Every layer's PR body states its position and dependency so reviewers know what they're
  looking at — e.g. `Stack 2/4 — depends on #101. Closes #108. Review only this layer's diff.` —
  and otherwise follows the repo's normal commit and PR conventions.
- Each layer `Closes` its own sub-issue (section 4); only the last layer closes the parent.
- Do not submit a layer whose build or tests fail — status checks apply per layer against trunk,
  so a red layer blocks everything above it.
- Layers make natural hand-off points for **specialized agents** — a data-modeling agent on the
  contracts layer, a backend agent on the service layer, a frontend agent on the UI layer — each
  producing an independently reviewable PR.
- After any mid-stack change: `gh stack rebase --upstack` → `gh stack push`. After a lower merge:
  `gh stack sync --prune`.

---

## Quick checklist

- [ ] `gh --version` ≥ 2.90.0 and `gh-stack` installed — or the section 11 fallback used deliberately.
- [ ] Layers cut so each one builds, tests, and reviews on its own; dependencies only point downward.
- [ ] Layer order planned up front (web UI can't reorder); stack kept to ~5 layers or fewer.
- [ ] One sub-issue per layer created under the parent issue, in stack order.
- [ ] Branch names carry the layer order (`…-1-model`, `…-2-api`, …).
- [ ] Decomposing an existing branch: `git diff <big-branch> --exit-code` empty before deleting it.
- [ ] `gh stack submit` run; each PR body `Closes` its own sub-issue and states its `Stack n/N` position.
- [ ] After any mid-stack edit: `gh stack rebase --upstack` → `gh stack push`.
- [ ] After a lower merge: `gh stack sync --prune`.
- [ ] Merged bottom-up via `gh stack merge` (legacy merge automation will not work).

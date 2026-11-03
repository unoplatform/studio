# Shared by Test-EvalCases.ps1 (checks the cases without running them) and Invoke-Evals.ps1 (runs the suites with
# `claude plugin eval`, in the "Evals" workflow and locally). Dot-source it.

$EvalRoot = (Resolve-Path "$PSScriptRoot/../..").Path
$EvalPlugin = 'plugins/uno-platform-studio'

# A case passes when its score (the mean over its runs of the fraction of graders passed) is at least this.
$EvalThreshold = 0.8
# One arm, the plugin's: CI asks whether the skills give right answers, not how much they add. Under --ablation none
# the "skill loaded" graders count toward the score too.
$EvalAblation = 'none'
# The agent and the judge, pinned so that scores compare from one run to the next.
$EvalModel = 'claude-sonnet-5-5'
$EvalJudgeModel = 'claude-sonnet-5-5'

# A suite's folder name is also a `claude` argument and a GitHub Actions step output; Test-EvalCases.ps1 enforces this.
$EvalSuiteNamePattern = '^[a-z0-9][a-z0-9-]*$'

# A suite is a folder under evals/ holding cases (<suite>/<case>/prompt.md), each tagged with the folder name, the tag
# Invoke-Evals.ps1 selects the suite's cases by.
function Get-EvalSuites {
    Get-ChildItem (Join-Path $EvalRoot 'evals') -Directory |
        Where-Object { Get-ChildItem $_.FullName -Filter prompt.md -Recurse -Depth 1 | Select-Object -First 1 } |
        ForEach-Object Name | Sort-Object
}

function Get-EvalCases([string] $suite) {
    Get-ChildItem (Join-Path $EvalRoot "evals/$suite") -Directory |
        Where-Object { Test-Path (Join-Path $_.FullName 'prompt.md') } |
        ForEach-Object { "evals/$suite/$($_.Name)" } | Sort-Object
}

# The SKILL.md files every skill suite depends on besides its own skill: uno-platform, which agents load first and
# whose routing table picks the other skills, and uno-build-app, which that table sends feature work to before any
# domain skill. Not their references, which only the workflow's own steps read.
$EvalSharedSkillFiles = @("$EvalPlugin/skills/uno-platform/SKILL.md", "$EvalPlugin/skills/uno-build-app/SKILL.md")

# The hooks an eval session can run: hooks.json, which wires every hook (so adding or rewiring one counts), and the
# SessionStart hook. The PostToolUse hook fires only after an edit, and no case allows Edit or Write.
$EvalHookFiles = @("$EvalPlugin/hooks/hooks.json", "$EvalPlugin/hooks/session-start.sh")

# How the evals run: the settings above, the runner, and the workflow's Claude Code version. A change to any of them can
# change every result.
$EvalHarnessFiles = @('.github/scripts/Evals.Common.ps1', '.github/scripts/Invoke-Evals.ps1', '.github/workflows/evals.yml')

# What a suite's result depends on, so a change to any of these runs it. A skill suite: its cases, its skill and
# $EvalSharedSkillFiles. routing: its cases, and the frontmatter of every SKILL.md (Test-EvalFrontmatterChanged); a
# skill's body cannot change a case that passes only when no skill loads. Every suite: the hooks and the harness. Not
# plugin.json: releases bump its version without changing behavior. Not the uno-dev agent: an eval can't load it.
function Get-EvalInputPaths([string] $suite) {
    $paths = @("evals/$suite") + $EvalHookFiles + $EvalHarnessFiles
    if ($suite -ne 'routing') { $paths += @("$EvalPlugin/skills/$suite") + $EvalSharedSkillFiles }
    $paths
}

function Get-EvalSkillFiles {
    Get-ChildItem (Join-Path $EvalRoot "$EvalPlugin/skills") -Directory | ForEach-Object { "$EvalPlugin/skills/$($_.Name)/SKILL.md" }
}

# The frontmatter of a SKILL.md's text, with LF endings, or '' when it has none.
function Get-EvalFrontmatter([string] $text) {
    [regex]::Match(($text -replace "`r`n", "`n"), '(?s)\A---\n.*?\n---\n').Value
}

# A file's text at a commit, or '' when it doesn't exist there.
function Get-EvalFileAt([string] $commit, [string] $path) {
    $lines = git -C $EvalRoot show "${commit}:$path" 2>$null
    $LASTEXITCODE ? '' : (($lines -join "`n") + "`n")
}

# Whether a SKILL.md's frontmatter differs between commit $from and the working tree. An agent picks a skill from its
# name and description alone, so this is the part of a skill the routing cases depend on.
function Test-EvalFrontmatterChanged([string] $path, [string] $from) {
    $file = Join-Path $EvalRoot $path
    $now = (Test-Path -LiteralPath $file) ? (Get-Content -LiteralPath $file -Raw) : ''
    (Get-EvalFrontmatter (Get-EvalFileAt $from $path)) -ne (Get-EvalFrontmatter $now)
}

# Where the branch left $base: changes are measured from here, so commits on $base since then don't count.
function Get-EvalMergeBase([string] $base) {
    $from = git -C $EvalRoot merge-base $base HEAD
    if ($LASTEXITCODE) { throw "No common ancestor of $base and HEAD; fetch $base, or name the suites with -Suite." }
    $from
}

# The files that differ between commit $from and the working tree: committed, staged and unstaged changes, and
# untracked files. CI's checkout has only the first; a contributor's working tree can have all of them.
function Get-EvalChangedPaths([string] $from) {
    @(git -C $EvalRoot diff --name-only --no-renames $from) + @(git -C $EvalRoot ls-files --others --exclude-standard) |
        Sort-Object -Unique
}

function Test-EvalPathUnder([string] $path, [string[]] $roots) {
    foreach ($root in $roots) {
        if ($path -ceq $root -or $path.StartsWith("$root/", [StringComparison]::Ordinal)) { return $true }
    }
    $false
}

# The suites a change to $paths since commit $from can affect, in Get-EvalSuites order.
function Get-EvalSuitesForChanges([string[]] $paths, [string] $from) {
    $skillFiles = @($paths | Where-Object { $_ -like "$EvalPlugin/skills/*/SKILL.md" })
    foreach ($suite in Get-EvalSuites) {
        $inputs = @(Get-EvalInputPaths $suite)
        $hit = @($paths | Where-Object { Test-EvalPathUnder $_ $inputs }).Count -gt 0
        if (-not $hit -and $suite -eq 'routing') {
            $hit = @($skillFiles | Where-Object { Test-EvalFrontmatterChanged $_ $from }).Count -gt 0
        }
        if ($hit) { $suite }
    }
}

# A key for a suite's inputs as committed at HEAD: the git object id of each input path, and for routing every
# SKILL.md's frontmatter. Invoke-Evals.ps1 -PassedDir skips a suite that already passed with the same key.
function Get-EvalInputsKey([string] $suite) {
    $lines = @(foreach ($path in Get-EvalInputPaths $suite) { "$path`t$(git -C $EvalRoot rev-parse --verify --quiet "HEAD:$path")" })
    if ($suite -eq 'routing') {
        $lines += @(foreach ($path in Get-EvalSkillFiles) { "$path#frontmatter`t$(Get-EvalFrontmatter (Get-EvalFileAt HEAD $path))" })
    }
    $bytes = [Text.Encoding]::UTF8.GetBytes($lines -join "`n")
    [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).Substring(0, 16).ToLowerInvariant()
}

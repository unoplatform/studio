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

# A suite is a folder under evals/ holding cases (<suite>/<case>/prompt.md), tagged with the folder name.
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

# What a suite's result depends on, so a change to any of these reruns it. A skill suite: its cases, its skill and
# $EvalSharedSkillFiles. routing: its cases, and the frontmatter of every SKILL.md (Test-EvalFrontmatterChanged); a
# skill's body cannot change a case that passes only when no skill loads. Every suite: the plugin's hooks, which run in
# every session. Not plugin.json: releases bump its version without changing behavior.
function Get-EvalInputPaths([string] $suite) {
    $paths = @("evals/$suite", "$EvalPlugin/hooks")
    if ($suite -ne 'routing') { $paths += @("$EvalPlugin/skills/$suite") + $EvalSharedSkillFiles }
    $paths
}

# The frontmatter of a SKILL.md's text, with LF endings, or '' when it has none (or the file doesn't exist).
function Get-EvalFrontmatter([string] $text) {
    [regex]::Match(($text -replace "`r`n", "`n"), '(?s)\A---\n.*?\n---\n').Value
}

# Whether a SKILL.md's frontmatter differs between $base and the working tree. An agent picks a skill from its name and
# description alone, so this is the part of a skill the routing cases depend on.
function Test-EvalFrontmatterChanged([string] $path, [string] $base) {
    $old = (git -C $EvalRoot show "${base}:$path" 2>$null) -join "`n"
    $file = Join-Path $EvalRoot $path
    $new = (Test-Path -LiteralPath $file) ? (Get-Content -LiteralPath $file -Raw) : ''
    (Get-EvalFrontmatter "$old`n") -ne (Get-EvalFrontmatter $new)
}

# The files that differ between where the branch left $base and the working tree: committed, staged and unstaged
# changes, and untracked files. CI's checkout has only the first; a contributor's working tree can have all of them.
function Get-EvalChangedPaths([string] $base) {
    $from = git -C $EvalRoot merge-base $base HEAD
    if ($LASTEXITCODE) { throw "No common ancestor of $base and HEAD; fetch $base, or name the suites with -Suite." }
    @(git -C $EvalRoot diff --name-only --no-renames $from) + @(git -C $EvalRoot ls-files --others --exclude-standard) |
        Sort-Object -Unique
}

function Test-EvalPathUnder([string] $path, [string[]] $roots) {
    foreach ($root in $roots) {
        if ($path -ceq $root -or $path.StartsWith("$root/", [StringComparison]::Ordinal)) { return $true }
    }
    $false
}

# The suites a change to $paths can affect, in Get-EvalSuites order. $base is where the paths changed from, to tell a
# SKILL.md's frontmatter change (which reruns routing) from a body change (which doesn't).
function Get-EvalSuitesForChanges([string[]] $paths, [string] $base) {
    $skillFiles = @($paths | Where-Object { $_ -like "$EvalPlugin/skills/*/SKILL.md" })
    foreach ($suite in Get-EvalSuites) {
        $inputs = @(Get-EvalInputPaths $suite)
        $hit = @($paths | Where-Object { Test-EvalPathUnder $_ $inputs }).Count -gt 0
        if (-not $hit -and $suite -eq 'routing') {
            $hit = @($skillFiles | Where-Object { Test-EvalFrontmatterChanged $_ $base }).Count -gt 0
        }
        if ($hit) { $suite }
    }
}

# Shared by Update-EvalReport.ps1 (writes evals/reports/<suite>.json from a local `claude plugin eval` run) and
# Test-EvalReports.ps1 (the CI check that every report is fresh, complete and passing). Dot-source it.

$EvalRoot = (Resolve-Path "$PSScriptRoot/../..").Path
$EvalPlugin = 'plugins/uno-platform-studio'
$EvalReportDir = 'evals/reports'

# A case passes when its with-plugin score (the mean over its runs of the fraction of graders passed) is at least this.
$EvalThreshold = 0.8
# One arm, the plugin's: the gate asks whether the skills give right answers, not how much they add. Under
# --ablation none the "skill loaded" graders count toward the score too.
$EvalAblation = 'none'
# A case's runs when its prompt.md sets none (claude plugin eval's default).
$EvalDefaultRuns = 3

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

# What a suite's result depends on. A skill suite: its cases and its skill. routing: its cases and the frontmatter of
# every SKILL.md (see Get-EvalFrontmatterPaths). Every suite: the plugin's hooks, which run in every session. Not
# plugin.json: releases bump its version without changing behavior.
function Get-EvalInputPaths([string] $suite) {
    $paths = @("evals/$suite", "$EvalPlugin/hooks")
    if ($suite -ne 'routing') { $paths += "$EvalPlugin/skills/$suite" }
    $paths
}

# The SKILL.md files whose frontmatter, not body, a suite depends on. The routing cases check that no Uno skill loads,
# and an agent picks a skill from its name and description alone, so editing a skill's body leaves them as they were.
function Get-EvalFrontmatterPaths([string] $suite) {
    if ($suite -ne 'routing') { return @() }
    Get-ChildItem (Join-Path $EvalRoot "$EvalPlugin/skills") -Directory | ForEach-Object { "$EvalPlugin/skills/$($_.Name)/SKILL.md" }
}

# SHA-256 over "<path>\t<git blob id>" lines for the suite's input files, and "<path>#frontmatter\t<SHA-256>" lines for
# the files it depends on only through their frontmatter. git hash-object applies the repo's line-ending rules, and the
# frontmatter is hashed with LF endings, so a contributor's working tree and the CI checkout hash the same content alike.
function Get-EvalInputsHash([string] $suite) {
    Push-Location $EvalRoot
    try {
        [string[]] $files = @(git ls-files --cached --others --exclude-standard --deduplicate -- (Get-EvalInputPaths $suite) |
            Where-Object { Test-Path -LiteralPath $_ -PathType Leaf })
        if ($LASTEXITCODE) { throw "git ls-files failed for suite $suite" }
        # Ordinal, not Sort-Object's culture-aware order, so every OS orders the paths alike.
        [Array]::Sort($files, [StringComparer]::Ordinal)
        $blobs = @($files | git hash-object --stdin-paths)
        if ($LASTEXITCODE) { throw "git hash-object failed for suite $suite" }
        $lines = @(for ($i = 0; $i -lt $files.Count; $i++) { "$($files[$i])`t$($blobs[$i])" })
        [string[]] $frontmatterFiles = @(Get-EvalFrontmatterPaths $suite)
        [Array]::Sort($frontmatterFiles, [StringComparer]::Ordinal)
        foreach ($file in $frontmatterFiles) {
            $text = (Get-Content -LiteralPath $file -Raw) -replace "`r`n", "`n"
            $front = [regex]::Match($text, '(?s)\A---\n.*?\n---\n')
            if (-not $front.Success) { throw "$file has no frontmatter" }
            $lines += "$file#frontmatter`t$([Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($front.Value))).ToLowerInvariant())"
        }
        $bytes = [Text.Encoding]::UTF8.GetBytes(($lines -join "`n") + "`n")
        [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
    } finally {
        Pop-Location
    }
}

function Get-EvalCaseRuns([string] $caseDir) {
    $front = Get-Content (Join-Path $EvalRoot "$caseDir/prompt.md") -Raw
    if ($front -match '(?m)^runs:\s*(\d+)\s*$') { [int]$Matches[1] } else { $EvalDefaultRuns }
}

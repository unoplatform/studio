#Requires -Version 7
<#
.SYNOPSIS
  Runs the eval suites a change affects, or the ones named, with `claude plugin eval`, and summarizes the result.

.DESCRIPTION
  Run it locally to check a change to a skill or an eval case before you push. With no -Suite it picks the suites
  whose inputs (Get-EvalInputPaths in Evals.Common.ps1) changed since -Base. It runs them with the plugin only, with
  the agent and judge set in Evals.Common.ps1, and fails when a case scores below the threshold or doesn't run, or the
  run stops early, such as at the cost ceiling. See evals/README.md.

  It uses your Claude Code login. At API prices a suite costs $0.50 to $3.20, and all ten about $19 (2026-11-03).

.PARAMETER Suite
  Folders under evals/ (uno-mvux, routing, ...), or all. Defaults to the suites whose inputs changed since -Base.

.PARAMETER Base
  The ref the change will merge into, used to pick the suites when -Suite is not given. Defaults to origin/main.

.PARAMETER List
  Print the suites that would run, without running them.

.PARAMETER Concurrency
  Agent runs at once, passed to `claude plugin eval --concurrency`.

.PARAMETER MaxCostUsd
  The run's cost ceiling, passed to `claude plugin eval --max-cost-usd`. Defaults to $5 a suite, about 1.5 times the
  most a suite has cost.

.PARAMETER OutputDir
  Where `claude plugin eval` writes aggregate-result.json and report.html. Defaults to evals/results/<UTC time>.

.EXAMPLE
  pwsh .github/scripts/Invoke-Evals.ps1 -List              # which suites your branch changes, without running them

.EXAMPLE
  pwsh .github/scripts/Invoke-Evals.ps1                    # run the suites your branch changes

.EXAMPLE
  pwsh .github/scripts/Invoke-Evals.ps1 -Suite uno-mvux    # one suite
#>
[CmdletBinding()]
param(
    [string[]] $Suite,
    [string] $Base = 'origin/main',
    [switch] $List,
    [ValidateRange(1, 8)] [int] $Concurrency = 4,
    [double] $MaxCostUsd,
    [string] $OutputDir
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/Evals.Common.ps1"

$suites = @(Get-EvalSuites)
# `pwsh -File Invoke-Evals.ps1 -Suite a,b` passes "a,b" as one string.
$Suite = @($Suite | ForEach-Object { $_ -split ',' } | ForEach-Object Trim | Where-Object { $_ })
if ($Suite -contains 'all') {
    $Suite = $suites
} elseif (-not $Suite) {
    $from = Get-EvalMergeBase $Base
    $Suite = @(Get-EvalSuitesForChanges @(Get-EvalChangedPaths $from) $from)
    Write-Host ($Suite ? "Suites whose inputs changed since ${Base}: $($Suite -join ', ')" : "No eval suite's inputs changed since $Base.")
}
# Every name is checked before the paid run starts.
$unknown = @($Suite | Where-Object { $_ -notin $suites })
if ($unknown) { throw "No eval suite named $($unknown -join ', ') under evals/. The suites are: $($suites -join ', ')." }
if ($List -or -not $Suite) { return }

if (-not $OutputDir) { $OutputDir = "evals/results/$((Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH-mm-ss'))Z" }
$OutputDir = [IO.Path]::GetFullPath($OutputDir, $EvalRoot)
if (-not $MaxCostUsd) { $MaxCostUsd = 5 * $Suite.Count }
# The target comes before --tag, whose list runs to the next option.
$evalArgs = @('plugin', 'eval', '.', '--tag') + $Suite + @(
    '--ablation', $EvalAblation, '--threshold', $EvalThreshold, '--model', $EvalModel, '--judge-model', $EvalJudgeModel,
    '--concurrency', $Concurrency, '--max-cost-usd', $MaxCostUsd, '--output-dir', $OutputDir, '--trust-plugin',
    '--no-publish')

Push-Location $EvalRoot
try {
    & claude @evalArgs
    $exit = $LASTEXITCODE
} finally {
    Pop-Location
}

$resultFile = Join-Path $OutputDir 'aggregate-result.json'
if (-not (Test-Path $resultFile)) { throw "claude plugin eval wrote no result (exit $exit)." }
$result = Get-Content $resultFile -Raw | ConvertFrom-Json

# One row per case of the suites run, with the graders that failed and in how many runs, so you see why a case lost
# points without opening the report. A case missing from the result (not tagged with its suite, or not reached by a run
# that stopped early) fails too.
$byDir = @{}
foreach ($case in $result.cases) { $byDir[$case.dir -replace '\\', '/'] = $case }
$rows = @()
$failedSuites = @()
$allRuns = @()
foreach ($dir in $Suite | ForEach-Object { Get-EvalCases $_ }) {
    $name = $dir -replace '^evals/', ''
    $case = $byDir[$dir]
    if ($case) {
        $runs = @($case.arms.with)
        $allRuns += $runs
        $why = @($runs.graders | Where-Object { $_.scored -and -not $_.passed } | Group-Object name | Sort-Object Name |
            ForEach-Object { "$($_.Name) $($_.Count)/$($runs.Count)" })
        $errored = @($runs | Where-Object error)
        if ($errored) {
            # The first error's text, made safe for a table cell.
            $text = $errored[0].error -replace '\s+', ' ' -replace '\|', '\|'
            if ($text.Length -gt 160) { $text = $text.Substring(0, 160) + '…' }
            $why += "run error $($errored.Count)/$($runs.Count): $text"
        }
        $why = $why -join ', '
        $score = '{0:0.00}' -f $case.aggregates.score
        $ok = $case.aggregates.score -ge $EvalThreshold
    } else {
        $why = $result.partial ? 'not run: the run stopped early' : "not run: its prompt.md tags don't include its suite"
        $score = '—'
        $ok = $false
    }
    $rows += "| ``$name`` | $($ok ? '✅' : '❌') $score | $why |"
    if (-not $ok) { $failedSuites += $dir.Split('/')[1] }
}
# claude plugin eval exits 0 when every case it ran passed; a case it didn't run fails here.
if ($failedSuites -and -not $exit) { $exit = 1 }

$minutes = [math]::Max(1, [math]::Round($result.durationSeconds / 60))
$lines = @(
    "### Evals: $(@($rows | Where-Object { $_ -like '*✅*' }).Count) of $($rows.Count) cases at or above $EvalThreshold"
    ''
    "Suites: $($Suite -join ', '). Agent ``$($result.suite.modelOverride)``, judge ``$($result.suite.judgeModel)``, " +
    "Claude Code $($result.claudeVersion). `$$('{0:0.00}' -f $result.costUsd) at API prices, $minutes min."
)
if ($result.partial) { $lines += '', "**The run stopped early ($($result.partialReason)).**" }
if ($allRuns -and -not @($allRuns | Where-Object { -not $_.error })) {
    # When no run could run, the cause is the login, a rate limit or a spend limit, not the change under test.
    $lines += '', '**Every run errored, so these scores say nothing about the skills.** Check your Claude Code login and its limits.'
}
if ($exit -and -not $failedSuites) { $lines += '', "``claude plugin eval`` exited $exit; its log says why." }
$lines += '', '| Case | Score | Failed graders (runs) |', '|---|---|---|'
$lines += $rows
$lines += '', "The full report, with each judge's reasoning, is ``report.html`` in $OutputDir."
$lines | ForEach-Object { Write-Host $_ }
exit $exit

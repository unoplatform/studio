#Requires -Version 7
<#
.SYNOPSIS
  Runs the eval suites a change affects, or the ones named, with `claude plugin eval`, and summarizes the result.

.DESCRIPTION
  The "Evals" workflow runs this on pull requests and weekly on main; run it locally to check a change before you push.
  With no -Suite it picks the suites whose inputs (Get-EvalInputPaths in Evals.Common.ps1) changed since -Base. It runs
  them with the plugin only, with the agent and judge set in Evals.Common.ps1, and exits non-zero when a case scores
  below the threshold or the run stops early, such as at -MaxCostUsd. See evals/README.md.

  It uses the Claude credentials in the environment: ANTHROPIC_API_KEY in CI, your Claude Code login locally. At API
  prices a suite costs $0.50 to $3.20, and all ten about $19 (2026-11-03).

.PARAMETER Suite
  Folders under evals/ (uno-mvux, routing, ...), or all. Defaults to the suites whose inputs changed since -Base.

.PARAMETER Base
  The ref the change will merge into, used to pick the suites when -Suite is not given. Defaults to origin/main.

.PARAMETER List
  Print the suites that would run, without running them. In GitHub Actions it also sets the step output `suites`.

.EXAMPLE
  pwsh .github/scripts/Invoke-Evals.ps1                    # the suites your branch changes

.EXAMPLE
  pwsh .github/scripts/Invoke-Evals.ps1 -Suite uno-mvux    # one suite
#>
[CmdletBinding()]
param(
    [string[]] $Suite,
    [string] $Base,
    [switch] $List,
    [ValidateRange(1, 8)] [int] $Concurrency = 4,
    [double] $MaxCostUsd = 30,
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
    # The workflow passes an empty -Base outside pull requests.
    if (-not $Base) { $Base = 'origin/main' }
    $Suite = @(Get-EvalSuitesForChanges @(Get-EvalChangedPaths $Base) $Base)
    Write-Host ($Suite ? "Suites whose inputs changed since ${Base}: $($Suite -join ', ')" : "No eval suite's inputs changed since $Base.")
}
# Every name is checked before the paid run starts.
$unknown = @($Suite | Where-Object { $_ -notin $suites })
if ($unknown) { throw "No eval suite named $($unknown -join ', ') under evals/. The suites are: $($suites -join ', ')." }

if ($List) {
    if ($env:GITHUB_OUTPUT) { "suites=$($Suite -join ',')" | Add-Content $env:GITHUB_OUTPUT }
    return
}
if (-not $Suite) { return }

if (-not $OutputDir) { $OutputDir = "evals/results/$((Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH-mm-ss'))Z" }
$OutputDir = [IO.Path]::GetFullPath($OutputDir, $EvalRoot)
# The target comes before --tag, whose list runs to the next option.
$evalArgs = @('plugin', 'eval', '.', '--tag') + $Suite + @(
    '--ablation', $EvalAblation, '--threshold', $EvalThreshold, '--model', $EvalModel, '--judge-model', $EvalJudgeModel,
    '--concurrency', $Concurrency, '--max-cost-usd', $MaxCostUsd, '--output-dir', $OutputDir, '--trust-plugin')
# An API key can't publish the report to claude.ai; the workflow uploads it as an artifact instead.
if ($env:GITHUB_ACTIONS) { $evalArgs += '--no-publish' }

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

# A Markdown table of every case, with the graders that failed and in how many of its runs, so a reviewer sees why a
# case lost points without opening the report.
$cases = @($result.cases | Sort-Object dir)
$minutes = [math]::Max(1, [math]::Round($result.durationSeconds / 60))
$cost = '{0:0.00}' -f $result.costUsd
$lines = @(
    "### Evals: $($result.aggregates.casesPassed) of $($result.aggregates.casesTotal) cases at or above $EvalThreshold"
    ''
    "Suites: $($Suite -join ', '). Agent ``$($result.suite.modelOverride)``, judge ``$($result.suite.judgeModel)``, " +
    "Claude Code $($result.claudeVersion). `$$cost at API prices, $minutes min."
)
if ($result.partial) { $lines += '', "**The run stopped early ($($result.partialReason)).** Cases it didn't finish are missing or score low." }
$lines += '', '| Case | Score | Failed graders (runs) |', '|---|---|---|'
$annotations = @()
foreach ($case in $cases) {
    $dir = $case.dir -replace '\\', '/' -replace '^evals/', ''
    $runs = @($case.arms.with)
    $failed = @($runs.graders | Where-Object { $_.scored -and -not $_.passed } | Group-Object name | Sort-Object Name |
        ForEach-Object { "$($_.Name) $($_.Count)/$($runs.Count)" })
    $errored = @($runs | Where-Object error)
    if ($errored) { $failed += "run error $($errored.Count)/$($runs.Count)" }
    $score = '{0:0.00}' -f $case.aggregates.score
    $passed = $case.aggregates.score -ge $EvalThreshold
    $lines += "| ``$dir`` | $($passed ? '✅' : '❌') $score | $($failed -join ', ') |"
    if (-not $passed) { $annotations += "::error title=Eval case below $EvalThreshold::$dir scored $score (failed: $($failed -join ', '))" }
}
$lines += '', "The full report, with each judge's reasoning, is ``report.html`` in $($env:GITHUB_ACTIONS ? 'the eval-results artifact' : $OutputDir)."

if ($env:GITHUB_STEP_SUMMARY) {
    $lines | Add-Content $env:GITHUB_STEP_SUMMARY
    $annotations | ForEach-Object { Write-Host $_ }
} else {
    Write-Host "`nReport: $(Join-Path $OutputDir 'report.html')"
}
exit $exit

#Requires -Version 7
<#
.SYNOPSIS
  Runs an eval suite with `claude plugin eval` and writes evals/reports/<suite>.json, the report CI checks.

.DESCRIPTION
  CI holds no model credentials, so it doesn't run the evals: the author of a change runs this, commits the report with
  the change, and Test-EvalReports.ps1 checks that the report matches the files it ran against, covers every case, and
  passes. See evals/README.md.

  The agent and judge models are the ones the gate requires ($EvalModel and $EvalJudgeModel in
  EvalReports.Common.ps1). To try another model, run `claude plugin eval` directly (evals/README.md): a report from it
  would fail the gate.

.PARAMETER Suite
  Folder under evals/ (uno-mvux, routing, ...). Defaults to every suite whose report is stale or missing.

.EXAMPLE
  pwsh .github/scripts/Update-EvalReport.ps1 -Suite uno-mvux
#>
[CmdletBinding()]
param(
    [string[]] $Suite,
    [ValidateRange(1, 8)] [int] $Concurrency = 4
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/EvalReports.Common.ps1"

$suites = @(Get-EvalSuites)
# `pwsh -File Update-EvalReport.ps1 -Suite a,b` passes "a,b" as one string.
$Suite = @($Suite | ForEach-Object { $_ -split ',' } | ForEach-Object Trim | Where-Object { $_ })
if (-not $Suite) {
    # The reasons Test-EvalReports.ps1 fails a report on, so a model change reruns every suite too.
    $Suite = @($suites | Where-Object { Get-EvalReportStaleReason $_ })
    if (-not $Suite) { Write-Host 'Every eval report is up to date.'; return }
    Write-Host "To rerun: $($Suite -join ', ')"
}
# Every name is checked before the first paid run starts.
$unknown = @($Suite | Where-Object { $_ -notin $suites })
if ($unknown) { throw "No eval suite named $($unknown -join ', ') under evals/. The suites are: $($suites -join ', ')." }
$resume = 'Reports already written are kept: rerun with no -Suite to resume.'

$failed = @()
$totalCost = 0
foreach ($name in $Suite) {
    $hash = Get-EvalInputsHash $name
    $json = Join-Path ([IO.Path]::GetTempPath()) "eval-$name-$([guid]::NewGuid()).json"

    Write-Host "`n=== $name" -ForegroundColor Cyan
    Push-Location $EvalRoot
    try {
        # --json makes the run quiet on stdout; stderr still carries progress and the published report's URL.
        $log = & claude plugin eval . --tag $name --ablation $EvalAblation --threshold $EvalThreshold `
            --model $EvalModel --judge-model $EvalJudgeModel --concurrency $Concurrency --trust-plugin --json $json 2>&1 |
            ForEach-Object { Write-Host $_; "$_" }
        $exit = $LASTEXITCODE
        if (-not (Test-Path $json)) { throw "claude plugin eval wrote no result for $name (exit $exit). $resume" }
        $result = Get-Content $json -Raw | ConvertFrom-Json
    } finally {
        Pop-Location
        Remove-Item $json -ErrorAction SilentlyContinue
    }
    $totalCost += $result.costUsd
    if ($result.partial) { throw "The $name run stopped early ($($result.partialReason)); no report written. $resume" }
    if ((Get-EvalInputsHash $name) -ne $hash) { throw "Files under $name's inputs changed during the run; no report written. $resume" }

    $url = $log | Select-String -Pattern 'Published:\s*(\S+)' | Select-Object -Last 1 | ForEach-Object { $_.Matches[0].Groups[1].Value }
    $report = [ordered]@{
        suite         = $name
        inputsHash    = $hash
        ranAt         = $result.startedAt
        claudeVersion = $result.claudeVersion
        pluginVersion = @($result.suite.plugins)[0].version
        # The values passed to --model and --judge-model: the documented --json result records neither.
        model         = $EvalModel
        judgeModel    = $EvalJudgeModel
        ablation      = $result.suite.ablation
        costUsd       = [math]::Round($result.costUsd, 2)
        reportUrl     = $url
        cases         = @($result.cases | Sort-Object dir | ForEach-Object { ConvertTo-EvalReportCase $_ })
    }
    $out = Join-Path $EvalRoot "$EvalReportDir/$name.json"
    New-Item -ItemType Directory -Force (Split-Path $out) | Out-Null
    $report | ConvertTo-Json -Depth 5 | Set-Content $out
    Write-Host "Wrote $EvalReportDir/$name.json ($($result.aggregates.casesPassed)/$($result.aggregates.casesTotal) cases at or above $EvalThreshold)."
    if ($exit -ne 0) { $failed += $name }
}

Write-Host "`nRan $($Suite.Count) suite(s) for `$$([math]::Round($totalCost, 2))."
if ($failed) {
    Write-Host "Below the threshold or failed to run: $($failed -join ', '). CI rejects these reports until the cases pass." -ForegroundColor Yellow
    exit 1
}

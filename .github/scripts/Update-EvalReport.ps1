#Requires -Version 7
<#
.SYNOPSIS
  Runs an eval suite with `claude plugin eval` and writes evals/reports/<suite>.json, the report CI checks.

.DESCRIPTION
  CI holds no model credentials, so it doesn't run the evals: the author of a change runs this, commits the report with
  the change, and Test-EvalReports.ps1 checks that the report matches the files it ran against, covers every case, and
  passes. See evals/README.md.

.PARAMETER Suite
  Folder under evals/ (uno-mvux, routing, ...). Defaults to every suite whose report is stale or missing.

.EXAMPLE
  pwsh .github/scripts/Update-EvalReport.ps1 -Suite uno-mvux
#>
[CmdletBinding()]
param(
    [string[]] $Suite,
    [string] $Model = 'claude-sonnet-5-5',
    [string] $JudgeModel = 'claude-sonnet-5-5',
    [ValidateRange(1, 8)] [int] $Concurrency = 4
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/EvalReports.Common.ps1"

if (-not $Suite) {
    $Suite = Get-EvalSuites | Where-Object {
        $path = Join-Path $EvalRoot "$EvalReportDir/$_.json"
        -not (Test-Path $path) -or (Get-Content $path -Raw | ConvertFrom-Json).inputsHash -ne (Get-EvalInputsHash $_)
    }
    if (-not $Suite) { Write-Host 'Every eval report is up to date.'; return }
    Write-Host "Stale or missing: $($Suite -join ', ')"
}

$failed = @()
foreach ($name in $Suite) {
    if ($name -notin (Get-EvalSuites)) { throw "No eval suite named '$name' under evals/." }
    $hash = Get-EvalInputsHash $name
    $json = Join-Path ([IO.Path]::GetTempPath()) "eval-$name-$([guid]::NewGuid()).json"

    Write-Host "`n=== $name" -ForegroundColor Cyan
    Push-Location $EvalRoot
    try {
        # --json makes the run quiet on stdout; stderr still carries progress and the published report's URL.
        $log = & claude plugin eval . --tag $name --ablation $EvalAblation --threshold $EvalThreshold `
            --model $Model --judge-model $JudgeModel --concurrency $Concurrency --trust-plugin --json $json 2>&1 |
            ForEach-Object { Write-Host $_; "$_" }
        $exit = $LASTEXITCODE
    } finally {
        Pop-Location
    }
    if (-not (Test-Path $json)) { throw "claude plugin eval wrote no result for $name (exit $exit)." }
    $result = Get-Content $json -Raw | ConvertFrom-Json
    Remove-Item $json
    if ($result.partial) { throw "The $name run stopped early ($($result.partialReason)); no report written." }
    if ((Get-EvalInputsHash $name) -ne $hash) { throw "Files under $name's inputs changed during the run; run it again." }

    $url = $log | Select-String -Pattern 'Published:\s*(\S+)' | Select-Object -Last 1 | ForEach-Object { $_.Matches[0].Groups[1].Value }
    $report = [ordered]@{
        suite         = $name
        inputsHash    = $hash
        ranAt         = $result.startedAt
        claudeVersion = $result.claudeVersion
        pluginVersion = @($result.suite.plugins)[0].version
        model         = $Model
        judgeModel    = $JudgeModel
        ablation      = $result.suite.ablation
        costUsd       = [math]::Round($result.costUsd, 2)
        reportUrl     = $url
        cases         = @($result.cases | Sort-Object dir | ForEach-Object {
            [ordered]@{ dir = $_.dir; runs = @($_.arms.with).Count; score = [math]::Round($_.aggregates.score, 3) }
        })
    }
    $out = Join-Path $EvalRoot "$EvalReportDir/$name.json"
    New-Item -ItemType Directory -Force (Split-Path $out) | Out-Null
    $report | ConvertTo-Json -Depth 5 | Set-Content $out
    Write-Host "Wrote $EvalReportDir/$name.json ($($result.aggregates.casesPassed)/$($result.aggregates.casesTotal) cases at or above $EvalThreshold)."
    if ($exit -ne 0) { $failed += $name }
}

if ($failed) {
    Write-Host "`nBelow the threshold or failed to run: $($failed -join ', '). CI rejects these reports until the cases pass." -ForegroundColor Yellow
    exit 1
}

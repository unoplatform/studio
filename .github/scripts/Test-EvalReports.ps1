#Requires -Version 7
<#
.SYNOPSIS
  Checks that every eval suite has a report in evals/reports/ that ran against the current files, covers every case
  with its full run count, and passes.

.DESCRIPTION
  Makes no model calls and needs no secrets, so it runs on every PR, forks included. A report goes stale whenever a
  file its suite depends on changes (see Get-EvalInputPaths); the fix is to rerun Update-EvalReport.ps1 and commit
  the new report. Every report is checked on every build, so one left stale on main fails later PRs until refreshed.
#>

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/EvalReports.Common.ps1"

$errors = [System.Collections.Generic.List[string]]::new()
$suites = @(Get-EvalSuites)

foreach ($suite in $suites) {
    $file = "$EvalReportDir/$suite.json"
    $rerun = "rerun it with: pwsh .github/scripts/Update-EvalReport.ps1 -Suite $suite, and commit $file"
    $path = Join-Path $EvalRoot $file
    if (-not (Test-Path $path)) { $errors.Add("$file is missing; $rerun"); continue }
    $report = Get-Content $path -Raw | ConvertFrom-Json

    if ($report.inputsHash -ne (Get-EvalInputsHash $suite)) {
        $errors.Add("$file is stale: the suite's cases, its skill or the plugin hooks changed after it ran; $rerun")
        continue
    }
    if ($report.ablation -ne $EvalAblation) {
        $errors.Add("$file ran with --ablation $($report.ablation), not $EvalAblation; $rerun")
    }

    $cases = @(Get-EvalCases $suite)
    $reported = @($report.cases | ForEach-Object dir)
    foreach ($dir in $cases | Where-Object { $_ -notin $reported }) { $errors.Add("$file has no result for $dir; $rerun") }
    foreach ($dir in $reported | Where-Object { $_ -notin $cases }) { $errors.Add("$file has a result for $dir, which is not a case") }

    foreach ($case in $report.cases | Where-Object dir -in $cases) {
        $runs = Get-EvalCaseRuns $case.dir
        if ($case.runs -lt $runs) {
            $errors.Add("$($case.dir) ran $($case.runs) time(s) in $file; its prompt.md asks for $runs. $rerun")
        } elseif ($case.score -lt $EvalThreshold) {
            $errors.Add("$($case.dir) scored $($case.score) in $file, below $EvalThreshold. Fix the skill or the case, then $rerun")
        }
    }
}

foreach ($report in Get-ChildItem (Join-Path $EvalRoot $EvalReportDir) -Filter *.json -ErrorAction SilentlyContinue) {
    if ($report.BaseName -notin $suites) { $errors.Add("$EvalReportDir/$($report.Name) has no suite under evals/; delete it") }
}

$prefix = $env:GITHUB_ACTIONS ? '::error::' : 'ERROR: '
$errors | ForEach-Object { Write-Host "$prefix$_" }
if ($errors.Count) { exit 1 }
Write-Host "Eval reports are up to date and passing ($($suites.Count) suites)."

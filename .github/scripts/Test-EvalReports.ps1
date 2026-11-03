#Requires -Version 7
<#
.SYNOPSIS
  Checks that every eval suite has a report in evals/reports/ that ran against the current files, covers every case
  with its full run count, and passes.

.DESCRIPTION
  Makes no model calls and needs no secrets, so it runs on every PR, forks included. A report goes stale whenever a
  file its suite depends on changes (see Get-EvalInputPaths) or the gate's models change (Get-EvalReportStaleReason);
  the fix is to rerun Update-EvalReport.ps1 and commit the new report. Every report is checked on every build, so one
  left stale on main fails later PRs until refreshed.
#>

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/EvalReports.Common.ps1"

$errors = [System.Collections.Generic.List[string]]::new()
$suites = @(Get-EvalSuites)
$toRerun = [System.Collections.Generic.List[string]]::new()

foreach ($suite in $suites) {
    $file = "$EvalReportDir/$suite.json"
    $rerun = "rerun it with: pwsh .github/scripts/Update-EvalReport.ps1 -Suite $suite, and commit $file"
    $reason = Get-EvalReportStaleReason $suite
    if ($reason) { $errors.Add("$file $reason; $rerun"); $toRerun.Add($suite); continue }
    $report = Get-Content (Join-Path $EvalRoot $file) -Raw | ConvertFrom-Json

    $cases = @(Get-EvalCases $suite)
    $reported = @($report.cases | ForEach-Object dir)
    foreach ($dir in $cases | Where-Object { $_ -notin $reported }) { $errors.Add("$file has no result for $dir; $rerun") }
    foreach ($dir in $reported | Where-Object { $_ -notin $cases }) { $errors.Add("$file has a result for $dir, which is not a case") }

    foreach ($case in $report.cases | Where-Object dir -in $cases) {
        $runs = Get-EvalCaseRuns $case.dir
        if ($case.runs -lt $runs) {
            $errors.Add("$($case.dir) ran $($case.runs) time(s) in $file; its prompt.md asks for $runs. $rerun")
        } elseif ($case.score -lt $EvalThreshold) {
            $why = $case.failed ? " (failed: $(@($case.failed.PSObject.Properties | ForEach-Object { "$($_.Name) $($_.Value)" }) -join ', '))" : ''
            $errors.Add("$($case.dir) scored $($case.score) in $file, below $EvalThreshold$why. Fix the skill or the case, then $rerun")
        }
    }
}

foreach ($report in Get-ChildItem (Join-Path $EvalRoot $EvalReportDir) -Filter *.json -ErrorAction SilentlyContinue) {
    if ($report.BaseName -notin $suites) { $errors.Add("$EvalReportDir/$($report.Name) has no suite under evals/; delete it") }
}

$prefix = $env:GITHUB_ACTIONS ? '::error::' : 'ERROR: '
$errors | ForEach-Object { Write-Host "$prefix$_" }
if ($toRerun.Count) {
    # One command for all of them, and what they cost the last time they ran (a missing report has no figure).
    $lastCost = ($toRerun | ForEach-Object { Join-Path $EvalRoot "$EvalReportDir/$_.json" } | Where-Object { Test-Path $_ } |
        ForEach-Object { (Get-Content $_ -Raw | ConvertFrom-Json).costUsd } | Measure-Object -Sum).Sum
    Write-Host ("`n$($toRerun.Count) of $($suites.Count) reports to rerun. ``pwsh .github/scripts/Update-EvalReport.ps1``, " +
        "with no -Suite, reruns all of them with your Claude credentials (their last runs cost `$$([math]::Round($lastCost, 2))). " +
        'Commit evals/reports/ afterwards. See evals/README.md.')
}
if ($errors.Count) { exit 1 }
Write-Host "Eval reports are up to date and passing ($($suites.Count) suites)."

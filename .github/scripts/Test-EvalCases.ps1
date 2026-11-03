#Requires -Version 7
<#
.SYNOPSIS
  Checks that the eval cases under evals/ are well formed, without running them.

.DESCRIPTION
  Makes no model calls. Each suite's folder name is safe to pass as an argument, each case is tagged with its suite
  (Invoke-Evals.ps1 selects a suite's cases by that tag, so an untagged case would never run), and each case has a
  non-empty prompt that no other case repeats. Each `llm` grader judges
  exactly one criterion: a judge asked whether several things all hold gives a different verdict from run to run when
  one of them is missed, so each criterion gets its own grader (and its own score). The checks are one `PASS if`, no
  list, and no `(and ` or `; ` in the PASS condition: a tripwire for the two phrasings a second requirement has hidden
  in before, not proof that a rubric judges one thing. See evals/README.md.
#>

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/Evals.Common.ps1"

$errors = [System.Collections.Generic.List[string]]::new()

# Splits a Markdown file into its YAML frontmatter and body.
function Read-Frontmatter([string] $path) {
    $text = (Get-Content $path -Raw) -replace "`r`n", "`n"
    $m = [regex]::Match($text, '(?s)\A---\n(.*?)\n---\n?(.*)\z')
    if (-not $m.Success) { return $null }
    [pscustomobject]@{ Front = $m.Groups[1].Value; Body = $m.Groups[2].Value.Trim() }
}

$prompts = @{}
foreach ($suite in Get-EvalSuites) {
    if ($suite -cnotmatch $EvalSuiteNamePattern) { $errors.Add("evals/${suite}: name suite folders with lowercase letters, digits and hyphens") }
    foreach ($case in Get-EvalCases $suite) {
        $prompt = Read-Frontmatter (Join-Path $EvalRoot "$case/prompt.md")
        if (-not $prompt) { $errors.Add("$case/prompt.md: no frontmatter"); continue }
        $tags = [regex]::Match($prompt.Front, '(?m)^tags:\s*\[([^\]]*)\]').Groups[1].Value -split ',' | ForEach-Object { $_.Trim().Trim('"', "'") }
        if ($suite -cnotin $tags) { $errors.Add("$case/prompt.md: its tags must include $suite, the tag Invoke-Evals.ps1 selects the suite's cases by") }
        if (-not $prompt.Body) { $errors.Add("$case/prompt.md: the prompt is empty") }
        else {
            $key = ($prompt.Body -replace '\s+', ' ').ToLowerInvariant()
            if ($prompts[$key]) { $errors.Add("$case/prompt.md: same prompt as $($prompts[$key])") } else { $prompts[$key] = $case }
        }

        $graders = @(Get-ChildItem (Join-Path $EvalRoot "$case/graders") -Filter *.md -ErrorAction SilentlyContinue)
        if (-not $graders) { $errors.Add("${case}: no graders"); continue }
        foreach ($file in $graders) {
            $relative = "$case/graders/$($file.Name)"
            $grader = Read-Frontmatter $file.FullName
            if (-not $grader) { $errors.Add("${relative}: no frontmatter"); continue }
            $isLlm = $grader.Front -match '(?m)^type:\s*llm\s*$'
            # The prefix shows at a glance which graders call the judge model (and cost money).
            if ($isLlm -ne $file.Name.StartsWith('rubric-')) {
                $errors.Add("${relative}: name llm graders rubric-<criterion>.md, and only llm graders")
            }
            if (-not $isLlm) { continue }
            if ([regex]::Matches($grader.Body, '\bPASS\b').Count -ne 1) {
                $errors.Add("${relative}: an llm grader states one criterion, with exactly one 'PASS if'")
            }
            if ($grader.Body -match '(?m)^\s*(?:[-*+]|\d+\.)\s') {
                $errors.Add("${relative}: a list in an llm grader is several criteria; give each its own rubric-<criterion>.md")
            }
            # From PASS up to the FAIL line, so a wrapped line counts too, without inline code: a quoted C# statement's
            # "; " is not a second requirement.
            $passText = [regex]::Match($grader.Body, '(?ms)^[^\n]*\bPASS\b.*?(?=^FAIL\b|\z)').Value -replace '`[^`\n]*`', ''
            if ($passText -match '(?i)\(and\s|;\s') {
                $errors.Add("${relative}: the PASS condition joins a second requirement with '(and ' or '; '; give it its own rubric-<criterion>.md, and put a clarification in the 'Facts the judge needs' line")
            }
        }
    }
}

$prefix = $env:GITHUB_ACTIONS ? '::error::' : 'ERROR: '
$errors | ForEach-Object { Write-Host "$prefix$_" }
if ($errors.Count) { exit 1 }
Write-Host "Eval cases are well formed ($($prompts.Count) cases)."

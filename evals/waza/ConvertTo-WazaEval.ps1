#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Generates Waza (https://github.com/microsoft/waza) eval suites from the claude plugin eval cases in evals/, so the
  same cases run through GitHub Copilot CLI.

.DESCRIPTION
  Writes evals/waza/out/ (gitignored): one suite per skill, plus a routing suite.

  - <skill>/eval.yaml: the cases' regex and rubric graders on the reply. Run it with `waza run --baseline` for the
    with/without-skills comparison.
  - routing/eval.yaml: the skill-loaded checks (`loads-skill.md`, and the routing cases' "no Uno skill" checks). Waza
    scores every grader, so a skill-loaded check in the baseline suite would count against the no-skills pass and
    inflate the lift. Run it without --baseline.

  claude plugin eval -> Waza:
    prompt.md body      -> inputs.prompt
    runs                -> config.trials_per_task (eval level, so the highest runs in the suite)
    type: regex         -> type: text, regex_match (or regex_not_match for match: not_contains)
    type: llm           -> type: prompt, with the judge told to call set_waza_grade_pass / set_waza_grade_fail
    type: tool_used     -> type: tool_constraint, expect_tools (max: 0 -> reject_tools) on the skill tool, with
                           the skill name pattern from input_match
    plugins             -> config.skill_directories: the plugin's skills/ folder, discovered, not injected
  Not carried over: max_turns and allowed_tools (Waza has no per-task equivalent for skill evals).

.PARAMETER Case
  <folder>/<case> paths under evals/ to convert. Defaults to every case.

.EXAMPLE
  ./evals/waza/ConvertTo-WazaEval.ps1 -Case uno-mvux/remove-item-from-list, routing/maui-question
#>
[CmdletBinding()]
param(
    [string[]] $Case
)

$ErrorActionPreference = 'Stop'

$evalsRoot = Split-Path $PSScriptRoot -Parent
$repoRoot = Split-Path $evalsRoot -Parent
$outRoot = Join-Path $PSScriptRoot 'out'
$skillsDir = Join-Path $repoRoot 'plugins/uno-platform-studio/skills'

# Frontmatter is flat `key: value`: single-quoted strings, inline arrays and bare scalars.
function Read-Frontmatter([string] $path) {
    $text = (Get-Content $path -Raw) -replace "`r`n", "`n"
    if ($text -notmatch '(?s)^---\n(.*?)\n---\n?(.*)$') { throw "${path}: no frontmatter" }
    $front, $body = $Matches[1], $Matches[2].Trim()
    $fields = @{}
    foreach ($line in $front -split "`n") {
        if ($line -notmatch '^(\w+):\s*(.*)$') { continue }
        $key, $raw = $Matches[1], $Matches[2].Trim()
        $fields[$key] = if ($raw -match "^'(.*)'$") { $Matches[1] -replace "''", "'" }
            elseif ($raw -match '^\[(.*)\]$') { @($Matches[1] -split ',' | ForEach-Object { $_.Trim().Trim('"') } | Where-Object { $_ }) }
            else { $raw }
    }
    [pscustomobject]@{ Fields = $fields; Body = $body }
}

function Format-Quoted([string] $value) { "'" + ($value -replace "'", "''") + "'" }

function Format-Block([string] $value, [int] $indent) {
    $pad = ' ' * $indent
    ($value -split "`n" | ForEach-Object { if ($_) { "$pad$_" } else { '' } }) -join "`n"
}

# input_match is a regex on the Skill tool's JSON input, '"skill"\s*:\s*"<name pattern>"'. Waza matches the skill
# argument itself, so keep <name pattern> and anchor it; without the closing quote it is a prefix (any uno- skill).
function ConvertTo-SkillPattern([string] $inputMatch) {
    if ($inputMatch -notmatch '^"skill"\\s\*:\\s\*"(.*?)("?)$') { throw "unsupported input_match: $inputMatch" }
    '^' + $Matches[1] + $(if ($Matches[2]) { '$' } else { '' })
}

function ConvertTo-Grader([string] $name, $grader) {
    $f = $grader.Fields
    switch ($f.type) {
        'regex' {
            $key = if ($f.match -eq 'not_contains') { 'regex_not_match' } else { 'regex_match' }
            @(
                "  - type: text"
                "    name: $name"
                "    config:"
                "      ${key}:"
                "        - $(Format-Quoted $f.pattern)"
            ) -join "`n"
        }
        'llm' {
            $judge = $grader.Body + "`n`nIf the reply meets the PASS criteria above, call set_waza_grade_pass. Otherwise call set_waza_grade_fail and say which criterion failed."
            @(
                "  - type: prompt"
                "    name: $name"
                "    config:"
                "      prompt: |"
                (Format-Block $judge 8)
            ) -join "`n"
        }
        'tool_used' {
            $key = if ($f.max -eq '0') { 'reject_tools' } else { 'expect_tools' }
            @(
                "  - type: tool_constraint"
                "    name: $name"
                "    config:"
                "      ${key}:"
                "        - tool: '^skill$'"
                "          skill_pattern: $(Format-Quoted (ConvertTo-SkillPattern $f.input_match))"
            ) -join "`n"
        }
        default { throw "unsupported grader type '$($f.type)' in $name" }
    }
}

function Write-Task([string] $dir, [string] $id, $prompt, [string[]] $graders) {
    $tags = @($prompt.Fields.tags)
    $task = @(
        "id: $id"
        "name: $id"
        "description: $(Format-Quoted $prompt.Fields.description)"
        "tags: [$($tags -join ', ')]"
        "inputs:"
        "  prompt: |"
        (Format-Block $prompt.Body 4)
        "graders:"
        ($graders -join "`n")
    ) -join "`n"
    New-Item -ItemType Directory -Force (Join-Path $dir 'tasks') | Out-Null
    Set-Content -Path (Join-Path $dir "tasks/$id.yaml") -Value $task
}

function Write-Suite([string] $dir, [string] $name, [string] $skill, [int] $trials, [string] $description) {
    $relSkills = [IO.Path]::GetRelativePath($dir, $skillsDir) -replace '\\', '/'
    $spec = @(
        "# Generated by evals/waza/ConvertTo-WazaEval.ps1 from evals/. Edit the source cases, not this file."
        "name: $name"
        "description: $(Format-Quoted $description)"
        "skill: $skill"
        "version: `"1.0`""
        "config:"
        "  executor: copilot-sdk"
        '  # waza models lists the Copilot models; override with --model and --judge-model.'
        "  model: claude-sonnet-4.6"
        "  judge_model: claude-sonnet-4.6"
        "  trials_per_task: $trials"
        "  timeout_seconds: 300"
        "  parallel: false"
        "  skill_directories:"
        "    - $relSkills"
        "  # Copilot discovers and loads the skills itself, as with the installed plugin."
        "  inject_skill_body: false"
        "metrics:"
        "  - name: task_completion"
        "    weight: 1"
        "    threshold: 0.8"
        "graders: []"
        "tasks:"
        "  - `"tasks/*.yaml`""
    ) -join "`n"
    New-Item -ItemType Directory -Force $dir | Out-Null
    Set-Content -Path (Join-Path $dir 'eval.yaml') -Value $spec
}

$cases = if ($Case) {
    # Split too, so a comma-separated list works from bash as well as PowerShell.
    $Case -split ',' | ForEach-Object { Get-Item (Join-Path $evalsRoot $_.Trim()) }
} else {
    Get-ChildItem $evalsRoot -Directory | Where-Object Name -ne 'waza' |
        ForEach-Object { Get-ChildItem $_ -Directory } | Where-Object { Test-Path (Join-Path $_ 'prompt.md') }
}

if (Test-Path $outRoot) { Remove-Item $outRoot -Recurse -Force }

$suites = @{}
$routingTrials = 1
foreach ($dir in $cases) {
    $folder = $dir.Parent.Name
    $id = $dir.Name
    $prompt = Read-Frontmatter (Join-Path $dir 'prompt.md')
    $content = @()
    $routing = @()
    foreach ($file in Get-ChildItem (Join-Path $dir 'graders') -Filter *.md | Sort-Object Name) {
        $grader = Read-Frontmatter $file.FullName
        $yaml = ConvertTo-Grader $file.BaseName $grader
        if ($grader.Fields.type -eq 'tool_used') { $routing += $yaml } else { $content += $yaml }
    }
    $runs = [int]($prompt.Fields.runs ?? 1)
    if ($content) {
        $suiteDir = Join-Path $outRoot $folder
        Write-Task $suiteDir $id $prompt $content
        $suites[$folder] = [Math]::Max($suites[$folder] ?? 1, $runs)
    }
    if ($routing) {
        Write-Task (Join-Path $outRoot 'routing') "$folder--$id" $prompt $routing
        $routingTrials = [Math]::Max($routingTrials, $runs)
    }
}

foreach ($folder in $suites.Keys) {
    Write-Suite (Join-Path $outRoot $folder) $folder $folder $suites[$folder] `
        "Reply quality for the $folder cases. Run with --baseline to compare with and without the skills."
}
if (Test-Path (Join-Path $outRoot 'routing/tasks')) {
    # Waza needs a target skill; uno-platform is the plugin's entry skill. inject_skill_body: false keeps it from
    # being preloaded, so each task's skill_invocation grader sees only what Copilot chose to load.
    Write-Suite (Join-Path $outRoot 'routing') 'routing' 'uno-platform' $routingTrials `
        'Which skills Copilot loads: the expected Uno skill for Uno prompts, none for MAUI and ASP.NET Core prompts.'
}

Get-ChildItem $outRoot -Recurse -File | ForEach-Object { [IO.Path]::GetRelativePath($repoRoot, $_.FullName) }

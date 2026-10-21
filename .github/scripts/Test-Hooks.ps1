#Requires -Version 7
<#
.SYNOPSIS
  Runs the plugin's PostToolUse hook against canned hook input and a stand-in DevServer CLI.
  Run it on macOS as well as Linux: the hook is POSIX sh, and BSD sed rejects GNU-only constructs
  (`\|` alternation, `N` on the last line) without an error, which empties the hook's output.
#>

$ErrorActionPreference = 'Stop'
$hook = (Resolve-Path "$PSScriptRoot/../../plugins/uno-platform-studio/hooks/hot-reload-edit.sh").Path
$errors = [System.Collections.Generic.List[string]]::new()
$tmp = Join-Path ([IO.Path]::GetTempPath()) "uno-hook-test-$PID"
New-Item -ItemType Directory "$tmp/App/Presentation" -Force | Out-Null
New-Item -ItemType File "$tmp/App/App.sln" -Force | Out-Null

# A stand-in for `uno-devserver ... app hot-reload-status`: prints $lines and exits with $code.
function New-Cli([string]$name, [string[]]$lines, [int]$code) {
    $path = "$tmp/$name.sh"
    $body = ($lines | ForEach-Object { "printf '%s\n' '$($_ -replace "'", "'\''")'" }) -join "`n"
    Set-Content $path "#!/bin/sh`n$body`nexit $code`n" -NoNewline
    chmod +x $path
    $path
}

function Invoke-Hook([string]$file, [string]$cli) {
    $env:UNO_DEVSERVER_CLI = $cli
    $json = @{ tool_name = 'Edit'; tool_input = @{ file_path = $file } } | ConvertTo-Json -Compress
    $json | sh $hook
    if ($LASTEXITCODE) { $errors.Add("hook exited with $LASTEXITCODE for $file") }
}

function Assert-Hook([string]$case, [string]$file, [string]$cli, [scriptblock]$check) {
    $out = (Invoke-Hook $file $cli) -join "`n"
    if (-not $out) { $errors.Add("${case}: no output"); return }
    try { $parsed = $out | ConvertFrom-Json } catch { $errors.Add("${case}: output is not JSON: $out"); return }
    if (-not (& $check $parsed)) { $errors.Add("${case}: unexpected output: $out") }
}

try {
    $cs = "$tmp/App/Presentation/MainModel.cs"
    $applied = New-Cli 'applied' @("edit to 'Presentation/MainModel.cs': applied in 1.2s") 0
    $failed = New-Cli 'failed' @("edit to 'Presentation/MainModel.cs': Failed in 0.8s", 'MainModel.cs(3,1): error CS1002: ; expected "x"') 1
    $rude = New-Cli 'rude' @("edit to 'Presentation/MainModel.cs': RudeEdit") 3
    $noApp = New-Cli 'no-app' @('no connected app') 0

    Assert-Hook 'no CLI' $cs "$tmp/missing" { $args[0].hookSpecificOutput.additionalContext -like 'An Uno app source file changed.*' }
    Assert-Hook 'XAML file' "$tmp/App/Presentation/MainPage.xaml" "$tmp/missing" { $args[0].hookSpecificOutput.additionalContext -like 'An Uno app*' }
    Assert-Hook 'applied' $cs $applied { $args[0].hookSpecificOutput.additionalContext -clike "Hot Reload edit to 'Presentation/MainModel.cs': applied in 1.2s The running app now runs this edit*" }
    Assert-Hook 'compile failure' $cs $failed { $args[0].decision -eq 'block' -and $args[0].reason -clike '*error CS1002: ; expected "x"' }
    Assert-Hook 'rude edit' $cs $rude { $args[0].hookSpecificOutput.additionalContext -clike "Hot Reload edit to 'Presentation/MainModel.cs': RudeEdit This is the one case*" }

    $out = Invoke-Hook "$tmp/App/README.md" $applied
    if ($out) { $errors.Add("other file types: expected no output, got $out") }
    $out = Invoke-Hook $cs $noApp
    if ($out) { $errors.Add("no app connected yet: expected no output, got $out") }
}
finally {
    Remove-Item $tmp -Recurse -Force
    Remove-Item Env:UNO_DEVSERVER_CLI -ErrorAction SilentlyContinue
}

$prefix = $env:GITHUB_ACTIONS ? '::error::' : 'ERROR: '
$errors | ForEach-Object { Write-Host "$prefix$_" }
if ($errors.Count) { exit 1 }
Write-Host 'Hook checks passed.'

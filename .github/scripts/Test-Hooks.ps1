#Requires -Version 7
<#
.SYNOPSIS
  Runs the plugin's PostToolUse hook against canned hook input and a stand-in DevServer CLI.
  Run it on macOS as well as Linux: the hook is POSIX sh, and BSD sed rejects GNU-only constructs
  (`\|` alternation, `N` on the last line) without an error, which empties the hook's output.
#>

$ErrorActionPreference = 'Stop'
$hook = (Resolve-Path "$PSScriptRoot/../../plugins/uno-platform-studio/hooks/hot-reload-edit.sh").Path
$guard = (Resolve-Path "$PSScriptRoot/../../plugins/uno-platform-studio/hooks/app-restart-guard.sh").Path
$errors = [System.Collections.Generic.List[string]]::new()
$tmp = Join-Path ([IO.Path]::GetTempPath()) "uno-hook-test-$PID"
New-Item -ItemType Directory "$tmp/App/Presentation" -Force | Out-Null
New-Item -ItemType File "$tmp/App/App.sln" -Force | Out-Null
# The hooks keep their per-session state under $TMPDIR; keep the test's out of the real one.
$env:TMPDIR = $tmp -replace '\\', '/'

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
    $json = @{ session_id = 'hook-test'; tool_name = 'Edit'; tool_input = @{ file_path = $file } } | ConvertTo-Json -Compress
    $json | sh $hook
    if ($LASTEXITCODE) { $errors.Add("hook exited with $LASTEXITCODE for $file") }
}

# The restart guard, as Claude Code runs it around a uno_execute_tool call: `pre` may deny, `post` clears the state.
function Invoke-Guard([string]$mode, [string]$toolName) {
    $json = @{ session_id = 'hook-test'; tool_name = 'mcp__uno-app__uno_execute_tool'; tool_input = @{ toolName = $toolName; arguments = @{} } } | ConvertTo-Json -Compress
    ($json | sh $guard $mode) -join "`n"
    if ($LASTEXITCODE) { $errors.Add("guard $mode exited with $LASTEXITCODE for $toolName") }
}

function Assert-Denied([string]$case, [string]$like) {
    $out = Invoke-Guard pre uno_app_restart
    if (-not $out) { $errors.Add("${case}: the restart was allowed"); return }
    try { $parsed = $out | ConvertFrom-Json } catch { $errors.Add("${case}: output is not JSON: $out"); return }
    $o = $parsed.hookSpecificOutput
    if ($o.hookEventName -ne 'PreToolUse' -or $o.permissionDecision -ne 'deny' -or $o.permissionDecisionReason -notlike $like) { $errors.Add("${case}: unexpected output: $out") }
}

function Assert-Allowed([string]$case) {
    $out = Invoke-Guard pre uno_app_restart
    if ($out) { $errors.Add("${case}: expected the restart allowed with no output, got $out") }
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

    # The restart guard.
    $pending = New-Cli 'pending' @("edit to 'Presentation/MainModel.cs': processing") 4
    Assert-Allowed 'no edit since the start'
    Invoke-Hook $cs $applied | Out-Null
    Assert-Denied 'applied' "Hot Reload applied every edit since the last start (last: edit to 'Presentation/MainModel.cs': applied in 1.2s)*uno_app_close then uno_app_start*"
    $out = Invoke-Guard pre uno_app_get_screenshot
    if ($out) { $errors.Add("other app tools: expected no output, got $out") }
    Invoke-Guard post uno_app_restart | Out-Null
    Assert-Allowed 'after a restart'
    Invoke-Hook $cs $applied | Out-Null
    Invoke-Hook $cs $rude | Out-Null
    Assert-Allowed 'after a rude edit'
    Invoke-Guard post uno_app_start | Out-Null
    Invoke-Hook $cs $applied | Out-Null
    Invoke-Hook "$tmp/App/App.csproj" $applied | Out-Null
    Assert-Allowed 'after a project file edit'
    Invoke-Guard post uno_app_start | Out-Null
    Invoke-Hook "$tmp/App/App.xaml.cs" $applied | Out-Null
    Assert-Allowed 'after an App.xaml.cs edit, applied or not'
    Invoke-Guard post uno_app_start | Out-Null
    Invoke-Hook $cs $pending | Out-Null
    Assert-Denied 'pending' 'The last edit is still on its way*'
    Invoke-Guard post uno_app_start | Out-Null
    Invoke-Hook $cs "$tmp/missing" | Out-Null
    Assert-Allowed 'without the CLI'
}
finally {
    Remove-Item $tmp -Recurse -Force
    Remove-Item Env:UNO_DEVSERVER_CLI -ErrorAction SilentlyContinue
    Remove-Item Env:TMPDIR -ErrorAction SilentlyContinue
}

$prefix = $env:GITHUB_ACTIONS ? '::error::' : 'ERROR: '
$errors | ForEach-Object { Write-Host "$prefix$_" }
if ($errors.Count) { exit 1 }
Write-Host 'Hook checks passed.'

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

    Assert-Hook 'no CLI' $cs "$tmp/missing" { $args[0].hookSpecificOutput.additionalContext -like 'An Uno app source file changed.*' }
    Assert-Hook 'XAML file' "$tmp/App/Presentation/MainPage.xaml" "$tmp/missing" { $args[0].hookSpecificOutput.additionalContext -like 'An Uno app*' }
    Assert-Hook 'applied' $cs $applied { $args[0].hookSpecificOutput.additionalContext -clike "Hot Reload edit to 'Presentation/MainModel.cs': applied in 1.2s The running app now runs this edit*" }
    Assert-Hook 'compile failure' $cs $failed { $args[0].decision -eq 'block' -and $args[0].reason -clike '*error CS1002: ; expected "x"' }
    Assert-Hook 'rude edit' $cs $rude { $args[0].hookSpecificOutput.additionalContext -clike "Hot Reload edit to 'Presentation/MainModel.cs': RudeEdit This is the one case*" }

    $out = Invoke-Hook "$tmp/App/README.md" $applied
    if ($out) { $errors.Add("other file types: expected no output, got $out") }

    # The gates. A state directory per test run, so cases never share /tmp with a real session.
    $gates = (Resolve-Path "$PSScriptRoot/../../plugins/uno-platform-studio/hooks").Path
    $env:UNO_HOOK_STATE_DIR = "$tmp/state"; New-Item -ItemType Directory "$tmp/state" -Force | Out-Null
    function Invoke-Gate([string]$script, [hashtable]$payload) { (($payload | ConvertTo-Json -Compress -Depth 6) | sh "$gates/$script") -join "`n" }
    function Assert-Denied([string]$case, [string]$out) {
        if (-not $out) { $errors.Add("${case}: expected a deny, got no output"); return }
        try { $p = $out | ConvertFrom-Json } catch { $errors.Add("${case}: output is not JSON: $out"); return }
        if ($p.hookSpecificOutput.permissionDecision -ne 'deny' -or -not $p.hookSpecificOutput.permissionDecisionReason) { $errors.Add("${case}: expected a deny with a reason: $out") }
    }
    function Assert-Allowed([string]$case, [string]$out) { if ($out) { $errors.Add("${case}: expected no output, got $out") } }
    function Edit-Recorded([string]$file, [string]$cli) { $env:UNO_DEVSERVER_CLI = $cli; $null = (@{ session_id = 's2'; tool_name = 'Edit'; tool_input = @{ file_path = $file } } | ConvertTo-Json -Compress) | sh $hook }

    $build = @{ session_id = 's1'; tool_name = 'Bash'; tool_input = @{ command = 'dotnet build app/App.csproj -f net10.0-desktop 2>&1 | tail -5' } }
    $env:UNO_DEVSERVER_CLI = $applied   # the build gate only asks whether the CLI exists
    Assert-Denied 'build gate: dotnet build with the CLI' (Invoke-Gate 'shell-build-gate.sh' $build)
    Assert-Allowed 'build gate: build-only marker' (Invoke-Gate 'shell-build-gate.sh' @{ session_id = 's1'; tool_name = 'PowerShell'; tool_input = @{ command = 'dotnet build app/App.csproj -f net10.0-ios # build-only' } })
    Assert-Allowed 'build gate: dotnet new' (Invoke-Gate 'shell-build-gate.sh' @{ session_id = 's1'; tool_name = 'Bash'; tool_input = @{ command = 'dotnet new unoapp -o app; dotnet test app' } })
    $env:UNO_DEVSERVER_CLI = "$tmp/missing"
    Assert-Allowed 'build gate: no CLI' (Invoke-Gate 'shell-build-gate.sh' $build)

    $restart = @{ session_id = 's2'; tool_name = 'mcp__uno-app__uno_execute_tool'; tool_input = @{ toolName = 'uno_app_restart'; arguments = @{} } }
    $find = @{ session_id = 's2'; tool_name = 'mcp__uno-app__uno_execute_tool'; tool_input = @{ toolName = 'uno_app_find_elements'; arguments = @{ automationId = 'x' } } }
    Assert-Allowed 'restart gate: no state' (Invoke-Gate 'app-restart-gate.sh' $restart)
    Edit-Recorded $cs $applied
    Assert-Denied 'restart gate: after an applied edit' (Invoke-Gate 'app-restart-gate.sh' $restart)
    Assert-Allowed 'restart gate: another tool' (Invoke-Gate 'app-restart-gate.sh' $find)
    Edit-Recorded $cs $rude
    Assert-Allowed 'restart gate: after a rude edit' (Invoke-Gate 'app-restart-gate.sh' $restart)
    $null = Invoke-Gate 'app-tool-record.sh' @{ session_id = 's2'; tool_name = 'mcp__uno-app__uno_execute_tool'; tool_input = @{ toolName = 'uno_app_restart' }; tool_response = @{ content = @(@{ type = 'text'; text = 'App started successfully. Process ID: 1' }); isError = $false } }
    Assert-Denied 'restart gate: after a restart' (Invoke-Gate 'app-restart-gate.sh' $restart)
    $null = Invoke-Gate 'app-tool-record.sh' @{ session_id = 's2'; tool_name = 'mcp__uno-app__uno_execute_tool'; tool_input = @{ toolName = 'uno_app_hot_reload_status'; arguments = @{ file = 'x.cs' } }; tool_response = @{ content = @(@{ type = 'text'; text = "edit to 'x.cs': RudeEdit" }) } }
    Assert-Allowed 'restart gate: status reported RudeEdit' (Invoke-Gate 'app-restart-gate.sh' $restart)
    Edit-Recorded "$tmp/App/App.xaml.cs" $applied
    Assert-Allowed 'restart gate: after an App.xaml.cs edit' (Invoke-Gate 'app-restart-gate.sh' $restart)

    # Close and start again is the restart's other spelling: a start that repeats the last launch is gated too.
    function Start-Call([string]$session, [hashtable]$arguments) { @{ session_id = $session; tool_name = 'mcp__uno-app__uno_execute_tool'; tool_input = @{ toolName = 'uno_app_start'; arguments = $arguments } } }
    $desktop = @{ projectPath = 'X:\app\App.csproj'; targetFramework = 'net10.0-desktop'; connectionTimeoutSeconds = 120 }
    $null = Invoke-Gate 'app-tool-record.sh' ((Start-Call 's3' $desktop) + @{ tool_response = @{ content = @(@{ type = 'text'; text = 'App started successfully. Process ID: 2' }); isError = $false } })
    Edit-Recorded $cs $applied   # session s2 in the helper; record for s3 as well
    $null = (@{ session_id = 's3'; tool_name = 'Edit'; tool_input = @{ file_path = $cs } } | ConvertTo-Json -Compress) | sh $hook
    Assert-Denied 'start gate: same launch after an applied edit' (Invoke-Gate 'app-restart-gate.sh' (Start-Call 's3' $desktop))
    Assert-Allowed 'start gate: other arguments' (Invoke-Gate 'app-restart-gate.sh' (Start-Call 's3' ($desktop + @{ args = @('--data=slow') })))
    Assert-Allowed 'start gate: other target' (Invoke-Gate 'app-restart-gate.sh' (Start-Call 's3' @{ projectPath = 'X:\app\App.csproj'; targetFramework = 'net10.0-android' }))
    $null = Invoke-Gate 'app-tool-record.sh' ((Start-Call 's3' ($desktop + @{ args = @('--data=slow') })) + @{ tool_response = @{ content = @(@{ type = 'text'; text = 'The app exited with code 1' }); isError = $true } })
    Assert-Allowed 'start gate: retry after a failed start' (Invoke-Gate 'app-restart-gate.sh' (Start-Call 's3' ($desktop + @{ args = @('--data=slow') })))
    Assert-Allowed 'start gate: no launch recorded' (Invoke-Gate 'app-restart-gate.sh' (Start-Call 's4' $desktop))
    # Alternating two argument sets must not pass: every launch of the session counts, not only the last.
    $slow = $desktop + @{ args = @('--data=slow') }
    $null = Invoke-Gate 'app-tool-record.sh' ((Start-Call 's3' $slow) + @{ tool_response = @{ content = @(@{ type = 'text'; text = 'App started successfully. Process ID: 3' }); isError = $false } })
    Assert-Denied 'start gate: back to an earlier launch' (Invoke-Gate 'app-restart-gate.sh' (Start-Call 's3' $desktop))
    Assert-Denied 'start gate: the latest launch again' (Invoke-Gate 'app-restart-gate.sh' (Start-Call 's3' $slow))
    # An edit the DevServer did not cover within the hook's wait is pending, not an unlock.
    $uncovered = New-Cli 'uncovered' @("edit to 'Presentation/MainModel.cs': no Hot Reload operation covered the file") 1
    Edit-Recorded $cs $uncovered
    Assert-Denied 'restart gate: after an uncovered edit' (Invoke-Gate 'app-restart-gate.sh' $restart)
}
finally {
    Remove-Item $tmp -Recurse -Force
    Remove-Item Env:UNO_DEVSERVER_CLI, Env:UNO_HOOK_STATE_DIR -ErrorAction SilentlyContinue
}

$prefix = $env:GITHUB_ACTIONS ? '::error::' : 'ERROR: '
$errors | ForEach-Object { Write-Host "$prefix$_" }
if ($errors.Count) { exit 1 }
Write-Host 'Hook checks passed.'

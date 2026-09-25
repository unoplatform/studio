#Requires -Version 7
<#
.SYNOPSIS
  Installs the plugin into throwaway Copilot CLI and Codex homes, the way those agents parse it.
  No credentials needed.

.DESCRIPTION
  1. This checkout's plugin must install in each agent with every skill listed. Skill frontmatter itself
     is checked by `claude plugin validate` and `agentskills validate`.
  2. Each agent's marketplace file from this checkout must install the plugin (from its pinned tag).
#>

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../..").Path
$plugin = Join-Path $root 'plugins/uno-platform-studio'
$expected = (Get-ChildItem "$plugin/skills" -Directory).Count
$work = Join-Path ([IO.Path]::GetTempPath()) "agent-install-$([guid]::NewGuid().ToString('n'))"
$errors = [System.Collections.Generic.List[string]]::new()
$savedHomes = @{ COPILOT_HOME = $env:COPILOT_HOME; CODEX_HOME = $env:CODEX_HOME }

function Invoke-Agent([string]$label) {
    # Splat so npm's .ps1 shims (codex on Windows) get separate arguments. Merge stderr so failures are reported.
    $command, $arguments = $args
    $out = & $command @arguments 2>&1 | Out-String
    if ($LASTEXITCODE) { $errors.Add("${label}: exit $LASTEXITCODE`n$out") }
    $out
}

function New-Home([string]$name) {
    (New-Item -ItemType Directory -Force (Join-Path $work $name)).FullName
}

try {
    # --- Copilot CLI (also reads .github/plugin/marketplace.json, like Copilot in VS Code)
    $env:COPILOT_HOME = New-Home 'copilot'
    $out = Invoke-Agent 'Copilot CLI: install from checkout' copilot plugin install $plugin
    if ($out -notmatch "Installed $expected skills") { $errors.Add("Copilot CLI: expected 'Installed $expected skills'`n$out") }

    $env:COPILOT_HOME = New-Home 'copilot-marketplace'
    Invoke-Agent 'Copilot CLI: add marketplace' copilot plugin marketplace add $root | Out-Null
    Invoke-Agent 'Copilot CLI: install from marketplace' copilot plugin install uno-platform-studio@uno-platform | Out-Null

    # --- Codex: a local source must sit inside the marketplace root, so stage one around a copy.
    $marketplace = New-Home 'codex-marketplace'
    New-Item -ItemType Directory -Force "$marketplace/plugins", "$marketplace/.agents/plugins" | Out-Null
    Copy-Item $plugin "$marketplace/plugins/" -Recurse
    $m = Get-Content "$root/.agents/plugins/marketplace.json" -Raw | ConvertFrom-Json
    $m.name = 'pr-check'
    ($m.plugins | Where-Object name -EQ 'uno-platform-studio').source = @{ source = 'local'; path = './plugins/uno-platform-studio' }
    $m | ConvertTo-Json -Depth 10 | Set-Content "$marketplace/.agents/plugins/marketplace.json"

    $env:CODEX_HOME = New-Home 'codex'
    Invoke-Agent 'Codex: add staged marketplace' codex plugin marketplace add $marketplace | Out-Null
    Invoke-Agent 'Codex: install from checkout' codex plugin add uno-platform-studio@pr-check | Out-Null
    # Codex lists every skill with only the first N characters of its description: N shrinks as the catalog
    # grows, within 2% of the model's context window (5,440 tokens for a 272k model). Report what survives.
    $prompt = codex debug prompt-input -c skills.max_context_tokens=5440 2>$null | Out-String | ConvertFrom-Json
    $lines = ($prompt.content.text -join "`n") -split "`n" | Where-Object { $_ -match '^- uno-platform-studio:' }
    if ($lines.Count -ne $expected) { $errors.Add("Codex: expected $expected skills in the model's skill list, found $($lines.Count)") }
    $visible = $lines | ForEach-Object { ($_ -replace ' \(file: .*$' -replace '^- [^:]+:[^:]+: ?').Length } | Sort-Object
    if ($visible) {
        $report = "Codex shows $($visible[0])-$($visible[-1]) characters of each skill description (median $($visible[[int]($visible.Count / 2)])) at a 5,440-token skills budget, with no other skills installed."
        Write-Host $report
        if ($env:GITHUB_STEP_SUMMARY) { Add-Content $env:GITHUB_STEP_SUMMARY $report }
    }

    $env:CODEX_HOME = New-Home 'codex-marketplace-home'
    Invoke-Agent 'Codex: add marketplace' codex plugin marketplace add $root | Out-Null
    Invoke-Agent 'Codex: install from marketplace' codex plugin add uno-platform-studio@uno-platform | Out-Null
}
finally {
    $savedHomes.GetEnumerator() | ForEach-Object { Set-Item "Env:$($_.Key)" $_.Value }
    Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
}

$prefix = $env:GITHUB_ACTIONS ? '::error::' : 'ERROR: '
$errors | ForEach-Object { Write-Host "$prefix$_" }
if ($errors.Count) { exit 1 }
Write-Host "Copilot CLI and Codex installed the plugin with all $expected skills."

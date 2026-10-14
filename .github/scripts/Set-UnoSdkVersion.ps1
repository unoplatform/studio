#Requires -Version 7
<#
.SYNOPSIS
  Moves each skill's metadata.uno-sdk from a -dev build to the newest stable Uno.Sdk of the same
  major.minor, so a release only claims SDKs users can install. Fails while that minor has no stable
  release. Stable values are left as they are, so a patch release doesn't change the SDK it targets.
#>

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../..").Path
$stable = (Invoke-RestMethod 'https://api.nuget.org/v3-flatcontainer/uno.sdk/index.json').versions | Where-Object { $_ -notmatch '-' }
$failed = $false

foreach ($file in Get-ChildItem "$root/plugins/uno-platform-studio/skills/*/SKILL.md") {
    $skill = $file.Directory.Name
    $text = [IO.File]::ReadAllText($file.FullName)
    $m = [regex]::Match($text, '(?m)^  uno-sdk: "?((\d+\.\d+)\.\d+-[^"\s]+)"?')
    if (-not $m.Success) { continue }
    $minor = $m.Groups[2].Value
    $target = $stable | Where-Object { $_ -like "$minor.*" } | Sort-Object { [version]$_ } | Select-Object -Last 1
    if (-not $target) {
        Write-Host "$($env:GITHUB_ACTIONS ? '::error::' : 'ERROR: ')${skill}: built against Uno.Sdk $($m.Groups[1].Value), and Uno.Sdk $minor has no stable release on NuGet yet"
        $failed = $true
        continue
    }
    $text = $text.Remove($m.Index, $m.Length).Insert($m.Index, "  uno-sdk: `"$target`"")
    # skills/ mirrors the plugin's skills (Test-Plugin.ps1 checks it).
    foreach ($path in $file.FullName, "$root/skills/$skill/SKILL.md") { [IO.File]::WriteAllText($path, $text) }
    Write-Host "${skill}: Uno.Sdk $($m.Groups[1].Value) -> $target"
    if ($env:GITHUB_STEP_SUMMARY) { "- ``$skill``: Uno.Sdk $($m.Groups[1].Value) → $target" | Add-Content $env:GITHUB_STEP_SUMMARY }
}

if ($failed) { exit 1 }

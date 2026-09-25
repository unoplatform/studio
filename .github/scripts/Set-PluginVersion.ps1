#Requires -Version 7
<#
.SYNOPSIS
  Stamps a release version into every plugin manifest and points each marketplace's source.ref at its tag.
#>
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../..").Path

foreach ($f in 'plugins/uno-platform-studio/.claude-plugin/plugin.json',
               'plugins/uno-platform-studio/.codex-plugin/plugin.json',
               '.claude-plugin/marketplace.json',
               '.github/plugin/marketplace.json',
               '.agents/plugins/marketplace.json') {
    $path = Join-Path $root $f
    # ponytail: every "version"/"ref" key in these files is one we own; Test-Plugin.ps1 verifies the result.
    $text = [IO.File]::ReadAllText($path)
    [IO.File]::WriteAllText($path, ($text -replace '("(?:version|ref)"\s*:\s*)"[^"]*"', "`$1`"$Version`""))
}

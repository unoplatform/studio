#Requires -Version 7
<#
.SYNOPSIS
  Repo checks for the uno-platform-studio plugin that `claude plugin validate` doesn't cover.
#>

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../..").Path
$plugin = 'plugins/uno-platform-studio'
$pluginManifest = "$plugin/.claude-plugin/plugin.json"
$errors = [System.Collections.Generic.List[string]]::new()

Push-Location $root
try {
    # --- skills/ must mirror plugins/uno-platform-studio/skills/ (except skills/README.md)
    function Get-Tree([string]$dir) {
        $map = @{}
        foreach ($f in Get-ChildItem $dir -Recurse -File) {
            $map[[IO.Path]::GetRelativePath((Resolve-Path $dir).Path, $f.FullName) -replace '\\', '/'] = (Get-FileHash $f.FullName).Hash
        }
        $map
    }
    $source = Get-Tree "$plugin/skills"
    $mirror = Get-Tree 'skills'
    $mirror.Remove('README.md')
    foreach ($path in @($source.Keys) + @($mirror.Keys) | Sort-Object -Unique) {
        if ($source[$path] -ne $mirror[$path]) {
            $errors.Add("skills/$path differs from $plugin/skills/$path (sync the mirror: copy $plugin/skills/* over skills/)")
        }
    }

    # --- One version across every manifest, and every marketplace pins the plugin to that release tag.
    # main is staging: agents only see what is at the pinned tag.
    $versions = [ordered]@{}
    foreach ($f in $pluginManifest, "$plugin/.codex-plugin/plugin.json") {
        $versions[$f] = (Get-Content $f -Raw | ConvertFrom-Json).version
    }
    foreach ($f in '.claude-plugin/marketplace.json', '.github/plugin/marketplace.json', '.agents/plugins/marketplace.json') {
        $m = Get-Content $f -Raw | ConvertFrom-Json
        $entry = $m.plugins | Where-Object name -EQ 'uno-platform-studio'
        if ($m.metadata) { $versions["$f (metadata.version)"] = $m.metadata.version }
        if ($entry.PSObject.Properties['version']) { $versions["$f (plugins[uno-platform-studio].version)"] = $entry.version }
        $versions["$f (plugins[uno-platform-studio].source.ref)"] = $entry.source.ref
        if ($entry.source.path -ne $plugin) { $errors.Add("${f}: plugin source.path must be '$plugin'") }
        # Claude Code expands an `owner/repo` url to SSH, which fails for users without a GitHub key.
        $expected = $entry.source.source -eq 'github' ? @{ key = 'repo'; value = 'unoplatform/studio' } : @{ key = 'url'; value = 'https://github.com/unoplatform/studio.git' }
        if ($entry.source.($expected.key) -ne $expected.value) { $errors.Add("${f}: plugin source.$($expected.key) must be '$($expected.value)'") }
    }
    $version = $versions[$pluginManifest]
    foreach ($entry in $versions.GetEnumerator()) {
        if ($entry.Value -ne $version) {
            $errors.Add("$($entry.Key) is '$($entry.Value)', expected '$version' (from $pluginManifest)")
        }
    }

    # --- Links between skills and to references/ (frontmatter is checked by `agentskills validate`)
    foreach ($dir in Get-ChildItem "$plugin/skills" -Directory) {
        $skill = "$plugin/skills/$($dir.Name)/SKILL.md"
        if (-not (Test-Path $skill)) { $errors.Add("$($dir.Name): missing SKILL.md"); continue }
        $text = Get-Content $skill -Raw

        foreach ($ref in [regex]::Matches($text, '`(uno-(?:mvux|navigation|toolkit|themes|testing)-[a-z0-9-]+)`') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique) {
            if (-not (Test-Path "$plugin/skills/$ref")) { $errors.Add("${skill}: references unknown skill '$ref'") }
        }
        foreach ($ref in [regex]::Matches($text, 'references/[A-Za-z0-9._-]+\.md') | ForEach-Object Value | Sort-Object -Unique) {
            if (-not (Test-Path "$plugin/skills/$($dir.Name)/$ref")) { $errors.Add("${skill}: missing file '$ref'") }
        }
    }
}
finally {
    Pop-Location
}

$prefix = $env:GITHUB_ACTIONS ? '::error::' : 'ERROR: '
$errors | ForEach-Object { Write-Host "$prefix$_" }
if ($errors.Count) { exit 1 }
Write-Host "Plugin checks passed (version $version)."

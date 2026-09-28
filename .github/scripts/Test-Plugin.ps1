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

    # --- Links between skills and to references/ (frontmatter is checked by `agentskills validate`).
    # Each skill is a hub: SKILL.md must route to every file in its references/, and a
    # `references/<topic>.md` mention resolves in the hub named earlier on the same line
    # (for example "the `uno-toolkit` skill (`references/card.md`)"), otherwise in the current skill.
    # Mentions inside fenced code blocks, HTML comments, and the YAML frontmatter do not count.
    function Get-ProseLines([string]$file) {
        $inFence = $false; $inComment = $false; $inFrontmatter = $false; $first = $true
        foreach ($line in Get-Content $file) {
            if ($first) { $first = $false; if ($line -eq '---') { $inFrontmatter = $true; continue } }
            if ($inFrontmatter) { if ($line -eq '---') { $inFrontmatter = $false }; continue }
            if ($line -match '^\s*(```|~~~)') { $inFence = -not $inFence; continue }
            if ($inFence) { continue }
            if ($inComment) { if ($line -match '-->') { $inComment = $false }; continue }
            if ($line -match '<!--' -and $line -notmatch '-->') { $inComment = $true; continue }
            $line -replace '<!--.*?-->', ''
        }
    }
    $hubs = (Get-ChildItem "$plugin/skills" -Directory).Name
    foreach ($dir in Get-ChildItem "$plugin/skills" -Directory) {
        $skill = "$plugin/skills/$($dir.Name)/SKILL.md"
        if (-not (Test-Path $skill)) { $errors.Add("$($dir.Name): missing SKILL.md"); continue }
        $files = @($skill) + @(Get-ChildItem "$plugin/skills/$($dir.Name)/references" -Filter *.md -ErrorAction SilentlyContinue | ForEach-Object FullName)
        $onDisk = @(Get-ChildItem "$plugin/skills/$($dir.Name)/references" -Filter *.md -ErrorAction SilentlyContinue | ForEach-Object Name)
        $routed = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach ($file in $files) {
            $isHub = $file -eq $skill
            foreach ($line in Get-ProseLines $file) {
                foreach ($m in [regex]::Matches($line, 'uno-(?:mvux|navigation|toolkit|themes|testing)-[a-z0-9-]+')) {
                    $errors.Add("${file}: references retired per-topic skill '$($m.Value)' (it is now a references/ file inside a hub)")
                }
                foreach ($m in [regex]::Matches($line, 'references/([A-Za-z0-9._-]+\.md)')) {
                    $before = $line.Substring(0, $m.Index)
                    $named = [regex]::Matches($before, 'uno-[a-z]+') | ForEach-Object Value | Where-Object { $_ -in $hubs } | Select-Object -Last 1
                    $hub = $named ? $named : $dir.Name
                    $name = $m.Groups[1].Value
                    # Case-sensitive existence check: CI runs on Linux.
                    $exists = @(Get-ChildItem "$plugin/skills/$hub/references" -Filter *.md -ErrorAction SilentlyContinue | Where-Object { $_.Name -ceq $name }).Count -gt 0
                    if (-not $exists) { $errors.Add("${file}: '$($m.Value)' does not exist in skill '$hub'") }
                    elseif ($isHub -and $hub -eq $dir.Name) { $routed.Add($name) | Out-Null }
                }
            }
        }
        foreach ($name in $onDisk) {
            if (-not $routed.Contains($name)) { $errors.Add("${skill}: does not route to 'references/$name'") }
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

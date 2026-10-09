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
        # The `github` owner/repo form is the Copilot marketplace's (.github/plugin/marketplace.json); the `git-subdir` URL form is Claude Code's and Codex's.
        # Claude Code clones over SSH only when the user's key authenticates to github.com, and over HTTPS otherwise (or always, with CLAUDE_CODE_PLUGIN_PREFER_HTTPS=1).
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
        $fence = $null; $inComment = $false; $inFrontmatter = $false; $first = $true
        foreach ($line in Get-Content $file) {
            if ($first) { $first = $false; if ($line -eq '---') { $inFrontmatter = $true; continue } }
            if ($inFrontmatter) { if ($line -eq '---') { $inFrontmatter = $false }; continue }
            if ($inComment) { if ($line -match '-->') { $inComment = $false; $line -replace '^.*?-->', '' }; continue }
            # A fence closes only on the same character with at least the opener's length (CommonMark).
            if ($line -match '^\s*(`{3,}|~{3,})') {
                if (-not $fence) { $fence = $Matches[1]; continue }
                if ($Matches[1][0] -eq $fence[0] -and $Matches[1].Length -ge $fence.Length) { $fence = $null; continue }
            }
            if ($fence) { continue }
            $line = $line -replace '<!--.*?-->', ''
            if ($line -match '<!--') { $inComment = $true; $line -replace '<!--.*$', ''; continue }
            $line
        }
    }

    # --- Workflow templates and relative Markdown links (including assets/ and ../ links).
    $pluginFiles = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    Get-ChildItem $plugin -Recurse -File | ForEach-Object { $pluginFiles.Add($_.FullName) | Out-Null }
    foreach ($file in Get-ChildItem $plugin -Recurse -File -Filter *.md) {
        foreach ($line in Get-ProseLines $file.FullName) {
            foreach ($m in [regex]::Matches($line, '\[[^\]]*\]\(([^\s)]+)\)')) {
                $target = ($m.Groups[1].Value -split '#', 2)[0]
                if (-not $target -or $target -match '^[a-zA-Z][a-zA-Z0-9+.-]*:') { continue }
                $resolved = [IO.Path]::GetFullPath((Join-Path $file.DirectoryName $target))
                if (-not $pluginFiles.Contains($resolved)) {
                    $errors.Add("$($file.FullName): relative link '$target' does not resolve to a plugin file with matching case")
                }
            }
        }
    }

    # --- The manually installed Codex agent must carry the same instructions as the plugin agent.
    $agentMarkdown = (Get-Content "$plugin/agents/uno-dev.agent.md" -Raw) -replace "`r`n", "`n"
    $agentToml = (Get-Content "$plugin/codex/uno-dev.toml" -Raw) -replace "`r`n", "`n"
    $markdownBody = [regex]::Match($agentMarkdown, '(?s)\A---\n.*?\n---\n(.*)\z')
    $tomlBody = [regex]::Match($agentToml, '(?s)developer_instructions\s*=\s*"""\n(.*?)\n"""')
    if (-not $markdownBody.Success -or -not $tomlBody.Success -or $markdownBody.Groups[1].Value.Trim() -cne $tomlBody.Groups[1].Value.Trim()) {
        $errors.Add('uno-dev: Markdown and Codex agent instructions differ or cannot be read')
    }
    foreach ($field in 'name', 'description') {
        $markdownValue = [regex]::Match($agentMarkdown, "(?m)^${field}: *([^\n]+)").Groups[1].Value.Trim().Trim('"')
        $tomlValue = [regex]::Match($agentToml, "(?m)^${field} *= *([^\n]+)").Groups[1].Value.Trim().Trim('"')
        if (-not $markdownValue -or $markdownValue -cne $tomlValue) { $errors.Add("uno-dev: Markdown and Codex agent $field differ or are missing") }
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
            $relative = [IO.Path]::GetRelativePath($root, $file) -replace '\\', '/'
            foreach ($line in Get-ProseLines $file) {
                # Retired skill names are only flagged when written as code (`uno-toolkit-card`), so prose like "uno-toolkit-based" passes.
                foreach ($m in [regex]::Matches($line, '`uno-(?:mvux|navigation|toolkit|themes|testing)-[a-z0-9-]+`')) {
                    $errors.Add("${relative}: references retired per-topic skill '$($m.Value)' (it is now a references/ file inside a hub)")
                }
                foreach ($m in [regex]::Matches($line, 'references/([A-Za-z0-9._-]+\.md)')) {
                    $before = $line.Substring(0, $m.Index)
                    $named = [regex]::Matches($before, '(?<![a-z-])uno-[a-z]+(?![a-z-])') | ForEach-Object Value | Where-Object { $_ -in $hubs } | Select-Object -Last 1
                    $hub = $named ? $named : $dir.Name
                    $name = $m.Groups[1].Value
                    # Case-sensitive existence check: CI runs on Linux.
                    $exists = @(Get-ChildItem "$plugin/skills/$hub/references" -Filter *.md -ErrorAction SilentlyContinue | Where-Object { $_.Name -ceq $name }).Count -gt 0
                    if (-not $exists) { $errors.Add("${relative}: '$($m.Value)' does not exist in skill '$hub'") }
                    elseif ($isHub -and $hub -eq $dir.Name) { $routed.Add($name) | Out-Null }
                }
            }
        }
        foreach ($name in $onDisk) {
            if (-not $routed.Contains($name)) { $errors.Add("${skill}: does not route to 'references/$name'") }
        }
    }

    # --- Size report. Every file an agent reads stays in its context and is re-read on each later
    # call, so references get a budget. Larger files are split by task; templates and lookup tables
    # that should not be split are exempt but need a table of contents (an `](#` link near the top).
    # ponytail: 4 chars/token is a fixed estimate; recalibrate against the real tokenizer if it drifts.
    $charsPerToken = 4
    $referenceBudget = 6000
    $exempt = @{
        'uno-navigation/references/shell-navigationview-template.md' = 'complete template'
        'uno-navigation/references/shell-responsive-template.md'     = 'complete template'
        'uno-navigation/references/shell-tabbar-template.md'         = 'complete template'
        'uno-themes/references/semantic-colors-brushes.md'           = 'lookup table'
    }
    $budget = [System.Collections.Generic.List[string]]::new()
    $sizes = foreach ($f in Get-ChildItem "$plugin/skills" -Recurse -File) {
        $path = [IO.Path]::GetRelativePath("$root/$plugin/skills", $f.FullName) -replace '\\', '/'
        # Count LF line endings so a Windows (CRLF) checkout reports what CI reports.
        $chars = ((Get-Content $f.FullName -Raw) -replace "`r`n", "`n").Length
        $note = ''
        if ($f.Directory.Name -eq 'references' -and $chars -gt $referenceBudget) {
            if ($exempt[$path]) {
                $note = "exempt: $($exempt[$path])"
                if (-not ((Get-Content $f.FullName -TotalCount 30) -match '\]\(#')) { $budget.Add("${path}: exempt from the size budget ($($exempt[$path])) but has no table of contents in its first 30 lines") }
            }
            else {
                $note = 'over budget'
                $budget.Add("${path}: $chars characters, over the $referenceBudget-character reference budget (split it by task)")
            }
        }
        [pscustomobject]@{ File = $path; Characters = $chars; Tokens = [int]($chars / $charsPerToken); Note = $note }
    }
    $sizes = $sizes | Sort-Object Characters -Descending
    $total = ($sizes | Measure-Object Characters -Sum).Sum
    $sizes | Format-Table -AutoSize | Out-String | Write-Host
    Write-Host "Total: $total characters, ~$([int]($total / $charsPerToken)) tokens at $charsPerToken characters per token."
    if ($env:GITHUB_STEP_SUMMARY) {
        @(
            '## Skill file sizes', '',
            "Estimated tokens = characters / $charsPerToken. Reference budget: $referenceBudget characters.", '',
            '| File | Characters | ~Tokens | Note |', '|---|--:|--:|---|'
            $sizes | ForEach-Object { "| ``$($_.File)`` | $($_.Characters) | $($_.Tokens) | $($_.Note) |" }
            "| **Total** | $total | $([int]($total / $charsPerToken)) | |"
        ) | Add-Content $env:GITHUB_STEP_SUMMARY
    }
}
finally {
    Pop-Location
}

$prefix = $env:GITHUB_ACTIONS ? '::error::' : 'ERROR: '
$errors | ForEach-Object { Write-Host "$prefix$_" }
# Warnings until the oversized references are split (#161); then these move to $errors.
$budget | ForEach-Object { Write-Host "$($env:GITHUB_ACTIONS ? '::warning::' : 'WARNING: ')$_" }
if ($errors.Count) { exit 1 }
Write-Host "Plugin checks passed (version $version)."

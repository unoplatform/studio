# Brief: Uno Linter

## Overview
A deterministic checker for Uno Platform XAML and C# that verifies what an AI agent actually wrote. Skills tell the agent what to do; the linter checks that it did it right after each edit, and names the platform control or resource to use instead.

## Strategic Value
- **Closes the verification gap**: skills only advise and fade as context fills; the linter catches the shortcut at the first edit, before it spreads across the app
- **Deterministic and measurable**: same code, same result; counts per rule track how well agents, skills and templates are doing across apps
- **Reuses .NET conventions**: `.editorconfig` severities, Roslyn-style ids, recommended/strict tiers like flutter_lints
- **One rule list, three surfaces**: plugin hook now, MCP tool next, Roslyn analyzer later; the rules live in a netstandard2.0 library (C#) and as Uno.XamlParser fixers (XAML), and every surface renders the same report

## Core Capabilities (5 User Stories)

### **Priority 1 (Critical)**
- **Same-turn correction**: hook after every XAML or C# edit (Claude Code, Codex) returns findings for the added lines only, bugs first; never blocks
- **Team configuration**: `recommended` vs `strict` profiles, per-rule severity in `.editorconfig`, Toolkit rules gated on a Toolkit reference, reasoned suppression comments

### **Priority 2 (High Value)**
- **Project audit and CI**: path mode with per-rule counts, top files, JSON output, exit codes 0/1/2
- **MCP tool**: `lint_project` / `lint_file` on the Uno dev server with the CLI's JSON shape

### **Priority 3 (Enhancement)**
- **Roslyn analyzer**: same rules and ids as build warnings in VS, Rider and `dotnet build`

## Key Findings So Far
- The C# tool reproduces the PowerShell prototype's counts except for the false positives it removes, each with a regression test
- Hook time and full-scan time are under budget; the single measurement, with its machine and method, is in the spec's Performance section
- An agent-built sample with SimpleTheme, Toolkit and MVUX enabled still hard-coded its whole palette on every page
- The CODEBEHIND rule flags well over a hundred handlers on Uno's own Toolkit gallery, which is why the architectural rules are strict-only

## Decisions Needed
- Confirm the recommended/strict split
- Plugin hook first or MCP tool first
- Owner of the rule list

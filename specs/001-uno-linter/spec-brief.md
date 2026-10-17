# Brief: Uno Linter

## Overview
A deterministic checker for Uno Platform XAML and C# that verifies what an AI agent actually wrote. Skills tell the agent what to do; the linter checks that it did it, in about a tenth of a second after each edit, and names the platform control or resource to use instead.

## Strategic Value
- **Closes the verification gap**: skills only advise and fade as context fills; the linter catches the shortcut at the first edit, before it spreads across the app
- **Deterministic and measurable**: same code, same result; counts per rule track how well agents, skills and templates are doing across apps
- **Reuses .NET conventions**: `.editorconfig` severities, Roslyn-style ids, recommended/strict tiers like flutter_lints
- **One rule list, three surfaces**: plugin hook now, MCP tool next, Roslyn analyzer later, all from one netstandard2.0 library

## Core Capabilities (5 User Stories)

### **Priority 1 (Critical)**
- **Same-turn correction**: hook after every XAML or C# edit returns findings for the added lines only; never blocks
- **Team configuration**: `recommended` vs `strict` profiles, per-rule severity in `.editorconfig`, Toolkit rules gated on a Toolkit reference, reasoned suppression comments

### **Priority 2 (High Value)**
- **Project audit and CI**: path mode with per-rule counts, top files, JSON output, exit codes 0/1/2
- **MCP tool**: `lint_project` / `lint_file` on the Uno dev server with the CLI's JSON shape

### **Priority 3 (Enhancement)**
- **Roslyn analyzer**: same rules and ids as build warnings in VS, Rider and `dotnet build`

## Key Findings So Far
- C# port reproduces the PowerShell prototype's counts exactly, minus five classes of false positive, each with a regression test
- 0.48 s full scan of a 175-file app; about 130 ms per hook call
- An agent-built sample with SimpleTheme, Toolkit and MVUX enabled still contained 93 hard-coded colors
- The CODEBEHIND rule alone flags 129 handlers on Uno's own Toolkit sample, which is why the architectural rules are strict-only

## Decisions Needed
- Confirm the recommended/strict split
- Plugin hook first or MCP tool first
- Owner of the rule list

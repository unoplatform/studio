# Spike: Uno.XamlParser as the XAML host

Date: 2026-10-14. Branch `dev/vs/uno-lint-xamlparser-spike`, stacked on `dev/vs/uno-linter`.

**Result: it works, it matches the built-in host exactly on valid XAML, and it lints the broken markup the built-in
host silently skips. The cost is hook latency, which a ReadyToRun build brings back under budget.** Recommendation:
adopt it as the XAML host, conditional on the two upstream asks and the packaging change below.

## What was built

- `src/Uno.Lint.Xaml`, a net10.0 project on the `Uno.XamlParser` 0.10.0-dev.74 NuGet package. `XamlParserEngine`
  runs Stage 0 with repairs on (so broken markup still yields a model) and Stage 1 with fixers off (so values are
  read as written). Stage 2/3 need a type resolver, which a CLI or hook does not have, so they are not wired.
- HEX and CARD ported onto the Stage-1 node model (`ParserRules.cs`), with the same messages and suppression.
- `IXamlEngine` in the netstandard2.0 core: an engine owns some rules, the built-in XDocument path runs the rest.
  The core keeps no dependency on the parser.
- `RuleLogic`: the color, key and geometry predicates moved out of `XamlAnalyzer` so both hosts call the same code.
  A rule means the same thing whichever host runs it.
- `--xaml-engine xamlparser` on the CLI and the hook; off by default. Reports repaired, partial and line-shifted files.
- 14 new tests: 6 parity cases, 6 broken-markup cases, the partial-read case, and a host-integration test.

## Measurement 1: parity on valid XAML

Both hosts on the five-app corpus, strict profile, compared finding by finding (file, line, message, suppressed).

| App | HEX | CARD | Differences |
|---|---|---|---|
| uno.toolkit.ui samples (179 files) | 293 | 4 | 0 |
| Uno.Themes sample (14) | 0 | 0 | 0 |
| BrewHouse (28) | 116 | 11 | 0 |
| GridWatch (28) | 49 | 16 | 0 |
| TestApp (6) | 0 | 0 | 0 |

489 findings, identical. TOKENTHEME and ICON, still on the built-in host, are unchanged too: the HEX/TOKENTHEME
hand-off works across hosts.

## Measurement 2: broken markup

Eight files with the mistakes agents make, each with a hex-colored card Border on a known line.

| Fixture | Built-in host | Uno.XamlParser |
|---|---|---|
| Valid | linted | linted |
| Unclosed element | skipped silently | repaired, HEX + CARD on the right line |
| Close tag with wrong casing (`</border>`) | skipped | repaired, correct |
| Unquoted attribute (`CornerRadius=12`) | skipped | repaired, correct |
| Stray ampersand | skipped | repaired, correct |
| Truncated output (missing closing tags) | skipped | repaired, correct |
| `--` inside a comment | skipped | repaired, correct |
| Early wrong close tag (`</Grid>` while a StackPanel is open) | skipped | partial: repair orphans a later close tag and the reader stops; the prefix is linted, findings correct |

No repair shifted line numbers. This is the main win: today a file the agent half-broke is exactly the file the hook
says nothing about.

## Measurement 3: latency

Same machine, median of 9 (hook) or 5 (scan) runs.

| Run | Built-in | Uno.XamlParser |
|---|---|---|
| Hook, one edit (CartPage), JIT | 159 ms | 273 ms |
| Hook, one edit, ReadyToRun | 150 ms | 194 ms |
| Full scan, Toolkit samples, JIT | 559 ms | 842 ms |
| Full scan, Toolkit samples, ReadyToRun | 340 ms | 518 ms |
| Full scan, BrewHouse, JIT | 367 ms | 479 ms |

The extra cost is per process: loading and JIT-compiling the parser pipeline. JIT puts the hook about 25 ms over the
250 ms budget (SC-001); ReadyToRun brings it to 194 ms. .NET 10 supports RID-specific tool packages, so the tool can
ship precompiled.

## Findings about Uno.XamlParser

1. **`IXamlSemanticVisitor` is declared but nothing calls it.** There is no plug-in point for observe-only visitors
   today. The spike reads the collector's `ElementStarts` and `TextNodes` after `ProcessAsync` instead. Ask: wire the
   visitors into the pipeline, or make the collector snapshot (`IXamlSemanticDocument`) the supported read API.
2. **The repaired text is internal.** It sits on `IEditableStringXamlStreamSource`; the spike reads it by reflection
   to check for line shifts. Ask: expose the repaired text, or a line map, on the result.
3. **One repair gap.** Balancing an early wrong close tag orphans the real close tag further down, so the reader
   stops (XML0001). Reproduced in `Lints_the_prefix_when_repair_cannot_finish`.
4. **Not thread-safe.** The semantic collector is a singleton holding per-run state. The spike serializes calls. The
   parser's own CLI shares one pipeline across parallel tasks, which looks unsafe when parallelism is above 1.
5. **The repository does not build from a fresh clone.** NU1902 on `Microsoft.Build.Tasks.Git` 10.0.201 is a
   warning promoted to an error. The NuGet package itself restores and works.
6. **Stage 0 diagnostics appear in both the stream result and the semantic result**, so a naive consumer counts them
   twice.
7. Package facts: net9.0 only, prerelease (0.10.0-dev.74), last commit May 2026.

## What it does not change

`.editorconfig`, suppression comments, project context, exit codes, hook attribution and all C# rules stay in the
uno-lint host, as Steve's review said. The XAML rules leave the netstandard2.0 analyzer goal if they move to the
parser; the C# rules keep it.

## Recommendation and next steps

Adopt Uno.XamlParser as the XAML host.

1. Ship the tool ReadyToRun as RID-specific packages so the hook stays under 250 ms.
2. Raise asks 1 and 2 with Carl; 3 to 6 are issues to file. Until 1 lands, the collector snapshot is the read API.
3. Port TOKENTHEME, ICON, BACKBAR and BINDVALUE with the same parity harness, then make the parser the default and
   delete the XDocument rules. BINDVALUE keeps its regex: the parser keeps markup as text.
4. Decide who owns the package's stability while the hook depends on it.
5. Record in the spec that XAML rules leave the analyzer goal.

## Run it

```bash
dotnet run --project src/Uno.Lint.Cli -c Release -- <app> --profile strict --xaml-engine xamlparser
dotnet test --filter XamlParserEngineTests
```

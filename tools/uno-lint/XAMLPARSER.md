# XAML rules on Uno.XamlParser

Date: 2026-10-15. Branch `dev/vs/uno-lint-xamlparser-spike`, stacked on `dev/vs/uno-linter`.

All six XAML rules run inside [Uno.XamlParser](https://github.com/unoplatform/uno.xamlparser) as Stage-1 semantic
fixers that only report. uno-lint no longer has its own XAML reader. It keeps what XamlParser does not do: the C#
rules, `.editorconfig` severities and profiles, project gates, suppression comments and counts, the hook and the
output. Findings on valid XAML are identical to the old reader's, apart from one false positive fixed along the way.
Malformed XAML that the old reader skipped is now linted.

## How it fits together

```
agent edit ─► uno-lint --hook / CLI ─► Linter (Core)
                                         ├─ .cs   ─► CSharpAnalyzer (Roslyn)            ─┐
                                         └─ .xaml ─► XamlParserEngine                     │
                                                       └─ Uno.XamlParser pipeline         ├─► findings ─► severity, suppression,
                                                            Stage 0  repair (only if malformed)            profile, gates, output
                                                            Stage 1  ElementTextCapture + DocumentTracker
                                                                     + HexRule, TokenThemeRule, IconRule,
                                                                       CardRule, BackBarRule, BindValueRule  ─┘
                                                                     report UNOL001...UNOL201 diagnostics
```

- **Extension points used.** `XamlStreamPipelineOptions.SemanticFixerTypes` for the rules (`IXamlSemanticFixer`,
  reporting through `XamlSemanticFixerContext.ReportDiagnostic`), and `VisitorTypes` for one stream visitor that
  captures element text. Both are supported registration lists; nothing in XamlParser is patched or reflected into,
  except one read noted below.
- **Reusable elsewhere.** `UnoLintXamlRules.AddTo(options)` registers the rules on any XamlParser pipeline. Studio Live
  and Uno.AI, which already run XamlParser after agent edits, can enable them with that one call; the diagnostics come
  back in `SemanticResult.Diagnostics` with codes `UNOL001` to `UNOL201`. A test does exactly this on a stock pipeline.
- **Streaming rule respected.** Per-document state lives on `XamlSemanticFixerContext.Items`, and only the path of open
  elements is kept, as the repository's `CLAUDE.md` requires. Rules that need children or descendants (CARD, ICON's
  Viewbox, TOKENTHEME) decide at the end tag. BACKBAR decides when the root closes.
- **Values as written.** The engine's policy applies Stage-0 repairs only, and XamlParser's own fixers are not
  registered, so rules see the markup the agent wrote.

## Measurements

Corpus: the five apps from FINDINGS.md, strict profile, every finding compared by rule, severity, file, line, message
and suppression against the old XDocument reader.

| App | Before | After | Difference |
|---|---|---|---|
| uno.toolkit.ui samples (179 files) | 451 | 445 | 6 false positives removed (`x:Key="Red"` read as a color) |
| Uno.Themes sample | 5 | 5 | none |
| BrewHouse | 127 | 127 | none |
| GridWatch | 81 | 81 | none |
| TestApp | 1 | 1 | none |

Info counters (brush references, brush keys, AdaptiveTriggers, templates, `utu:Responsive`) are identical too.

Malformed markup, eight fixtures of common agent mistakes: the old reader linted 1 of 8; now 8 of 8, with correct line
numbers. A test makes all six XAML rules fire on one repaired file.

| Run | Old reader | XamlParser |
|---|---|---|
| Hook, one edit, JIT | 159 ms | 227 ms |
| Hook, one edit, ReadyToRun | 150 ms | 185 ms |
| Full scan, Toolkit samples, JIT | 559 ms | 759 ms |
| Full scan, Toolkit samples, ReadyToRun | 340 ms | 463 ms |

Both hook numbers are under the 250 ms budget. ReadyToRun is still worth shipping (.NET 10 RID-specific tool packages).

## Issues found in Uno.XamlParser 0.10.0-dev.74

To raise with the maintainer. The first is a correctness bug that affects every consumer, not only uno-lint.

1. **The quote-repair sanitizer corrupts valid XAML.** Any element text containing `:/` (`ms-appx:///Assets/x.png`,
   `http://...`) makes `MissingAttributeQuotesSanitizer` rewrite a well-formed file; the reader then stops with
   XML0001 and every element after that point is lost. It happens under the `DiagnosticsOnly` policy too, so the
   policy does not stop the rewrite. Minimal repro:
   `<ResourceDictionary xmlns=".../presentation" xmlns:x=".../xaml"><x:String x:Key="A">ms-appx:///a.png</x:String><SolidColorBrush x:Key="B" Color="Red"/></ResourceDictionary>`.
   uno-lint works around it by running sanitizers only on files that are not well-formed.
2. **Fixers cannot see element text.** `IXamlSemanticFixer` has no text callback, so `<Color>#7A67F8</Color>` is
   invisible. uno-lint adds a stream visitor for it. Ask: a text callback, or the text on the end-tag node.
3. **`IXamlSemanticVisitor` is declared but never called.** Fixers are the working extension point.
4. **`XmlNamespaces` is internal**, though the repository's guidelines tell rule authors to match framework types with
   it. uno-lint repeats the two namespace URIs.
5. **Fixers run again on a parent** each time a property element folds into it, with `IsProcessingElementStart`
   true. Report-only rules must deduplicate. Ask: a flag that tells a re-run from a real start tag.
6. **The end-tag node does not identify its element.** Rules keep their own stack to know which element closed.
7. **The repaired text is internal** (`IEditableStringXamlStreamSource`). uno-lint reads it by reflection only to
   check that repairs did not shift line numbers.
8. **One repair gap.** Balancing an early wrong close tag orphans the real close tag further down (XML0001). uno-lint
   lints the prefix read before the stop.
9. **Not thread-safe.** The collector is a singleton with per-run state; the parser's own CLI shares one pipeline
   across parallel tasks. uno-lint serializes calls.
10. **The repository does not build from a fresh clone** (NU1902 promoted to an error). The NuGet package works.

## What stays in uno-lint, and why

| Concern | Why XamlParser cannot do it |
|---|---|
| C# rules | XamlParser reads XAML only |
| Per-rule severity from `.editorconfig`, profiles | Its policy decides whether a fix is applied, not how a diagnostic is rated |
| Toolkit, MVUX, Themes gates | A fixer sees one file; the gates come from the csproj and `Directory.Build.props` chain |
| Suppression comments and counts | Not a parser concept |
| Hook attribution, exit codes, JSON, report | Product surface |

## Run it

```bash
dotnet run --project src/Uno.Lint.Cli -c Release -- <app> --profile strict
dotnet test --filter XamlParserEngineTests
```

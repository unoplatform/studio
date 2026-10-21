# XAML rules on Uno.XamlParser

The six XAML rules run inside the [Uno.XamlParser](https://www.nuget.org/packages/Uno.XamlParser) NuGet package
(`0.10.0-dev.80`) as Stage-1 semantic fixers that only report. uno-lint has no XAML reader of its own. It keeps what the
parser does not do: the C# rules, `.editorconfig` severities and profiles, project gates, suppression comments and
counts, the hook and the report. Everything below is observable through the package's public API.

## How it fits together

```
agent edit ─► uno-lint hook / lint ─► Linter (Uno.Lint.Host)
                                        ├─ .cs   ─► CSharpAnalyzer (Roslyn)                          ─┐
                                        └─ .xaml ─► XamlLintEngine                                     │
                                                      UnoLintXaml.IsWellFormed? ─► sanitizers on/off   ├─► severity, gates,
                                                      Uno.XamlParser pipeline                          │   LintReport
                                                        Stage 1: ElementTextCapture, DocumentTracker,   │
                                                                 HEX, TOKENTHEME, ICON, CARD,          │
                                                                 BACKBAR, BINDVALUE (report only)      │
                                                      UnoLintXaml.Collect ─► findings ─────────────────┘
```

- **Extension points used.** `XamlStreamPipelineOptions.SemanticFixerTypes` for the rules (`IXamlSemanticFixer`,
  reporting through `XamlSemanticFixerContext.ReportDiagnostic`), and `VisitorTypes` for one stream visitor that
  captures element text. Both are public registration lists.
- **Values as written.** uno-lint's processing policy applies Stage-0 repairs only, and the parser's own fixers are not
  registered, so rules see the markup the agent wrote.
- **Streaming.** Per-document state lives on `XamlSemanticFixerContext.Items`, and only the path of open elements is
  kept. Rules that need children or descendants (CARD, ICON's Viewbox, TOKENTHEME) decide at the end tag; BACKBAR
  decides when the root closes.

## Using the rules in another XamlParser host

Studio Live and Uno.AI already run Uno.XamlParser after agent edits. `Uno.Lint.Xaml` gives them the same rules in three
steps, the ones uno-lint's own engine runs:

```csharp
var sanitize = !UnoLintXaml.IsWellFormed(text);                 // 1. repair only malformed text (see below)
services.AddXamlStreamPipeline(o =>
{
    o.UseDefaultSanitizers = sanitize;
    UnoLintXaml.AddTo(o);                                        // 2. register the rules
}).AddXamlProcessingPipeline();
var result = await pipeline.ProcessAsync(XamlStreamSources.FromString(text, path), cancellationToken: ct);
var lint = UnoLintXaml.Collect(result, path, text, isEnabled);  // 3. findings, as uno-lint reports them
```

`Collect` applies per-file rule gating, keeps one finding for a brush that is both a literal (HEX) and single-themed
(TOKENTHEME), applies suppression comments, sorts findings by line and computes the info counters. Diagnostics read
straight from `SemanticResult.Diagnostics` skip all of that. A test runs exactly these steps on a stock pipeline with
the parser's default fixers.

A pipeline built from one service provider processes one document at a time, because the parser's semantic collector
and uno-lint's text visitor keep the current document on the instance. A host that shares a pipeline across threads
serializes `ProcessAsync`; `XamlLintEngine` does it with a semaphore, and its calls can be cancelled while they wait.

## Package behavior uno-lint works around

Each item is reproducible with the package alone, and is filed with its maintainers. The workarounds for the fixer context (element text, the closing element, re-runs on a parent) go once a release gives fixers that context.

| Behavior in 0.10.0-dev.80 | Effect | uno-lint's workaround |
|---|---|---|
| Fixers are not given element text | `<Color>#7A67F8</Color>` would be invisible | a stream visitor (`ElementTextCapture`) records it |
| Fixers run again on a parent each time a property element is folded into it, with `IsProcessingElementStart` true | report-only rules would report twice | rules skip a start position they have already seen |
| The end-tag call does not say which element closed | rules cannot tell children from the element | rules keep the open-element path |
| The repaired text is not exposed | callers cannot tell whether repairs shifted line numbers | uno-lint reads it through reflection, only after a repair; a test fails if that stops working |
| Balancing an early wrong close tag can orphan the real one further down (XML0001) | the rest of the file is not read | the elements read before the stop are linted; the file counts as partial |
| A document type declaration stops the reader | the file is not linted | none needed: XAML has no DTD, and no entity is ever expanded |

Fixed in 0.10.0-dev.80: with the default sanitizers on, valid markup whose element text contains `:/` (`ms-appx:///Assets/x.png`, `http://...`) was rewritten and the reader stopped with XML0001. uno-lint still runs the sanitizers only when `IsWellFormed` is false, so a valid file is never rewritten.

## What stays in uno-lint, and why

| Concern | Why the parser does not do it |
|---|---|
| C# rules | It reads XAML only |
| Per-rule severity from `.editorconfig`, profiles | Its policy decides whether a fix is applied, not how a diagnostic is rated |
| Toolkit, MVUX, Themes gates | A fixer sees one file; the gates come from the csproj and `Directory.Build.props` chain |
| Suppression comments and counts | Not a parser concept |
| Hook attribution, exit codes, the report | Product surface |

## Run it

```bash
dotnet run --project src/Uno.Lint.Cli -c Release -- lint <app> --profile strict
dotnet test -c Release --filter XamlEngineTests
```

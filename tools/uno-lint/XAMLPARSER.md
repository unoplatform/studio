# XAML rules on Uno.XamlParser

The six XAML rules run inside the [Uno.XamlParser](https://www.nuget.org/packages/Uno.XamlParser) NuGet package
(`0.10.0-dev.87`) as Stage-1 semantic fixers that only report. uno-lint has no XAML reader of its own. It keeps what the
parser does not do: the C# rules, `.editorconfig` severities and profiles, project gates, suppression comments and
counts, the hook and the report. Everything below is observable through the package's public API.

## How it fits together

```
agent edit ─► uno-lint hook / lint ─► Linter (Uno.Lint.Host)
                                        ├─ .cs   ─► CSharpAnalyzer (Roslyn)                          ─┐
                                        └─ .xaml ─► XamlLintEngine                                     │
                                                      UnoLintXaml.IsWellFormed? ─► sanitizers on/off   ├─► severity, gates,
                                                      Uno.XamlParser pipeline                          │   LintReport
                                                        Stage 1: DocumentTracker, HEX, TOKENTHEME,    │
                                                                 ICON, CARD, BACKBAR, BINDVALUE        │
                                                                 (report only)                         │
                                                      UnoLintXaml.Collect ─► findings ─────────────────┘
```

- **Extension points used.** `XamlStreamPipelineOptions.SemanticFixerTypes` for the rules (`IXamlSemanticFixer`,
  reporting through `XamlSemanticFixerContext.ReportDiagnostic`), a public registration list. The context gives each
  rule the element's index, its text at the end tag (`CurrentElementText`) and whether the call is a re-run on a parent
  (`IsReapplyingToParent`); the read result gives `SanitizedText` after a repair.
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
keeps the current document on the instance. A host that shares a pipeline across threads
serializes `ProcessAsync`; `XamlLintEngine` does it with a semaphore, and its calls can be cancelled while they wait.

## Package behavior uno-lint works around

Each item is reproducible with the package alone, and is filed with its maintainers.

| Behavior in 0.10.0-dev.87 | Effect | uno-lint's workaround |
|---|---|---|
| Balancing an early wrong close tag can orphan the real one further down (XML0001) | the rest of the file is not read | the elements read before the stop are linted; the file counts as partial |
| A document type declaration stops the reader | the file is not linted | none needed: XAML has no DTD, and no entity is ever expanded |

Fixed in 0.10.0-dev.87 ([uno.xamlparser#39](https://github.com/unoplatform/uno.xamlparser/pull/39)): fixers were not given the element's text, the end-tag call did not say which element closed, re-runs on a parent were indistinguishable from a first visit, and the repaired text was not exposed. uno-lint used to carry a text-capturing visitor, an open-element path with a seen-positions set, and a reflection read of the repaired text for these; `XamlSemanticFixerContext.CurrentElementIndex`, `CurrentElementText`, `IsReapplyingToParent` and `XamlStreamReadResult.SanitizedText` replace them.

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

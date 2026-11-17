# Accessibility Semantics

A screen reader reads the app through its automation tree, not its pixels. Rule 9's `AutomationProperties.AutomationId` is for tests and is never spoken, so it does not replace anything here. Write these with the element, as the page is written.

## What each element carries

| Element | Set | Why |
|---|---|---|
| An icon-only button, an image that carries meaning | `AutomationProperties.Name` | Without it the control is announced as "button", with nothing else |
| An input with a visible label beside it | `AutomationProperties.LabeledBy="{x:Bind NameLabel}"` on the input; or its `Header`, which names it already | The label and the field are otherwise two unrelated elements |
| The page title and each section title | `AutomationProperties.HeadingLevel="Level1"` on the title, `Level2` on sections | Lets a screen-reader user jump between headings |
| Text that changes by itself: a loading, saved, error or result-count message | `AutomationProperties.LiveSetting="Polite"`; `Assertive` only for an error that blocks the task | A change is otherwise silent unless focus happens to be on it |
| The main content, the navigation and a search area of a page | `AutomationProperties.LandmarkType` (`Main`, `Navigation`, `Search`, `Form`) | Lets the user skip to a region |
| A decorative image or divider | `AutomationProperties.AccessibilityView="Raw"` | Keeps noise out of the tree |

A control whose text content already says what it does (a `Button` with `Content="Save"`) needs no `Name`. Do not repeat the control type in a name: "Delete", not "Delete button".

## Names inside a list

In an item template, a row's actions are named for their item, bound like its automation id: `AutomationProperties.Name="{Binding Title}"` on the row, and a name that includes the item on a per-row icon button, so twenty rows are not twenty identical "Delete" entries.

## Names are text, in every language

A name a screen reader speaks is text the user receives. Where the app keeps its text in string resources, the name comes from them too, in every language the app ships: `x:Uid="SearchButton"` on the element, and the key `SearchButton.[using:Microsoft.UI.Xaml.Automation]AutomationProperties.Name` in each language's `Resources.resw`. A tooltip, a placeholder and the hint that shows a shortcut ("Ctrl K") are text in the same way. A name bound to the item (`{Binding Title}`) is already the user's own text.

Before the report, search the XAML for `AutomationProperties.Name="` followed by a literal: each hit is a string the other languages never got.

## What the targets do with it

Uno Platform maps these properties to each platform's accessibility API: UI Automation on Windows, `NSAccessibility` on macOS, and on WebAssembly a parallel tree of semantic elements, where a heading level becomes `<h1>` to `<h6>` and a live setting becomes `aria-live`. The same XAML serves every target.

## Checking, and what to claim

- Read the tree back from the running app with the App MCP's snapshot (the `uno-testing` skill). An icon-only control that shows no name there is a defect.
- Tab through each page you wrote: every action is reachable, in reading order, and nothing traps focus.
- Contrast and text scaling are checked per [design.md](design.md).

These are checks of the automation tree, not a run with a screen reader. Report them as that, and report a screen-reader pass as not run unless one ran.

Docs, when this page does not answer the question: `uno_platform_docs_search("accessibility AutomationProperties HeadingLevel LiveSetting LandmarkType")`.

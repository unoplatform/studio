---
name: uno-platform
description: "Entry point for building or changing any Uno Platform app: cross-platform .NET UI with WinUI/XAML or C# Markup targeting Windows, iOS, Android, WebAssembly, macOS, Linux, and embedded. Use this skill whenever the user mentions Uno Platform, an Uno project (UnoFeatures, uno.sdk, Uno.Extensions, Uno.Toolkit), WinUI XAML in a cross-platform context, Hot Design, Hot Reload, Uno Studio, or asks to scaffold, extend, restyle, navigate, test, or fix an Uno app, even when the request only says 'the app' or names a page, control, or feature. It routes to the domain skills (uno-mvux, uno-navigation, uno-toolkit, uno-themes, uno-testing) and sets the project-wide rules that apply before any of them."
metadata:
  author: uno-platform
  version: "3.0"
  category: overview
---

# Uno Platform

This skill is a map. It tells you which domain skill to load for a task and lists the rules every Uno Platform change must respect. Do not write code from this file alone: load the domain skill and its reference first, then ground details in the docs.

## Pick the domain skills

Most real requests touch two or three domains. Load every one that applies.

| The request involves | Load |
|----------------------|------|
| Loading data, binding input, Models, ViewModels, feeds, states, commands, lists, selection, paging | `uno-mvux` |
| Pages, routes, back navigation, dialogs, tab bars, navigation drawers, passing data between pages, app shell | `uno-navigation` |
| Uno Toolkit controls (`TabBar`, `NavigationBar`, `CardContentControl`, `Chip`, `DrawerControl`, `SafeArea`, `AutoLayout`, `FlexPanel`, `LoadingView`, `ShadowContainer`, and more) or its attached-property extensions | `uno-toolkit` |
| Colors, brushes, typography, Material Design 3, the Simple theme, dark mode, restyling controls, theme resource keys | `uno-themes` |
| Verifying the running app: inspecting the visual tree, clicking through flows, screenshots, assertions (needs the Uno App MCP) | `uno-testing` |

Worked examples:

- "Add a products page that lists items from our API with a bottom tab" → `uno-navigation` (tab shell, route), `uno-mvux` (`IListFeed<T>` + `FeedView`), `uno-toolkit` (`TabBar`), `uno-themes` if styling is mentioned.
- "The card on the dashboard looks flat, make it match Material" → `uno-toolkit` (`CardContentControl`), `uno-themes` (Material brushes).
- "Check that the login form shows an error on bad credentials" → `uno-testing`, with `uno-mvux` if the form logic also needs changes.

## Project-wide rules

- **Feature switches live in `<UnoFeatures>`** in the project file (for example `<UnoFeatures>Material;Navigation;Toolkit;MVUX</UnoFeatures>`). Add the feature there instead of adding raw NuGet package references; the Uno SDK resolves versions.
- **The XAML dialect is WinUI 3.** Use `Microsoft.UI.Xaml` types and `x:Bind`/`Binding` as in WinUI. Do not use WPF, UWP-only, or Xamarin.Forms/MAUI syntax.
- **Toolkit and Extensions namespaces** are `xmlns:utu="using:Uno.Toolkit.UI"`, `xmlns:uen="using:Uno.Extensions.Navigation.UI"`, and `xmlns:mvux="using:Uno.Extensions.Reactive.UI"`.
- **Prefer platform controls over hand-built approximations.** A rounded `Border` with a background is a card, so use `CardContentControl`; a row of buttons that switches views is a `TabBar`; a list with add/remove is an `IListState<T>`. The domain skills spell these out.
- **Ground API details in the docs, not memory.** Call `uno_platform_docs_search(...)`, then `uno_platform_docs_fetch(sourcePath="…")` with the `sourcePath` from a result. Never pass a URL, `.html` link, or hand-built path. Uno APIs change between releases and the docs MCP is version-correct.
- **Verify before finishing.** When the Uno App MCP is available, use `uno-testing` to confirm the change renders and behaves correctly rather than declaring success from the code alone.

## New app scaffolding

For a new project, the typical stack this plugin supports is MVUX + Navigation + Toolkit + Material. Read `uno-navigation` (`references/setup.md` and one shell template), `uno-mvux` (`references/overview.md`), and `uno-toolkit` (`references/getting-started.md`) in that order, then build the shell before individual pages.

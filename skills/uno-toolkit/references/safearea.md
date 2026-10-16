# Uno Toolkit SafeArea

## Key Principles (Stable)

- **ALWAYS use SafeArea** on pages with TextBox/PasswordBox on mobile — keyboard WILL obscure inputs otherwise
- Two usage modes: `<utu:SafeArea Insets="...">` (control) or `utu:SafeArea.Insets="..."` (attached property). The control form inserts a `ScrollViewer` as its content root whenever `Insets` contains `SoftInput`; the attached form does not, so put it on an existing `ScrollViewer`
- `Insets` values: `Left`, `Top`, `Right`, `Bottom`, `SoftInput` (keyboard), `VisibleBounds` (all four sides, what the template's `MainPage` uses), `All` (`VisibleBounds` plus `SoftInput`), `None`, or combinations
- `Mode` — `Padding` (default, adds padding) or `InsetMode.Margin` (adds margin)
- For keyboard: use `Insets="SoftInput"` or `Insets="SoftInput,Bottom"`
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Docs, for what this page does not cover

Use these when the sections above do not answer the question: search, then fetch only the section you need, with the `anchor` from the search result. A whole page stays in context for the rest of the session.

### Fetch the SafeArea Documentation

```
uno_platform_docs_search("Uno Toolkit SafeArea insets notch keyboard mobile form")
```

Primary documentation pages:
- **SafeArea Control**: `external/uno.toolkit.ui/doc/controls/SafeArea.md`
- **SafeArea How-To**: `external/uno.toolkit.ui/doc/controls/walkthroughs/SafeArea.howto.md`

Fetch the how-to:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls/walkthroughs/SafeArea.howto.md", anchor="…")
```

### For Detailed Properties Reference

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls/SafeArea.md", anchor="…")
```

## Related Skills

- `references/input-extensions.md` — Auto-focus and keyboard behavior for forms
- `references/statusbar-extensions.md` — Status bar customization

# Uno Toolkit SafeArea

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the SafeArea Documentation

```
uno_platform_docs_search("Uno Toolkit SafeArea insets notch keyboard mobile form")
```

Primary documentation pages:
- **SafeArea Control**: `external/uno.toolkit.ui/doc/controls/SafeArea.md`
- **SafeArea How-To**: `external/uno.toolkit.ui/doc/controls/walkthroughs/SafeArea.howto.md`

Fetch the how-to:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls/walkthroughs/SafeArea.howto.md")
```

### Step 2: For Detailed Properties Reference

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls/SafeArea.md")
```

## Key Principles (Stable)

- **ALWAYS use SafeArea** on pages with TextBox/PasswordBox on mobile — keyboard WILL obscure inputs otherwise
- Two usage modes: `<SafeArea Insets="...">` (control) or `utu:SafeArea.Insets="..."` (attached property)
- `Insets` values: `Left`, `Top`, `Right`, `Bottom`, `SoftInput` (keyboard), or combinations
- `Mode` — `Padding` (default, adds padding) or `InsetMode.Margin` (adds margin)
- For keyboard: use `Insets="SoftInput"` or `Insets="SoftInput,Bottom"`
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Related Skills

- `references/input-extensions.md` — Auto-focus and keyboard behavior for forms
- `references/statusbar-extensions.md` — Status bar customization

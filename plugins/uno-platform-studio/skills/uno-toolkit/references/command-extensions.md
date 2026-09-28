# Uno Toolkit Command Extensions

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the CommandExtensions Documentation

```
uno_platform_docs_search("Uno Toolkit CommandExtensions command TextBox enter ToggleSwitch ListView tap")
```

Primary documentation pages:
- **Command Extensions helper**: `external/uno.toolkit.ui/doc/helpers/command-extensions.md`
- **CommandExtensions (Chefs)**: `external/uno.chefs/doc/toolkit/CommandExtensions.md`

Fetch the helper page:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/helpers/command-extensions.md")
```

### Step 2: For Reference Documentation

```
uno_platform_docs_search("Uno Toolkit CommandExtensions attached property Command CommandParameter")
```

## Critical Rules

- There is **no `CommandTrigger` attached property**. `utu:CommandExtensions.Command="{Binding ...}"` alone is sufficient — the trigger event is determined automatically by the control type (Enter on `TextBox`/`PasswordBox`, item click on `ListView`, `SelectionChanged` on any other `Selector` such as `ComboBox`, toggle on `ToggleSwitch`, invocation on `NavigationView`, tap on any `UIElement`). Do not invent a separate trigger property.
- `ListView` requires `IsItemClickEnabled="True"`; without it the command never fires and the Toolkit logs a warning.

## Key Principles (Stable)

- `utu:CommandExtensions.Command="{Binding MyCommand}"` — attaches a command
- `utu:CommandExtensions.CommandParameter` — optional parameter; when unset the control supplies one (`ClickedItem`, `SelectedItem`, `InvokedItem`, `Text`, `Password`, `IsOn`, the item DataContext, or the element itself)
- Supported controls: TextBox (enter key), PasswordBox, ToggleSwitch, ListView (item click), other Selectors like ComboBox (selection changed), NavigationView, ItemsRepeater, any UIElement (tap)
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Related Skills

- `references/input-extensions.md` — Input field focus flow
- `references/itemsrepeater-extensions.md` — ItemsRepeater selection

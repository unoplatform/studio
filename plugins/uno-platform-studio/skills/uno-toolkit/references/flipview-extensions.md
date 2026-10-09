# Uno Toolkit FlipView Extensions

## Workflow

### Step 1: Fetch the FlipView Extensions Documentation

```
uno_platform_docs_search("Uno Toolkit FlipView extensions navigation buttons previous next arrows")
```

### Step 2: For Reference

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/helpers/FlipView-extensions.md")
```

Look for FlipViewExtensions in the helpers section.

## Key Principles (Stable)

- Two attached properties for your own `Button`s: `utu:FlipViewExtensions.Next="{Binding ElementName=flipView}"` and `utu:FlipViewExtensions.Previous="{Binding ElementName=flipView}"`; clicking the button moves the `FlipView`
- No arrow or chevron styles ship with the helper; the only related style is `NoArrowsFlipViewStyle`, which hides the built-in desktop arrows
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Related Skills

- (standalone helper, no primary dependencies)

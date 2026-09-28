# Visual Tree Inspection Guide

This guide explains how to use the `uno_app_visualtree_snapshot` tool to inspect and interact with UI elements in Uno Platform applications.

## Understanding the Visual Tree

The snapshot is the hierarchical structure of every UI element currently rendered, as an indented text outline: one element per line, two spaces of indentation per level of nesting. Each line can carry:

- **Type**: the element class (`Button`, `TextBox`, `Grid`); library and framework types are prefixed `lib:`
- **Handle**: the reference the interaction tools take, introduced by `^`
- **Name**: `x:Name` or `AutomationProperties.Name`, introduced by `#`
- **Source position**: `:line` or `:line:column` in the XAML file named by the enclosing `@` scope
- **Text**: the element's text content, in quotes
- **Automation patterns**, **bindings**, **DataContext type**, **opacity**, and **bounds** at the higher detail levels

## Tool Parameters

### detail (default: "compact")

- `"compact"`: structure, names, text, and source positions. Fast; enough to find an element and click it.
- `"normal"`: adds the automation patterns (`[i t x s v r c]`), classic `{Binding}` hints, locally set DataContext types, and state flags. Use it whenever you will act on elements or check state.
- `"full"`: adds framework-internal nodes, `@@x,y,w,h` bounds, and `!offscreen` flags. Large; use it for coordinate clicks and failure diagnosis.

### includeHidden (default: false)

When `true`, collapsed elements are listed with a `!hidden` flag at `"normal"` or `"full"` detail (`"compact"` lists them without the flag), so pair it with `detail: "normal"` to prove something is collapsed. Use it for that or to find an element that appears later; never interact with a hidden element.

### elementRef (optional)

Scopes the snapshot to one element's subtree. Pass the bare handle of a container to keep a large tree small.

## Reading the Output

### Line format

```
@ Views/MainPage.xaml ^0  (Page, dc:MainVM)
  Grid ^1 #RootGrid :3
    TextBlock ^4 :7  "Welcome"
    Button ^5 #Save :9 [i]  "Save" IsEnabled={CanSave}
    TextBox ^6 #Email :11 [v]  Text={Email,2way}
    lib:ProgressRing ^7  !lib
```

| Token | Meaning |
|-------|---------|
| `@ File ^N (Kind, dc:VM)` | Opens a source-file scope; `:line` values below are relative to that file |
| `Type ^N` | Element type and handle. The handle is the bare token after `^`: for `Button ^5`, pass `"5"`, never `"^5"` |
| `lib:Type` and `!lib` | Library or framework element; not editable and usually not what you test |
| `#Name` | `x:Name` or `AutomationProperties.Name` |
| `:L` / `:L:C` | Source line and column |
| `"text"` | Text content |
| `[i t x s v r c]` | Supported automation patterns (`normal`+): invoke, toggle, expandCollapse, selectionItem, value, rangeValue, scroll |
| `Prop={Path}` | A classic `{Binding}`; `{Path,2way}` shows a non-default mode, `{Path\|conv}` a converter. Compiled `x:Bind` is not shown |
| `dc:Type` | The element owns a locally set DataContext of that type |
| `o:.5` | Opacity (omitted when 1); `xf` marks a RenderTransform |
| `@@x,y,w,h` | Arranged bounds relative to the snapshot root (`full` only) |
| `!hidden` / `!offscreen` / `!code` | Collapsed (with `includeHidden`), outside the window (`full`), or created in code |

The tree shows binding paths, not values. To read `IsEnabled`, `IsChecked`, or a bound `Text` at this moment, use `uno_app_get_element_datacontext` on the element or a nearby container, or check a screenshot.

### With bounds

```
Button ^5 #Submit :9 [i]  "Submit" @@100,200,150,40
```

The centre point for `uno_app_pointer_click` is `x + w/2, y + h/2`: here `175, 220`.

## Common Element Types

### Layout Containers

- `Grid` - Row/column-based layout
- `StackPanel` - Linear stacking (horizontal or vertical)
- `Border` - Single-child with border/background
- `ScrollViewer` - Scrollable content area (`[c]`)

### Input Elements

- `Button` - Clickable button (`[i]`, use default action)
- `TextBox` - Text input (`[v]`: `setValue`, or focus then type)
- `PasswordBox` - Secure text input
- `ComboBox` - Dropdown selection (`[x]`)
- `CheckBox` - Boolean toggle (`[t]`)
- `RadioButton` - Exclusive selection (`[st]`)
- `Slider`, `NumberBox` - Range value (`[r]`: `setRangeValue`)
- `ToggleSwitch` - On/off toggle (`[t]`)

### Display Elements

- `TextBlock` - Read-only text
- `Image` - Image display
- `ProgressRing` / `ProgressBar` - Loading indicators
- `ListView` / `ItemsRepeater` - List of items

### Navigation Elements

- `NavigationView` - Side navigation
- `TabBar` - Uno Toolkit tab bar
- `Frame` - Page container

## Finding Elements

### By name

Look for `#Name`:
```
Button ^5k #SaveButton :41:10 [i]  "Save"
```

`AutomationProperties.AutomationId` is not shown; set `AutomationProperties.Name` (or `x:Name`) on elements a test must find.

### By text

For buttons and text elements:
```
Button ^5k :41:10 [i]  "Submit"
TextBlock ^5m :12:10  "Welcome, User"
```

### By type and position

When elements lack names and text:
1. Find the parent container
2. Look for elements of the type in document order
3. Use the nth occurrence

### By hierarchy

Follow the indentation:
```
@ Views/LoginPage.xaml ^a  (Page)
  Grid ^b #RootGrid :7:4
    StackPanel ^c #LoginPanel :12:6
      Button ^d #LoginButton :20:8 [i]  "Log in"
```

### Inside templates

A `@ Styles/Theme.xaml ^N` scope inside a control means you are looking at its template; the element you named is the line above the scope. Act on the named element, not on the template's `ContentPresenter`.

## Handling Dynamic Content

### Lists and collections

List items have handles but may be recycled:
```
ListView ^40 #Books :30:8
  lib:ListViewItem ^41  dc:Book  !lib
    TextBlock ^42 :44:14  "First Item"
  lib:ListViewItem ^43  dc:Book  !lib
    TextBlock ^44 :44:14  "Second Item"
```

**Important:** Item handles change when scrolling or when the source changes. Always take a fresh snapshot before interacting. Count items by counting the `dc:` or template lines directly under the list.

### Conditional UI

Elements appear and disappear with state:
- Use `includeHidden: true` to see collapsed elements
- Refresh the tree after state changes
- Wait for async operations to complete

### Loading Content

During loading you may see empty containers, placeholders, or progress indicators. Wait for loading to complete before proceeding.

## Debugging Tips

### Element not found

1. Take a `detail: "full"` snapshot to see framework nodes
2. Set `includeHidden: true` to see collapsed elements
3. Check whether the element is on a different page
4. Verify the XAML defines the expected element

### Wrong element focused

1. Use Tab navigation to move focus
2. Use `setValue` on a `[v]` element instead of typing
3. Click the element first

### Stale handles

After any UI change:
1. Refresh the visual tree
2. Use handles from the fresh snapshot only
3. Never cache handles between operations

## Performance

For large trees:
- Keep `detail` at `"compact"` or `"normal"`
- Scope with `elementRef` to the page or container of interest
- Snapshot before a series of related reads, and refresh only when state changes

## Best Practices Summary

1. **Use `detail: "normal"`** when you will act on elements; `"full"` only for coordinates and diagnosis
2. **Name the elements a test needs** with `x:Name` or `AutomationProperties.Name`
3. **Refresh the tree after UI changes** before interacting
4. **Prefer handles over coordinates**
5. **Read values from the DataContext**, not from binding hints
6. **Handle dynamic content** with fresh snapshots

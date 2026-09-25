# Visual Tree Inspection Guide

This guide explains how to effectively use the `uno_app_visualtree_snapshot` tool to inspect and interact with UI elements in Uno Platform applications.

## Understanding the Visual Tree

The visual tree represents the hierarchical structure of all UI elements currently rendered in the application. Each element in the tree includes:

- **Type**: The element class (Button, TextBox, Grid, etc.)
- **Name**: The `x:Name` if defined in XAML
- **Handle**: A unique reference for interaction tools
- **Properties**: Relevant properties like Content, Text, IsEnabled
- **Automation Properties**: AutomationId, Name, HelpText
- **Bounds**: Position and size (when requested)

## Tool Parameters

### justMyCode (default: true)

When `true`, filters the tree to show only elements defined in your application code:
- Removes internal framework elements
- Hides template internals
- Shows cleaner, more navigable output

Set to `false` when you need to:
- Debug template issues
- Interact with framework-level elements
- Understand complete element composition

### includeBounds (default: false)

When `true`, includes element position and size:
- X, Y coordinates (absolute physical pixels)
- Width and Height
- Useful for coordinate-based interactions

Set to `true` when:
- Using `uno_app_pointer_click`
- Debugging layout issues
- Calculating click positions

### includeHidden (default: false)

When `true`, includes hidden/collapsed elements:
- Elements with `Visibility.Collapsed`
- Elements with `Opacity = 0`
- Elements outside the visible viewport

Set to `true` when:
- Looking for elements that may appear later
- Debugging visibility binding issues
- Testing conditional UI logic

## Reading the XML Output

### Basic Element Structure

```xml
<StackPanel handle="sp_001" Orientation="Vertical">
  <TextBlock handle="tb_001" Text="Welcome" />
  <Button handle="btn_001" x:Name="LoginButton" Content="Log In" IsEnabled="true">
    <Button.AutomationProperties>
      <AutomationId>LoginBtn</AutomationId>
      <Name>Log In Button</Name>
    </Button.AutomationProperties>
  </Button>
</StackPanel>
```

### Key Attributes to Look For

| Attribute | Purpose |
|-----------|---------|
| `handle` | Required for interaction tools |
| `x:Name` | Developer-assigned name |
| `AutomationId` | Test automation identifier |
| `Content` | Button/ContentControl content |
| `Text` | TextBlock/TextBox text |
| `IsEnabled` | Whether element accepts interaction |
| `IsChecked` | Checkbox/Toggle state |

### With Bounds Included

```xml
<Button handle="btn_001" 
        x:Name="SubmitButton" 
        Content="Submit"
        Bounds="100,200,150,40">
  <!-- X=100, Y=200, Width=150, Height=40 -->
</Button>
```

The center point for clicking would be:
- X: 100 + (150/2) = 175
- Y: 200 + (40/2) = 220

## Common Element Types

### Layout Containers

- `Grid` - Row/column-based layout
- `StackPanel` - Linear stacking (horizontal or vertical)
- `Border` - Single-child with border/background
- `ScrollViewer` - Scrollable content area

### Input Elements

- `Button` - Clickable button (use default action)
- `TextBox` - Text input (focus, then type)
- `PasswordBox` - Secure text input
- `ComboBox` - Dropdown selection
- `CheckBox` - Boolean toggle
- `RadioButton` - Exclusive selection
- `Slider` - Range value selection
- `ToggleSwitch` - On/off toggle

### Display Elements

- `TextBlock` - Read-only text
- `Image` - Image display
- `ProgressRing` / `ProgressBar` - Loading indicators
- `ListView` / `ItemsRepeater` - List of items

### Navigation Elements

- `NavigationView` - Side navigation
- `TabView` - Tab-based navigation
- `Frame` - Page container

## Finding Elements Strategies

### By Name

Look for `x:Name` attribute:
```xml
<Button x:Name="SaveButton" handle="btn_save" ... />
```

### By AutomationId

Best practice for test automation:
```xml
<Button handle="btn_001">
  <Button.AutomationProperties>
    <AutomationId>SaveBtn</AutomationId>
  </Button.AutomationProperties>
</Button>
```

### By Content/Text

For buttons and text elements:
```xml
<Button handle="btn_001" Content="Submit" />
<TextBlock handle="tb_001" Text="Welcome, User" />
```

### By Type and Position

When elements lack unique identifiers:
1. Find parent container
2. Look for elements by type in order
3. Use the nth occurrence

### By Hierarchy

Navigate the tree structure:
```xml
<Page>
  <Grid x:Name="RootGrid">
    <StackPanel x:Name="LoginPanel">
      <Button x:Name="LoginButton" handle="target_handle" />
    </StackPanel>
  </Grid>
</Page>
```

## Handling Dynamic Content

### Lists and Collections

ListView items have handles but may be recycled:
```xml
<ListView handle="list_001">
  <ListViewItem handle="item_001" Content="First Item" />
  <ListViewItem handle="item_002" Content="Second Item" />
  <ListViewItem handle="item_003" Content="Third Item" />
</ListView>
```

**Important:** Item handles may change when scrolling. Always get a fresh snapshot before interacting.

### Conditional UI

Elements may appear/disappear based on state:
- Use `includeHidden: true` to see collapsed elements
- Refresh tree after state changes
- Wait for async operations to complete

### Loading Content

During loading, you may see:
- Empty containers
- Placeholder elements
- Progress indicators

Wait for loading to complete before proceeding.

## Debugging Tips

### Element Not Found

1. Set `justMyCode: false` to see all elements
2. Set `includeHidden: true` to see collapsed elements
3. Check if element is inside a different page/view
4. Verify XAML defines the expected element

### Wrong Element Focused

1. Get visual tree to verify focus state
2. Use Tab navigation to move focus
3. Try clicking the element first

### Stale Handles

After significant UI changes:
1. Always refresh the visual tree
2. Use new handles from fresh snapshot
3. Don't cache handles between operations

## Performance Considerations

### Large Trees

For complex UIs, the tree can be large:
- Use `justMyCode: true` to reduce size
- Navigate to specific page/view first
- Look for parent containers to narrow scope

### Frequent Updates

Don't call visual tree snapshot excessively:
- Get snapshot before a series of related operations
- Refresh only when state changes significantly
- Cache element locations for multiple clicks in same area

## Best Practices Summary

1. **Always set justMyCode: true** unless debugging framework issues
2. **Use AutomationId** for reliable element identification
3. **Refresh tree after UI changes** before interacting
4. **Get bounds only when needed** for coordinate clicks
5. **Prefer handles over coordinates** for reliability
6. **Check IsEnabled** before attempting interaction
7. **Handle dynamic content** with fresh snapshots

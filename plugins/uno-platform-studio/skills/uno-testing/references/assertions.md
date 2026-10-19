# Uno Platform Test Assertions Skill

> **Prerequisite:** This reference requires the **Uno App MCP** (the `uno_app_*` tools), provided by the Uno tooling. It is a separate server from the documentation MCP used by other skills. Before proceeding, confirm the `uno_app_*` tools are available; if they are not, stop and tell the user to sign in to Uno Platform and enable the Uno tooling rather than attempting to test without them.

This reference provides patterns for validating UI state and asserting test conditions when testing Uno Platform applications with the Uno App MCP tools.

## Assertion Types

### Visual Assertions (Screenshots)

Use `uno_app_get_screenshot` to capture visual state:

```
uno_app_get_screenshot(fileType: "png", quality: 100)
```

Omit `path` to receive the image in the result. A `path` must be inside the App MCP workspace (the solution folder); if the save reports a missing path, create the folder and retry.

**Validation approaches:**
1. **Visual comparison**: Compare with baseline images
2. **Manual review**: Save screenshots for human inspection
3. **AI-assisted validation**: Describe expected visual state and verify

### Structure Assertions (Visual Tree)

Use `uno_app_visualtree_snapshot` to verify UI structure:

**Assert element exists:**
- Get visual tree snapshot
- Search for the element by `#Name`, text, or type
- Fail if element not found

**Assert element not visible:**
- Get tree with `includeHidden: false`
- Verify element is NOT in the returned tree

**Assert element visible:**
- Get tree with `includeHidden: false`
- Verify element IS in the returned tree

### Property Assertions

The snapshot shows binding paths (`IsEnabled={CanSave}`), not current values. Read the value from the element's DataContext, or from a screenshot for visual state:

| Property | What to Check |
|----------|---------------|
| `IsEnabled` | Element can be interacted with |
| `IsChecked` | Checkbox/Toggle state |
| `Content` | Button/Label text |
| `Text` | TextBox/TextBlock content |
| `SelectedIndex` | ComboBox/List selection |
| `Value` | Slider/ProgressBar value |

### Data Assertions

Use `uno_app_get_element_datacontext` (Pro or Business licence; on Community, report these assertions as not run) to validate bound data:

```
uno_app_get_element_datacontext(elementRef: "5k")
```

Verify:
- ViewModel property values
- Collection counts and items
- Computed properties
- State flags

## Common Assertion Patterns

### Assert Page Loaded

```
1. uno_app_visualtree_snapshot(detail: "normal")
2. Search for expected page/view element
3. Verify page-specific elements exist
4. Optionally capture screenshot for visual confirmation
```

### Assert Button State

```
1. uno_app_visualtree_snapshot(detail: "normal")
2. Find the button by `#Name` or its quoted text
3. Read `IsEnabled` from the DataContext (or confirm the disabled look in a screenshot)
4. Verify the quoted text matches
```

### Assert Text Content

```
1. uno_app_visualtree_snapshot(detail: "normal")
2. Find the TextBlock/TextBox element
3. Verify its quoted text matches the expected value (a TextBox shows its bound value through the DataContext)
```

### Assert List Items

```
1. uno_app_visualtree_snapshot(detail: "normal")
2. Find ListView/ItemsRepeater element
3. Count the item lines directly under it (each carries `dc:ItemType`)
4. Verify the expected count, or read the collection count from the DataContext
5. Check item content if needed
```

### Assert Dialog Displayed

```
1. uno_app_visualtree_snapshot(detail: "normal")
2. Search for ContentDialog or Popup element
3. Verify dialog-specific content exists
4. Capture screenshot for visual confirmation
```

### Assert Form Validation Errors

```
1. Enter invalid data in form
2. Attempt to submit
3. uno_app_visualtree_snapshot(detail: "normal")
4. Look for validation error TextBlocks
5. Verify error messages match expected text
```

### Assert Navigation Occurred

```
1. Trigger navigation action
2. uno_app_visualtree_snapshot(detail: "normal")
3. Verify previous page elements are gone
4. Verify new page elements are present
```

## Screenshot-Based Assertions

### Capturing for Comparison

```
uno_app_get_screenshot(
  fileType: "png",
  quality: 100,
  path: "<solution folder>/tests/screenshots/test_case_name.png"
)
```

If the save reports a missing path, create the folder and retry.

### Assertion Strategies

**Pixel-perfect comparison:**
- Use when UI is deterministic
- Be aware of anti-aliasing differences
- May need tolerance threshold

**Region comparison:**
- Compare specific areas of interest
- Ignore dynamic content regions
- Focus on stable UI elements

**Semantic comparison:**
- Describe what should be visible
- Verify key visual elements
- Allow for minor variations

### Handling Dynamic Content

When screenshots contain dynamic data:
1. Use data mocking/seeding
2. Focus assertions on static elements
3. Mask dynamic regions
4. Use structural validation instead

## Data Context Assertions

### Verify ViewModel State

```
1. Get element handle from visual tree
2. uno_app_get_element_datacontext(elementRef: "<handle>")
3. Parse returned XML
4. Assert property values match expected state
```

### Example Assertions

**Assert user is logged in:**
- Get DataContext of user info element
- Verify `IsLoggedIn: true`
- Verify `Username` has value

**Assert item count:**
- Get DataContext with collection property
- Verify collection count matches expected

**Assert computed state:**
- Get DataContext after action
- Verify derived/computed properties updated

## Timing and Retry Patterns

### Wait for Condition

When UI updates asynchronously:

```
Loop (max N attempts):
  1. Get visual tree snapshot
  2. Check for expected condition
  3. If condition met, break
  4. Wait brief interval
  5. Retry
Fail if condition not met after all attempts
```

### Wait for Element

```
Retry up to 10 times:
  1. uno_app_visualtree_snapshot(detail: "normal")
  2. Search for expected element
  3. If found, return success
  4. Wait 500ms
  5. Retry
Fail with "Element not found" if all retries exhausted
```

### Wait for Loading Complete

```
Retry until no loading indicators:
  1. uno_app_visualtree_snapshot(detail: "normal")
  2. Search for ProgressRing, ProgressBar, loading text
  3. If none found, loading complete
  4. Wait 500ms
  5. Retry
```

## Failure Handling

### Capture Diagnostic Info

On assertion failure:
1. Capture screenshot for visual debugging
2. Get the full visual tree (detail: "full")
3. Log expected vs actual values
4. Save DataContext if relevant

### Common Failure Causes

| Failure | Likely Cause | Resolution |
|---------|--------------|------------|
| Element not found | Timing issue | Add retry/wait |
| Wrong property value | State not updated | Wait for async complete |
| Screenshot mismatch | Dynamic content | Use data seeding or masking |
| Stale handle | Tree changed | Refresh visual tree |

## Test Organization Tips

### Before Each Test
1. Ensure app is running: `uno_app_get_runtime_info`
2. Navigate to known initial state
3. Clear any persistent test data

### After Each Test
1. Capture final screenshot (pass or fail)
2. Reset app state if possible
3. Log assertion results

### On Failure
1. Capture diagnostic screenshot
2. Dump visual tree
3. Save DataContext of relevant elements
4. Preserve failure artifacts for debugging

## Best Practices

1. **Be specific**: Assert on exact values, not just existence
2. **Use proper identifiers**: rely on `x:Name` or `AutomationProperties.Name` over text and position
3. **Handle timing**: Always account for async operations
4. **Capture evidence**: Screenshots on both pass and fail
5. **Test independence**: Each test should set up its own state
6. **Meaningful messages**: Include context in failure messages

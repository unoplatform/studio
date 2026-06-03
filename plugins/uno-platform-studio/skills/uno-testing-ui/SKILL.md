---
name: uno-testing-ui
description: "Automates UI testing for Uno Platform applications using the Uno App MCP server tools."
when_to_use: "Use this skill when performing automated UI testing, visual validation, interaction testing, or end-to-end testing of Uno Platform cross-platform applications. Covers app lifecycle management, visual tree inspection, element interaction, screenshot capture, and test assertions."
compatibility: Requires the Uno App MCP server to be configured and available. Works with Uno Platform applications targeting desktop (Windows, macOS, Linux), WebAssembly, iOS, and Android.
metadata:
  author: uno-platform
  version: "1.3"
  category: testing
---

# Uno Platform UI Testing Agent Skill

This skill enables automated UI testing of Uno Platform applications using the Uno App MCP server tools. It provides comprehensive guidance for launching apps, inspecting UI elements, simulating user interactions, and validating visual output.

## Overview

The Uno App MCP server exposes a set of tools that allow agents to:
- Start and stop Uno Platform applications
- Capture screenshots for visual validation
- Inspect the visual tree to discover UI elements
- Interact with elements using automation peers
- Simulate keyboard and pointer input
- Retrieve element data context for state validation

## Prerequisites

1. The Uno App MCP server must be configured in your environment
2. The target application must be an Uno Platform project with MCP client integration
3. The project must have valid target frameworks (e.g., `net9.0-desktop`, `net9.0-browserwasm`)

## Quick Start Workflow

For most UI testing scenarios, follow this workflow:

1. **Start the app** → `uno_app_start`
2. **Verify app is running** → `uno_app_get_runtime_info`
3. **Capture initial screenshot** → `uno_app_get_screenshot`
4. **Get visual tree** → `uno_app_visualtree_snapshot`
5. **Interact with elements** → `uno_app_element_peer_default_action` or input tools
6. **Validate results** → Screenshots + visual tree inspection
7. **Close the app** → `uno_app_close`

## Available MCP Tools

### App Lifecycle Tools

| Tool | Description |
|------|-------------|
| `uno_app_start` | Starts the application in debug mode with Hot Reload |
| `uno_app_get_runtime_info` | Gets runtime info (PID, window title, platform, uptime) |
| `uno_app_close` | Terminates the running application (desktop only) |

### Visual Inspection Tools

| Tool | Description |
|------|-------------|
| `uno_app_get_screenshot` | Captures a screenshot for visual validation |
| `uno_app_visualtree_snapshot` | Gets XML visual tree snapshot with element handles |
| `uno_app_get_element_datacontext` | Gets the DataContext of a specific element |

### Interaction Tools

| Tool | Description |
|------|-------------|
| `uno_app_element_peer_default_action` | Invokes the default automation action on an element |
| `uno_app_element_peer_action` | Invokes a specific automation peer action |
| `uno_app_pointer_click` | Performs pointer click at coordinates |
| `uno_app_key_press` | Presses a keyboard key |
| `uno_app_type_text` | Types text using keyboard simulation |

### Diagnostic Tools

| Tool | Description |
|------|-------------|
| `uno_app_get_memory_counters` | Gets memory diagnostics for the running app |

## Detailed Tool Usage

### Starting an Application

Use `uno_app_start` to launch the application:

```
Tool: uno_app_start
Parameters:
  - projectPath: Full path to the .csproj file (must be within MCP working directory)
  - targetFramework: Target framework moniker (e.g., "net9.0-desktop", "net9.0-browserwasm")
  - args: Optional command-line arguments (array)
```

**Example scenarios:**
- Desktop testing: `targetFramework = "net9.0-desktop"`
- WebAssembly testing: `targetFramework = "net9.0-browserwasm"`

**Important:** Always verify the app started successfully by calling `uno_app_get_runtime_info` afterward.

### Inspecting the Visual Tree

Use `uno_app_visualtree_snapshot` to get an XML representation of the UI:

```
Tool: uno_app_visualtree_snapshot
Parameters:
  - justMyCode: Filter to user-defined elements only (default: true)
  - includeBounds: Include element bounds for coordinate-based interaction (default: false)
  - includeHidden: Include hidden/collapsed elements (default: false)
```

The returned XML contains element handles (refs) that can be used with interaction tools. Each element includes:
- Element type and name
- Automation properties
- Handle/reference for interactions
- Bounds (if requested)

**Tip:** Set `justMyCode: true` to reduce noise from framework elements and focus on application UI.

### Interacting with Elements

**Preferred approach:** Use automation peers when element handles are available.

1. **Default Action** (most common):
   ```
   Tool: uno_app_element_peer_default_action
   Parameters:
     - elementRef: Handle from visual tree snapshot
   ```
   This invokes the natural action (click for buttons, toggle for checkboxes, etc.)

2. **Specific Action** (for advanced scenarios):
   ```
   Tool: uno_app_element_peer_action
   Parameters:
     - elementRef: Handle from visual tree snapshot
     - action: Specific automation action name
     - actionParameters: Optional parameters array
   ```

**Fallback approach:** Use coordinate-based input when handles aren't suitable.

3. **Pointer Click**:
   ```
   Tool: uno_app_pointer_click
   Parameters:
     - x: Absolute physical X coordinate
     - y: Absolute physical Y coordinate
     - button: "left", "middle", or "right"
     - clickCount: Number of clicks (default: 1)
     - delayBetweenPresseAndReleaseInMs: Delay in milliseconds (default: 10)
   ```

4. **Keyboard Input**:
   ```
   Tool: uno_app_key_press
   Parameters:
     - virtualKey: VirtualKey name (e.g., "Enter", "Tab", "A")
     - virtualKeyModifiers: Optional modifiers ("control", "shift", "menu", "windows")
     - unicodeKey: Optional explicit unicode character
   ```

5. **Text Entry**:
   ```
   Tool: uno_app_type_text
   Parameters:
     - text: String of text to type
     - intervalInMs: Delay between key presses
   ```

### Capturing Screenshots

Use `uno_app_get_screenshot` for visual validation:

```
Tool: uno_app_get_screenshot
Parameters:
  - fileType: "png" or "jpeg"
  - quality: Image quality 1-100 (default: 75)
  - path: Optional file path to save the screenshot
```

Screenshots are essential for:
- Visual regression testing
- Documenting test execution
- Debugging failed tests
- Validating layout and styling

### Validating Element State

Use `uno_app_get_element_datacontext` to inspect bound data:

```
Tool: uno_app_get_element_datacontext
Parameters:
  - elementRef: Handle from visual tree snapshot
```

Returns an XML representation of the element's DataContext, useful for verifying:
- ViewModel state
- Data binding correctness
- Business logic results

## Testing Patterns

### Pattern 1: Simple Click Test

1. Start the app
2. Get visual tree snapshot
3. Find the target button by name/type
4. Call `uno_app_element_peer_default_action` with its handle
5. Get new visual tree or screenshot
6. Verify expected changes

### Pattern 2: Form Input Test

1. Start the app
2. Navigate to the form (if needed)
3. Get visual tree to find input fields
4. For each field:
   - Call `uno_app_element_peer_default_action` to focus
   - Call `uno_app_type_text` to enter data
5. Submit the form
6. Validate results via screenshot or data context

### Pattern 3: Navigation Test

1. Start the app
2. Get initial visual tree snapshot
3. Trigger navigation (click nav button, etc.)
4. Wait briefly for navigation to complete
5. Get new visual tree snapshot
6. Verify the expected page/view is displayed

### Pattern 4: Visual Regression Test

1. Start the app
2. Navigate to target state
3. Capture screenshot with `uno_app_get_screenshot`
4. Compare with baseline image
5. Report differences

## Best Practices

### Element Selection
- **Prefer automation peers** over coordinate clicks for reliability
- Use `justMyCode: true` to filter framework elements
- Look for elements with `x:Name` or `AutomationProperties.AutomationId`

### Test Stability
- Always verify app is running with `uno_app_get_runtime_info` before testing
- Add brief delays between rapid interactions if needed
- Refresh visual tree after actions that change UI state

### Assertions
- Use screenshots for visual validation
- Use `uno_app_get_element_datacontext` for data validation
- Check visual tree for element presence/absence

### Cleanup
- Always call `uno_app_close` at the end of tests (desktop)
- Handle test failures gracefully with cleanup

## Troubleshooting

### "No connected app instance"
- The app hasn't started or has crashed
- Call `uno_app_start` to launch the app
- Check build output for errors

### Element not found in visual tree
- Element may be hidden/collapsed (try `includeHidden: true`)
- Element may be dynamically created (refresh the tree)
- Check if using correct framework filter

### Interaction not working
- Verify element handle is from latest tree snapshot
- Check if element is enabled and visible
- Try coordinate-based input as fallback

### Screenshots are blank or incorrect
- Ensure app window is visible and not minimized
- Check for correct target framework
- Verify app has finished rendering

## References

For detailed implementation patterns, see:
- [references/INTERACTION-PATTERNS.md](references/INTERACTION-PATTERNS.md) - Detailed interaction examples
- [references/VISUAL-TREE-GUIDE.md](references/VISUAL-TREE-GUIDE.md) - Visual tree inspection guide

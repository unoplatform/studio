# Uno Platform UI Testing

> **Prerequisite:** This reference requires the **Uno App MCP** (the `uno_app_*` tools), provided by the Uno tooling. It is a separate server from the documentation MCP used by other skills. Confirm the `uno_app_*` tools are available first (through `uno_discover_tools` when the client only lists the proxy tools); if they are not, stop and tell the user to sign in to Uno Platform and enable the Uno tooling rather than attempting to test without them.

This reference covers launching apps, inspecting UI elements, simulating user interactions, and validating visual output with the Uno App MCP tools.

## Overview

The Uno App MCP server exposes tools that let an agent:
- Start and stop Uno Platform applications
- Capture screenshots for visual validation
- Inspect the visual tree to discover UI elements
- Interact with elements through automation peers
- Simulate keyboard and pointer input
- Read an element's DataContext for state validation

## Prerequisites

1. The Uno App MCP server is configured, its host is connected, a solution is selected, and its app tools are licensed (`uno_health` reports a non-zero `toolCount`; a zero with `HostUnreachable`, `NoSolutionFound`, or `WorkspaceAmbiguous` in `issues` is a connection or workspace problem, not a licence problem). A Community licence also reports a non-zero `toolCount` but lacks the tools marked Pro or Business below; `uno_discover_tools` shows which ones this machine has
2. The target application is an Uno Platform project (Uno.Sdk 6.x) under the App MCP's workspace folder
3. The project has a target framework the tools can launch: `netX.0-desktop` for desktop, `netX.0-browserwasm` for WebAssembly. Read the exact monikers from `<TargetFrameworks>` in the `.csproj`

## Quick Start Workflow

1. **Build once** → `dotnet build <app>.csproj -f net10.0-desktop`, so compile errors surface as build output
2. **Start the app** → `uno_app_start(projectPath, targetFramework, connectionTimeoutSeconds: 120)`
3. **Verify it is connected** → `uno_app_get_runtime_info` (poll it; never call `uno_app_start` again to "retry")
4. **Capture the initial screenshot** → `uno_app_get_screenshot(fileType: "png")`
5. **Get the visual tree** → `uno_app_visualtree_snapshot(detail: "normal")`
6. **Interact with elements** → `uno_app_element_peer_default_action` for `[i]` elements, `uno_app_element_peer_action` for the other pattern letters, or the input tools
7. **Validate results** → screenshots, the visual tree, and DataContext
8. **Close the app** → `uno_app_close`, always before the next build

## Available MCP Tools

### App Lifecycle Tools

| Tool | Description |
|------|-------------|
| `uno_app_start` | Builds and starts the application in debug mode with Hot Reload; kills any instance it already started |
| `uno_app_get_runtime_info` | Reports whether an app instance is connected (PID, window title, uptime) |
| `uno_app_get_memory_counters` | Business licence only. Returns the running app's memory counters (for example total available memory); useful for leak checks across a flow |
| `uno_app_close` | Terminates the running application (desktop only) |
| `uno_devserver_diagnostics` | Reports the DevServer port, solution, instance count, and last launch outcome without changing anything |

### Visual Inspection Tools

| Tool | Description |
|------|-------------|
| `uno_app_get_screenshot` | Captures the window, or one element, as an image |
| `uno_app_visualtree_snapshot` | Returns the visual tree as an indented text outline with element handles |
| `uno_app_get_element_datacontext` | Pro or Business licence. Returns an element's DataContext as XML |

### Interaction Tools

| Tool | Description |
|------|-------------|
| `uno_app_element_peer_default_action` | Runs `invoke` on an element; works only on `[i]` elements |
| `uno_app_element_peer_action` | Pro or Business licence. Invokes a specific automation pattern action (`toggle`, `setValue`, ...) |
| `uno_app_pointer_click` | Clicks at window coordinates, in the same units as the snapshot's `@@` bounds |
| `uno_app_key_press` | Presses one key on the focused element |
| `uno_app_type_text` | Types text into the focused element |

**Community fallback.** When `uno_discover_tools` does not list the Pro or Business tools: take an unscoped `detail: "full"` snapshot, `uno_app_pointer_click` the centre of the field's `@@x,y,w,h` bounds to focus it, then `uno_app_type_text(text, intervalInMs: 50)`; select list items the same way. Read state from screenshots and the tree's text, and report every DataContext assertion as not run.

Some clients expose these only through a proxy: `uno_discover_tools` lists them and `uno_execute_tool(toolName, arguments)` runs one. The parameters below are the same either way.

## Detailed Tool Usage

### Starting an Application

```
Tool: uno_app_start
Parameters:
  - projectPath: Full path to the .csproj file (must be within the MCP workspace directory)
  - targetFramework: Target framework moniker from the .csproj (e.g., "net10.0-desktop", "net10.0-browserwasm")
  - connectionTimeoutSeconds: Wait up to this long for the app to connect (1-300; use 120). Omit to return as soon as the build succeeds
  - args: Optional command-line arguments (array)
  - stdoutFile: Optional file the app's stdout is written to
  - display: Optional X11 display for Linux (e.g., ":0"); ignored on other platforms
```

Each call terminates any app the tool already started. After it returns, confirm with `uno_app_get_runtime_info`.

**When the start times out:** the process is usually still launching, and a desktop app connects on its own once it is up. Poll `uno_app_get_runtime_info` every few seconds. If it keeps reporting no instance, read `stdoutFile` and call `uno_devserver_diagnostics`. Do not call `uno_app_start` again: that kills the launching process and starts the wait over.

**WebAssembly targets:** the tool starts the dev server and returns the app URL. A browser must open that URL before the runtime tools work; `connectionTimeoutSeconds` waits for it.

**Hot Design at start:** the template's `MainWindow.UseStudio();` (in `App.xaml.cs`, under `#if DEBUG`) opens Hot Design by itself on the app's first launch, and whenever it was active in the previous session. Its designer and introduction overlay cover the app, and the visual tree does not include them, so the tree looks normal while screenshots and clicks hit the designer. Before starting the app, the call should be `MainWindow.UseStudio(launchHotDesignOnStart: false);`: change it yourself in an app you created, and ask the developer first in an existing app. `true` always opens Hot Design and `null` (the default) restores the last session's state. The `UNO_HOTDESIGN_LAUNCH` environment variable and the `UnoHotDesignLaunch` MSBuild property override the argument.

**Before rebuilding:** call `uno_app_close`. A running app locks its `bin` folder, and `dotnet build` or the next `uno_app_start` then fails with `MSB3026`/`MSB3027` copy errors.

### Inspecting the Visual Tree

```
Tool: uno_app_visualtree_snapshot
Parameters:
  - detail: "compact" (default: structure and origin), "normal" (adds automation patterns, bindings, DataContext, state flags), or "full" (adds framework nodes, bounds, offscreen flags)
  - includeHidden: Include collapsed elements, flagged !hidden at "normal" or "full" detail (default: false)
  - elementRef: Optional handle; scopes the snapshot to that element's subtree
```

The result is a text outline, one element per line:

```
@ Views/MainPage.xaml ^0  (Page, dc:MainVM)
  Grid ^1 #RootGrid :3
    TextBlock ^4 :7  "Welcome"
    Button ^5 #Save :9 [i]  "Save" IsEnabled={CanSave}
```

The handle is the bare token after `^` (`"5"` for `Button ^5`; a leading `^` is stripped with a warning); `#Name` is the `x:Name` or `AutomationProperties.Name`; `[i]` lists the supported automation patterns; `Prop={Path}` shows a binding path, not its value. `references/VISUAL-TREE-GUIDE.md` explains every token.

**Tip:** use `detail: "normal"` when you will act on elements, and `elementRef` to keep a large page's tree small. Do not scope a snapshot you take for click coordinates: `@@` bounds are relative to the snapshot root, and clicks are relative to the window.

### Interacting with Elements

**Preferred approach:** automation peers, using a handle from the latest snapshot.

1. **Default action** (buttons):
   ```
   Tool: uno_app_element_peer_default_action
   Parameters:
     - elementRef: Bare handle from the snapshot ("5", not "^5")
   ```
   Always runs `invoke`, so it works only on `[i]` elements (`Button`, `HyperlinkButton`). On a `CheckBox`, `TextBox`, `ComboBox`, or list item it returns an unsupported-action failure; use the specific action below.

2. **Specific action**:
   ```
   Tool: uno_app_element_peer_action
   Parameters:
     - elementRef: Bare handle from the snapshot
     - action: invoke (default), toggle, expand, collapse, select, addToSelection, removeFromSelection, setValue, setRangeValue
     - actionParameters: Optional array; for setValue/setRangeValue the first entry is the value
   ```
   The line's pattern letters say which actions the element supports: `[t]` `toggle`, `[x]` `expand`/`collapse`, `[s]` `select`, `[v]` `setValue`, `[r]` `setRangeValue`. `setValue` fills a `TextBox` without needing keyboard focus. `ListViewItem` shows no letters on Uno, so select it by pointer or keyboard. This tool needs a Pro or Business licence; on Community, click the field's bounds to focus it, then `uno_app_type_text`.

**Fallback approach:** coordinates and keyboard, when no pattern applies.

3. **Pointer click**:
   ```
   Tool: uno_app_pointer_click
   Parameters:
     - x: X coordinate in the same units as the snapshot's `@@` bounds (logical, relative to the window; do not scale for DPI)
     - y: Y coordinate, same units
     - button: "left", "middle", or "right"
     - clickCount: Number of clicks (default: 1)
     - delayBetweenPresseAndReleaseInMs: Delay in milliseconds (default: 10)
   ```
   Take the coordinates from a `detail: "full"` snapshot without `elementRef`: the centre of the element's `@@x,y,w,h` bounds. A scoped snapshot reports bounds relative to the scoped element, which do not match the click space.

4. **Key press**:
   ```
   Tool: uno_app_key_press
   Parameters:
     - virtualKey: VirtualKey name (e.g., "Enter", "Tab", "A")
     - virtualKeyModifiers: Optional modifier ("control", "shift", "menu", "windows")
     - unicodeKey: Optional explicit unicode character (one character; longer strings are ignored)
   ```

5. **Text entry**:
   ```
   Tool: uno_app_type_text
   Parameters:
     - text: String of text to type
     - intervalInMs: Delay between key presses (required; 50 is fine)
   ```
   Both tools raise key events in-process on the element that holds XAML focus and return `false` when nothing is focused, so focus the field first: click its bounds, or Tab to it. Prefer `setValue` for form fields when it is available.

### Capturing Screenshots

```
Tool: uno_app_get_screenshot
Parameters:
  - fileType: "png" or "jpeg"
  - quality: Image quality 1-100 (default: 75)
  - path: Optional file path to save to; omit to receive the image in the result
  - elementRef: Optional handle; captures only that element
```

Without `path`, the image comes back in the tool result and can be inspected directly. With `path`:
- the path must be inside the App MCP's workspace directory (the selected solution's folder; `uno_health` reports it as `effectiveWorkspaceDirectory`), or the call fails with a security error;
- if the save fails with "Could not find a part of the path", the folder is missing: create it and retry.

Save inside the solution, then copy the file elsewhere if the task keeps screenshots outside it.

### Validating Element State

```
Tool: uno_app_get_element_datacontext
Parameters:
  - elementRef: Bare handle from the snapshot
```

Returns an XML representation of the element's DataContext. Because the snapshot shows binding paths rather than current values, this is how to check ViewModel state, collection counts, computed properties, and flags such as `IsEnabled` or `IsChecked`. It needs a Pro or Business licence; on Community, read what the screenshot and the tree's text show, and report DataContext assertions as not run.

## Testing Patterns

### Pattern 1: Simple Click Test

1. Start the app
2. Snapshot with `detail: "normal"`
3. Find the target button by `#Name` or text
4. Call `uno_app_element_peer_default_action` with its handle
5. Re-snapshot or screenshot
6. Verify the expected change

### Pattern 2: Form Input Test

1. Start the app
2. Navigate to the form (if needed)
3. Snapshot to find the input fields
4. For each field, `uno_app_element_peer_action(elementRef, "setValue", ["value"])`; or click its bounds to focus it and `uno_app_type_text(text, intervalInMs: 50)`
5. Submit the form
6. Validate through screenshot or DataContext

### Pattern 3: Navigation Test

1. Start the app
2. Take the initial snapshot
3. Trigger navigation (click the nav item, etc.)
4. Poll the tree until the new page's `@ ... (Page)` scope appears
5. Verify the expected page elements are present and the old ones are gone

### Pattern 4: Visual Regression Test

1. Start the app
2. Navigate to the target state
3. Capture a screenshot with `uno_app_get_screenshot(fileType: "png")`
4. Compare with the baseline image
5. Report differences

## Best Practices

### Element Selection
- **Prefer automation peers** over coordinate clicks
- Look for elements with `x:Name` or `AutomationProperties.Name`, and suggest adding one when a test has to fall back to text or position
- Act on the named element, not on the `ContentPresenter` inside its template

### Test Stability
- Confirm the app is connected with `uno_app_get_runtime_info` before testing
- Refresh the visual tree after actions that change UI state
- Poll rather than sleep: re-snapshot until the expected element appears

### Assertions
- Use screenshots for visual validation
- Use `uno_app_get_element_datacontext` for values
- Check the visual tree for presence and absence

### Cleanup
- Call `uno_app_close` at the end of tests (desktop), and before any rebuild
- Capture a screenshot and a `detail: "full"` tree on failure before closing

## Troubleshooting

### "No connected app instance" or a start timeout
- The app is still launching: poll `uno_app_get_runtime_info` every few seconds
- Call `uno_devserver_diagnostics` for the resolved port and the last launch outcome
- Read the `stdoutFile` for startup exceptions
- Do not call `uno_app_start` again until you know the process has died

### Screenshots show the Hot Design designer, or clicks do nothing, while the tree looks normal
- Hot Design opened over the app. Set `MainWindow.UseStudio(launchHotDesignOnStart: false);` in `App.xaml.cs` (in an existing app, only after the developer agrees; otherwise ask them to exit Hot Design in the app window), `uno_app_close`, and start the app again

### Build fails with MSB3026/MSB3027 (file in use)
- The previous instance is still running: `uno_app_close`, then build again

### Screenshot "path must be within the current working directory" or "Could not find a part of the path"
- Save inside the workspace directory, into a folder that exists, or omit `path`

### Element not found in visual tree
- It may be collapsed (try `includeHidden: true`)
- It may be created dynamically (refresh the tree)
- It may be a framework node (try `detail: "full"`)

### Interaction not working
- Verify the handle comes from the latest snapshot
- Check the pattern letters: the element may not support the action you chose
- Keyboard tools need a focused element; click or Tab to it first, or use `setValue`
- Try coordinate-based input as a last resort

### Screenshots are blank or incorrect
- Ensure the app window is visible and not minimized
- Verify the app has finished rendering

## References

- [INTERACTION-PATTERNS.md](INTERACTION-PATTERNS.md) - Detailed interaction examples
- [VISUAL-TREE-GUIDE.md](VISUAL-TREE-GUIDE.md) - Reading the snapshot
- [assertions.md](assertions.md) - Assertion and wait patterns

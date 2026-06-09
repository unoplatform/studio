# Interaction Patterns Reference

This document provides detailed examples of common UI interaction patterns using the Uno App MCP tools.

## Button Interactions

### Clicking a Button by Handle

The most reliable way to click a button:

1. Get the visual tree:
   ```
   uno_app_visualtree_snapshot(justMyCode: true, includeBounds: false, includeHidden: false)
   ```

2. Parse the XML to find the button element. Example output:
   ```xml
   <Button x:Name="SubmitButton" handle="btn_001" Content="Submit" />
   ```

3. Invoke the default action:
   ```
   uno_app_element_peer_default_action(elementRef: "btn_001")
   ```

### Double-Click Scenarios

For elements requiring double-click, use pointer click:

```
uno_app_pointer_click(x: 250, y: 100, button: "left", clickCount: 2, delayBetweenPresseAndReleaseInMs: 50)
```

## Text Input

### Entering Text in a TextBox

1. Find the TextBox in the visual tree
2. Focus the element:
   ```
   uno_app_element_peer_default_action(elementRef: "textbox_handle")
   ```
3. Type the text:
   ```
   uno_app_type_text(text: "Hello World", intervalInMs: 50)
   ```

### Clearing a TextBox

1. Focus the TextBox
2. Select all text:
   ```
   uno_app_key_press(virtualKey: "A", virtualKeyModifiers: "control")
   ```
3. Delete:
   ```
   uno_app_key_press(virtualKey: "Delete")
   ```

### Entering Special Characters

Use `unicodeKey` for characters not easily represented by virtual keys:

```
uno_app_key_press(virtualKey: "None", unicodeKey: "€")
```

## Keyboard Navigation

### Tab Navigation

Move focus between elements:
```
uno_app_key_press(virtualKey: "Tab")
```

Move backwards:
```
uno_app_key_press(virtualKey: "Tab", virtualKeyModifiers: "shift")
```

### Enter to Submit

Press Enter on focused button or form:
```
uno_app_key_press(virtualKey: "Enter")
```

### Escape to Cancel

Close dialogs or cancel operations:
```
uno_app_key_press(virtualKey: "Escape")
```

## List and Selection Interactions

### Selecting an Item in a ListView

1. Get the visual tree to find list items
2. Use default action on the item:
   ```
   uno_app_element_peer_default_action(elementRef: "listitem_handle")
   ```

### Multi-Select with Ctrl+Click

For lists that support multi-selection:

1. Click first item normally
2. For additional items:
   ```
   uno_app_pointer_click(x: 150, y: 200, button: "left")
   ```

### Keyboard List Navigation

Navigate in lists using arrow keys:
```
uno_app_key_press(virtualKey: "Down")
uno_app_key_press(virtualKey: "Up")
```

## ComboBox/Dropdown Interactions

### Opening a ComboBox

```
uno_app_element_peer_default_action(elementRef: "combobox_handle")
```

### Selecting an Item

After opening:
1. Use arrow keys to navigate:
   ```
   uno_app_key_press(virtualKey: "Down")
   ```
2. Press Enter to select:
   ```
   uno_app_key_press(virtualKey: "Enter")
   ```

## Checkbox and ToggleSwitch

### Toggling a Checkbox

```
uno_app_element_peer_default_action(elementRef: "checkbox_handle")
```

### Verifying State

After toggling, get a new visual tree snapshot and check the element's state properties.

## Slider Interactions

### Using Keyboard

1. Focus the slider
2. Use arrow keys:
   ```
   uno_app_key_press(virtualKey: "Right")  // Increase
   uno_app_key_press(virtualKey: "Left")   // Decrease
   ```

### Using Page Keys

For larger jumps:
```
uno_app_key_press(virtualKey: "PageUp")
uno_app_key_press(virtualKey: "PageDown")
```

## Dialog Handling

### Detecting a Dialog

Get the visual tree and look for:
- `ContentDialog` elements
- Popup or flyout containers
- Modal overlay elements

### Dismissing Dialogs

Use the dialog's button handles or:
```
uno_app_key_press(virtualKey: "Escape")  // Cancel/Close
uno_app_key_press(virtualKey: "Enter")   // Accept/OK
```

## Scroll Interactions

### Scrolling with Mouse Wheel

Not directly supported; use keyboard alternatives:
```
uno_app_key_press(virtualKey: "PageDown")  // Scroll down
uno_app_key_press(virtualKey: "PageUp")    // Scroll up
uno_app_key_press(virtualKey: "Home")      // Scroll to top
uno_app_key_press(virtualKey: "End")       // Scroll to bottom
```

### Scrolling to Element

If an element is off-screen, focusing it may scroll it into view:
```
uno_app_element_peer_default_action(elementRef: "offscreen_element_handle")
```

## Context Menu Interactions

### Opening Context Menu

```
uno_app_pointer_click(x: 200, y: 150, button: "right", clickCount: 1)
```

### Selecting Menu Item

After opening, get the visual tree to find menu items and use default action.

## Drag and Drop

### Basic Drag Operation

Drag operations typically require:
1. Mouse down at source
2. Move to destination
3. Mouse up

This pattern is complex and may require coordinate-based interaction with careful timing. Consider testing drag-and-drop logic through unit tests where possible.

## Timing and Synchronization

### Waiting for UI Updates

After actions that trigger async operations or animations:
1. Get a fresh visual tree snapshot
2. Check for expected state changes
3. Retry if needed with brief delays

### Handling Loading States

Look for loading indicators in the visual tree:
- Progress rings/bars
- Disabled states on interactive elements
- Loading text or placeholders

Wait until these are no longer present before continuing.

## Error Recovery

### Element Not Responding

1. Try refreshing the visual tree
2. Verify element is still visible and enabled
3. Try alternative interaction method (keyboard vs mouse)
4. Check if a dialog or overlay is blocking

### Unexpected State

1. Capture screenshot for debugging
2. Get visual tree for analysis
3. Consider restarting the app and test

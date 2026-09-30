# Verifying without the App MCP

For Release builds, CI, and devices the DevServer does not reach, the App MCP is not available. Drive the app through the operating system's accessibility tree instead: Uno builds it from the same automation peers the App MCP reads. Locate a control by its name, act on it, read the tree again, and assert on what it reports. Take a screenshot only for a visual assertion or as evidence, and save it to a file.

## What each target exposes

Checked on Uno.Sdk 6.8 (Skia renderer, `recommended` preset, Release build, `IsUiAutomationMappingEnabled` not set):

| Target | Tool | `AutomationProperties.Name` (or the content text) | `AutomationProperties.AutomationId` |
|--------|------|------|------|
| Windows (`net10.0-desktop`) | UI Automation | `Name` | `AutomationId`; falls back to `x:Name` |
| Android (Skia) | `uiautomator dump` | `content-desc` | not exposed, even with `IsUiAutomationMappingEnabled` ([unoplatform/uno#23934](https://github.com/unoplatform/uno/issues/23934)) |
| macOS (Skia) | AX API | not exposed to automation clients ([unoplatform/uno#24669](https://github.com/unoplatform/uno/issues/24669)) | not exposed |

A `TextBlock` shows its current text as its name on both platforms, so a label, a counter, or a validation message can be asserted from the tree. For other targets (iOS, WebAssembly, Linux), use the App MCP on a Debug build; this path has not been verified there.

So on Android, a control is found only by its name: give each control a test touches an `AutomationProperties.Name` that is unique on its page. The name is what screen readers announce, so make it a readable label ("Save inspection"), not an ID.

## Windows: UI Automation

PowerShell 7 on Windows loads UI Automation with no install:

```powershell
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
$A = [System.Windows.Automation.AutomationElement]; $Scope = [System.Windows.Automation.TreeScope]
$Cond = [System.Windows.Automation.PropertyCondition]
$app = Start-Process $exe -PassThru      # bin/Release/net10.0-desktop/<App>.exe
# Poll until the window exists (FindFirst returns $null until then).
$win = $A::RootElement.FindFirst($Scope::Children, $Cond::new($A::ProcessIdProperty, $app.Id))
function Find($id) { $win.FindFirst($Scope::Descendants, $Cond::new($A::AutomationIdProperty, $id)) }

(Find 'SaveButton').GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
(Find 'NotesBox').GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('Checked')
(Find 'AgreeCheck').GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern).Toggle()
(Find 'StatusText').Current.Name        # assert: current text
(Find 'SaveButton').Current.IsEnabled   # assert: enabled state
```

- Find by `AutomationIdProperty`, or by `NameProperty` for controls with no ID.
- Act through patterns: `Invoke` (buttons), `Value.SetValue` (text boxes; no focus or typing needed), `Toggle` (`CheckBox`, and `ToggleSwitch`, which appears as a `Button`). Read `ToggleState` back from the same pattern.
- A find returns `$null` until the UI has updated: poll it, like the App MCP wait loop, before asserting.
- `Stop-Process` the app when done.

## Android: uiautomator

```bash
adb shell uiautomator dump /sdcard/ui.xml && adb exec-out cat /sdcard/ui.xml > ui.xml
```

- Each control is a `<node>`: `class` gives the role (`android.widget.Button`, `EditText`, `CheckBox`; a `TextBlock` is an `android.view.View`), `content-desc` the name, `checkable`/`checked` the toggle state, and `bounds="[x1,y1][x2,y2]"` its rectangle in physical pixels.
- Tap the centre of the bounds: `adb shell input tap <(x1+x2)/2> <(y1+y2)/2>`. `input tap` also takes physical pixels, so do not convert to dp.
- Type into an `EditText`: tap it, then `adb shell input text "Ada"` (`%s` for a space).
- Dump again after every action: bounds move when layout changes, and assertions read the new dump.
- The `text` attribute stays empty, even on an `EditText` after typing: assert a field's value through the label or message that displays it, or a screenshot.
- `ERROR: null root node` means the UI is still starting or animating: wait a second and dump again.
- In Git Bash, set `MSYS_NO_PATHCONV=1`, or `/sdcard/...` is rewritten into a Windows path.

## Screenshots

Save them to files for the report (`adb exec-out screencap -p > step3.png`, or a window capture on Windows) and open one only when the assertion is visual: layout, colour, clipping, theme. An image read back into the conversation stays in context for every later call.

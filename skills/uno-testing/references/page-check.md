# Page check

A pass over every screen of a desktop app, in each language and window width, that measures what a screenshot only shows: text cut off, overlaps, controls with no name, resource keys on screen. Two files in this skill's `assets/`: `PageCheck.cs` goes into the app and measures; `page-check.cs` builds the app and runs it once per language and width. It needs no App MCP, only a Debug `net10.0-desktop` build.

## What it does, and what it does not

It opens the first screen, then every item of the shell's `NavigationView` (menu, footer and settings items) or Toolkit `TabBar`, then the tabs a section shows, and measures each. It does not open a record, a dialog, a picker or a menu, type, scroll, or force a list's empty, no-match, loading and error states: your own check scripts do that, and ask for a measure at each step (*Your own steps*). It measures text, buttons, inputs and scrolling; it does not see a cut-off image, box or placeholder, a colour or a contrast: read the PNGs for those. A shell built on anything else (`SelectorBar`, `TabView`, `Pivot`, your own buttons) is reached from your own steps.

## Add it to the app

1. Copy `assets/PageCheck.cs` into the app project folder.
2. In `App.xaml.cs`, at the end of `OnLaunched` (after `InitializeNavigationAsync`, or after `MainWindow.Activate()`), add `_ = PageCheck.RunIfAskedAsync(MainWindow);`.
3. Copy `assets/page-check.cs` to `<Solution>/tests/ui/page-check.cs`. **Never put it in a project folder**: a `.cs` file there compiles into the app.

The line does nothing unless the `PAGECHECK` environment variable is set, and outside a Debug desktop build the file compiles to an empty stub, so both can stay in the app.

## Run it

```bash
dotnet run tests/ui/page-check.cs -- <App>/<App>.csproj --widths open,400 --env <the app's sample-data switch>
```

- **Sample data only.** Pass the app's own switch with `--env NAME=value` (repeatable), so no pass signs in or writes to a live service.
- **Languages** default to the app's own (`LocalizationConfiguration:Cultures` in `appsettings.json`, else its `.resw` folders); `--languages en,fr` names them. The check saves the language through Uno.Extensions' `ILocalizationService` when the app hosts it, else `ApplicationLanguages.PrimaryLanguageOverride`, starts the app again, and puts the first language back at the end. An app that keeps its language its own way reads it from a variable: `--env MYAPP_LANGUAGE={language}`.
- **Widths** default to `1280,400`; `open` is the size the app opens at. `--env PAGECHECK_HEIGHT=1100` gives a taller window, which shows more of a long page in each PNG.
- **One instance.** The script builds and starts the app itself: close the one the App MCP started first (`uno_app_close`). `--timeout` (300 s) stops a pass that hangs, by killing the process it started.
- **Linux** needs an X display in `DISPLAY`; with none, the script says so and stops. Start one yourself (`Xvfb :97 -screen 0 1920x1200x24 &`) and stop it afterwards.

It prints one line per pass (`fr-400  finished  steps=7 findings=16 exceptions=0  <folder>`) and a total by kind, and exits non-zero when a pass did not finish. Each pass folder holds a PNG per step, `findings.txt`, `texts.txt`, `steps.txt` and `app.log`; `<out>/findings.txt` has every pass.

## Your own steps

From a check script (`without-app-mcp.md`), start the app with `PAGECHECK=<folder>` and `PAGECHECK_STAY=1`: after the sections it stays open. Bring the app to a state, wait for it, then write a step name into the file `<folder>/snap` (`echo accounts-empty > <folder>/snap`): the screen is measured as it is, open dialogs and flyouts included, and the file is removed when the step is done. Create `<folder>/end` to finish. From code, `await PageCheck.SnapAsync("accounts-empty")` does the same, and `PageCheck.RunIfAskedAsync(MainWindow, WalkAsync)` runs your walk after the sections.

## Read the findings

One line per finding, tab-separated: pass, step, kind, detail. Each problem is listed once per pass, at the first step that showed it; open that step's PNG beside it.

| Kind | What it means | Usual fix |
|------|---------------|-----------|
| `raw-key` | A resource key or a format hole (`{0}`) shown as text | Add the key to every language's `.resw`; format the string |
| `clipped` | One line with no ellipsis that needs more width than it has | Let it wrap, give it `TextTrimming`, or give it the room |
| `cut-right`, `cut-bottom` | The text sits in something given less room than it takes (the detail names it), so part of it is not drawn | A `*` column where an `Auto` one or a horizontal `StackPanel` let the row grow; in a narrow window, stack the row |
| `off-screen` | Text past the window's right edge, with nothing that scrolls it into view | The page's narrow layout |
| `overlap-button`, `overlap-text` | Two buttons or two texts drawn over each other | Their own rows or columns; no negative margin or fixed position |
| `h-scroll`, `too-wide` | A page that scrolls sideways, or is laid out wider than the window | Remove the fixed `Width` or `MinWidth` that forces it |
| `unnamed-input` | An input with no `Header`, `AutomationProperties.Name` or `LabeledBy`; a placeholder is not a name | Name it, from the string resources |
| `unnamed-button` | A button with no text and no `AutomationProperties.Name` | Name it, from the string resources |
| `exception` | An exception nothing handled, after the step named | Fix its cause |
| `check` | The check could not do what was asked: the width or the language differs, no section was found, the pass stopped | Read the detail; that pass proves less than asked |

- Read `texts.txt` of each language once: a string left in another language, a date or an amount in the wrong format, show there and nowhere else.
- **A finding is a defect.** Fix it and run the check again, or list it in the report with the reason it stays. Never drop one silently; one you believe is false is reported too, with what its PNG shows.
- No finding is not a pass for the screens the check never opened: say which states your own steps covered.

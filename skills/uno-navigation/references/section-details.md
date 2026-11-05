# Details Pages Inside a Shell Section

In a TabBar or NavigationView shell, the Visibility region shows each section (a route nested under `Main`) in its own `FrameView`: a `Frame` it creates for that section. A details or record page opened from a section belongs on that section's Frame, above its list, so the tab or sidebar item stays selected and back returns to the list.

## Register the details page beside its section, with `DependsOn`

```csharp
new RouteMap("Main", View: views.FindByViewModel<MainModel>(), IsDefault: true, Nested:
[
    new RouteMap("Overview", View: views.FindByViewModel<OverviewModel>(), IsDefault: true),
    new RouteMap("Contacts", View: views.FindByViewModel<ContactsModel>()),
    new RouteMap("ContactRecord", View: views.FindByViewModel<ContactRecordModel>(), DependsOn: "Contacts"),
])
```

`DependsOn: "Contacts"` puts the section under the record on the Frame (`Contacts/ContactRecord`), whoever opens it.

**Never nest the details route under the section** (`Nested:` on `Contacts`). A nested route means "a child region inside `ContactsPage`", and the page has none:

- Opened from the section, the section's Frame refuses a route whose parent is not its current page's parent. The request bubbles to the Visibility region, which accepts any route with a view and shows the record as a new sibling section with an empty back stack. The record renders, but `-` and `NavigateBackAsync` reach the root and do nothing. Log: `FrameNavigator Request: -`, then `PanelVisiblityNavigator Request: -`, `NavigationViewNavigator Request: -`.
- Opened from the shell (`./Contacts/ContactRecord`), the Frame shows the section and parks the rest for a child region that never attaches: `Parking navigation '…' … no child regions attached`.

## Open it

| From | Call | Result |
|------|------|--------|
| The section's own Model | `NavigateRouteAsync(this, "ContactRecord", data: contact)` | pushed on the section's Frame |
| A Model in another section | the same call | the shell selects Contacts and pushes the record on its Frame |
| The shell's Model (search, notifications, a sidebar "New" button) | `NavigateRouteAsync(this, "./ContactRecord", data: contact)` | the same; without `./` the shell's Frame replaces the whole shell |

No service that hands requests to the section's Model is needed.

## Go back

`uen:Navigation.Request="-"` on the record's Back button, or `await navigator.NavigateBackAsync(this)` in its Model (after Delete). The Frame pops the record and the list shows again; no fallback route is needed.

## Switch to another section from a section

A section's own navigator cannot select another section. From the Overview Model, `NavigateRouteAsync(this, "Opportunities")`, `"../Opportunities"` and `"-/Opportunities"` all show the Opportunities page, but the sidebar stays on Overview and clicking Overview then does nothing. `"./Opportunities"` and `"/Main/Opportunities"` show nothing.

Send it through the shell's navigator, with `./`, the same call as a shell button. The shell's Model receives that navigator; keep it in a singleton that sections inject:

```csharp
public class ShellNavigation
{
    private INavigator? _shell;
    public void Attach(INavigator shell) => _shell = shell;   // from MainModel's constructor
    public async Task NavigateToSectionAsync(object sender, string section, object? data = null)
    {
        if (_shell is not null) await _shell.NavigateRouteAsync(sender, "./" + section, data: data);
    }
}
```

The section then calls `await shell.NavigateToSectionAsync(this, "Opportunities")`. Opening a details page in another section does not need this: the call in the table above selects its section.

## Keep the content region a Visibility `Grid`

This recipe needs one Frame per section. With a single `Frame` as the shell's content region, a section already visited does not show again unless its item uses `-/` ([uno.extensions#3221](https://github.com/unoplatform/uno.extensions/issues/3221)).

Checked on Uno.Extensions 8.0.0-dev.250 with a NavigationView shell, the section switch included (sidebar selection read through UI Automation). The framework's Commerce test app uses the same shape in its TabBar and NavigationView shells (`DependsOn` details routes, `-` on the details page).

## Related Skills

- `references/routes.md`: route registration
- `references/navigationview.md`, `references/tabbar.md`: the shells
- `references/troubleshooting.md`: back does nothing, request parked

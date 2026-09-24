# Recipes

A recipe is a small complete implementation of one user-visible behaviour: view, presentation state, service, registration, navigation, failure handling and tests. Recipes exist because the defects that hurt real apps are in the wiring a snippet leaves out: a setting that never saves, a button with no command, a detail page bound to a stale copy of its data.

**Status.** The reference implementations are being built as features of the [Uno Chefs](https://github.com/unoplatform/uno.chefs) sample app and are not published yet. Until they are, use the contracts below as the acceptance checks for a matching feature: they tell you what "working" means, whatever code you write.

## Catalogue

### `persisted-settings`

Changing a preference updates the display and persists through the app's settings service. A new session restores it. A failed save leaves a clear, recoverable state. Rapid edits cannot let an older save win.

Accept when: the documented default shows on first launch with no write during loading; a toggle reaches the service and the view settles on the saved value; the value survives closing and relaunching the app; a forced write failure shows an unsaved or error state and retry recovers; rapid false, true, false ends with false both displayed and stored; navigating away and back creates no duplicate subscription or write; keyboard activation behaves like a click.

### `list-detail-mutation`

A list opens a detail page. Changing an item (for example, marking it favourite) updates the detail page and every list that shows the item. A navigation payload identifies or seeds the item; it is not the long-lived source of its state.

Accept when: the detail page updates from live state after the change; the list reflects it on return without a restart; another view showing the same item updates; a filtered list (such as favourites only) gains or loses the item and its count updates; a failed change shows no false confirmed state; two similar items are told apart by identity; revisiting the detail page does not multiply handlers; the UI uses the entity the service returned, not the request.

### `search-filter-results`

Query and filter changes update results consistently. Loading, empty, error and retry are visible. A slow older request cannot overwrite results for a newer query.

Accept when: matches and count agree; no-match shows an empty state, not stale results; retry after an error reruns the current query; a slow query A followed by a fast query B never shows A's results; cancelling a filter edit leaves applied results unchanged; clear restores defaults; leaving mid-request causes no error; retrying the same query after a failure is not suppressed as "unchanged".

### `actionable-card`

A card opens its item. A secondary action on the card (favourite, vote) acts on the same item without navigating. Both work by keyboard and assistive technology.

Accept when: activating the card navigates once, to the right item; the secondary action mutates once and does not navigate; keyboard focus order and activation match pointer behaviour; the secondary action's state updates in the rendered card; disabled and pending states agree with behaviour; a recycled card acts on its current item, not a previous one; actions are visible and unclipped in light, dark, narrow and wide.

### `theme-and-brand`

The app keeps its brand while rendering readable, consistent controls in each supported theme and window size. Theme switching goes through the existing theme service.

Accept when: body, action and secondary text are readable on their actual surfaces in light; switching to dark updates without a restart; the system theme is followed where supported; an explicit choice survives a relaunch; nothing clips at narrow or wide sizes; custom cards and overlays pair foreground and background deliberately; a legacy theme keeps its initialisation; the XAML uses only semantic keys, so swapping Material and Simple still resolves every key. See [design.md](design.md).

### `dialog-result`

A page opens a modal editor or filter. Apply returns a typed result and updates the caller once. Cancel or dismiss commits nothing.

Accept when: the dialog opens with the current values as a draft; apply updates the caller exactly once; cancel leaves the caller unchanged; back, Escape or tapping outside behave as documented cancel; an invalid draft keeps the dialog open with an accessible error; reopening starts from committed values; rapid repeated opening does not stack dialogs; keyboard focus stays in the dialog and returns to the caller.

## Choosing and adapting

1. Match the requested behaviour to a contract above. Most real features combine two or three.
2. Compare the recipe's architecture with what discovery found: MVUX or MVVM, navigation style, theme family.
3. Prefer the approach that needs no new dependency and no architectural change. In an existing app, keep its architecture and apply the same contract (see [existing-app.md](existing-app.md)).
4. When a published recipe fits, read its guide, source and tests before copying. Rename namespaces with ordinary edits and build the result. The copied app must not reference this plugin at runtime.
5. Adapting outside a recipe's tested versions or architecture is allowed. Label it as uncertified and verify it locally.

Record the chosen recipe, why it fits and what you adapted in the checkpoint.

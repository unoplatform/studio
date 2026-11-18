# Checking Previews in a Running App

Authoring a preview needs nothing running (see `references/previews.md`), but when the app is running, check the previews in it.

To see the result, run the app, open Hot Design, and pick **Previews** in its left bar — the preview sits under its control type, or under **Data Templates**.

An agent with the Uno app MCP can check the same way through `uno_execute_tool`. **Start the app before writing the previews**, once every package they need is referenced, then write every page's previews and wait about 10 seconds, once, before checking them: Hot Reload adds new preview files to the running app, but a package added later needs a restart. Don't build while the app runs: it locks the build output, and Hot Reload compiles each file as it is saved.

1. Call `app_hotdesign_set_mode` with `in_app`. The designer takes a moment to start and answers "not ready" until it has — retry every few seconds.
2. Call `app_hotdesign_set_app_mode` with `previews`. Once the previews are written, read the catalogue with `app_read_resource` on `hotdesign://previews`. It is large, so search it for the page names instead of reading it whole. A group that is missing has not been added by Hot Reload yet: wait a few seconds and read it again.
3. Screenshot **one preview per page: its Loaded state**. It shows the most, and a mock that was not applied shows there as an error; an Error preview hides one, because the real feed fails too. The other states go through the same mock, so don't screenshot each of them: the catalogue from step 2 already shows they exist.
4. Open the preview with `app_hotdesign_select_preview`, wait 2–3 seconds for it to render, then take a window screenshot. An earlier one can show a half-rendered page. Check that the Properties panel names the preview and page you asked for: the selection sometimes lands on another one, so select it again if it did. The first time the designer opens in an app, an introduction dialog covers the canvas — make sure it is dismissed before judging what the preview shows. Don't rely on `app_hotdesign_screenshot_preview` for this: it renders off-screen without the `LoadDataContext()` data, so a mocked page looks empty there even when the preview is correct.

## Related Skills

- `references/previews.md` — Write the previews this checks
- `uno-testing` — Start the app and drive the Uno App MCP

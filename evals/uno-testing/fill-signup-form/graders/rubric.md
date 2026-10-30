---
type: llm
---
The reply gives a sequence of Uno App MCP (`uno_app_*`) calls to fill and submit a form in a running Uno Platform app.
Uno App MCP facts the judge needs: `uno_app_element_peer_default_action` always runs `invoke`, so it works only on invokable elements (`Button`, `HyperlinkButton`); a `TextBox` takes `uno_app_element_peer_action(action: "setValue")` or `uno_app_type_text(text, automationId)`; a `CheckBox` takes `action: "toggle"`; a `ComboBox` takes `action: "expand"` and an item is then chosen with the keyboard (`uno_app_key_press` Down/Enter) or a click. Element tools accept `automationId` or `name` selectors directly. The check after the action is `uno_app_find_elements(text: "Welcome!" or automationId, waitForMs / until)`, which returns `{checked}`/`{disabled}`/`value=` states.
PASS if all hold:
- Each control gets an action that fits it: setValue or type_text for the TextBox, toggle for the CheckBox, expand plus keyboard/click selection for the ComboBox, the invoke default action only for the Button.
- Success is confirmed with `uno_app_find_elements` waiting on the "Welcome!" text (or its automation id), not by a screenshot alone.
- Elements are addressed by `automationId`/`name` selectors (or snapshot handles), not by pixel coordinates as the first choice.
FAIL if it uses the default (invoke) action on the TextBox, CheckBox, or ComboBox, relies on coordinates as the primary method, or describes test results as if it had run them.

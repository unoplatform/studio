---
type: llm
---
The reply gives a sequence of Uno App MCP (`uno_app_*`) calls to fill and submit a form in a running Uno Platform app.
Facts the judge needs: `uno_app_element_peer_default_action` always runs `invoke`, so it works only on invokable elements (`Button`, `HyperlinkButton`); a `TextBox` takes `uno_app_element_peer_action(action: "setValue")` or `uno_app_type_text(text, automationId)`; a `CheckBox` takes `action: "toggle"`; a `ComboBox` takes `action: "expand"` and an item is then chosen with the keyboard (`uno_app_key_press` Down/Enter) or a click.
PASS if the ComboBox is opened with the `expand` peer action (or a click) and an item is chosen with the keyboard or a click.
FAIL if it uses the default (invoke) action on the ComboBox, or sets its value without opening it.

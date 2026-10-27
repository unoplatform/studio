---
type: llm
---
The reply gives a ListView ItemTemplate in an Uno Platform (Material) app where each recipe renders as a card with rounded corners, a shadow, a photo, a title and a cook time.
PASS if all hold:
- The card is Uno Toolkit's `utu:CardContentControl` with a card style such as `ElevatedCardContentControlStyle` (or `FilledCardContentControlStyle`/`OutlinedCardContentControlStyle`), which supplies the corner radius, elevation and theming.
- If the card's layout is in a `ContentTemplate`, the `CardContentControl` sets `Content="{Binding}"` (or `{x:Bind}`) so the inner template receives the recipe; if the layout is direct content of the card, item bindings resolve against the recipe.
- It does not build the card from a `Border` with `CornerRadius` plus `Background`/`ThemeShadow`/`Translation`, and does not use the slot-based `utu:Card` control. A `Border` used only to clip the photo's corners is fine.
FAIL if the card surface is a hand-made rounded `Border` (with or without `ThemeShadow`), a `Grid` with a shadow, or `utu:Card`.

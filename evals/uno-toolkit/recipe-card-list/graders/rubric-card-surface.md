---
type: llm
---
The reply gives a ListView ItemTemplate in an Uno Platform (Material) app where each recipe renders as a card with rounded corners, a shadow, a photo, a title and a cook time.
Facts the judge needs: Uno Toolkit's `utu:CardContentControl` with a card style supplies the corner radius, elevation and theming.
PASS if the card surface is the `CardContentControl` (a `Border` used only to clip the photo's corners is fine).
FAIL if the card surface is a hand-made rounded `Border` (with or without `ThemeShadow`), a `Grid` with a shadow, or the slot-based `utu:Card`.

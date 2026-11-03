---
type: llm
---
The reply gives a ListView ItemTemplate in an Uno Platform (Material) app where each recipe renders as a card with rounded corners, a shadow, a photo, a title and a cook time.
PASS if the card's layout receives the recipe: either the layout is in a `ContentTemplate` and the `CardContentControl` sets `Content="{Binding}"` (or `{x:Bind}`), or the layout is the card's direct content, where item bindings resolve against the recipe.
FAIL if the layout is in a `ContentTemplate` and the card's `Content` is not bound, so the template has no recipe.

---
type: llm
---
The reply puts a LiveCharts2 chart inside a card on an Uno Platform page.
Facts the judge needs: a LiveCharts2 chart has no intrinsic size. In an `Auto` row it collapses; inside a container that sizes to its content (an unstretched `CardContentControl`, a `Border` aligned left) it takes the width of its sibling text. A fixed or `*` row height, plus a card stretched horizontally (`HorizontalAlignment="Stretch"` and `HorizontalContentAlignment="Stretch"` on a `CardContentControl`), gives it a definite size.
PASS if the chart gets a definite size in both directions inside the card.
FAIL if the chart sits in an `Auto`-height row or `StackPanel` with no height of its own, or inside a card left at its default alignment.

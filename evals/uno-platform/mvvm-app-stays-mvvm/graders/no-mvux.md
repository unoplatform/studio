---
type: regex
pattern: '\b(?:IListFeed|IListState|IFeed|IState)\s*<[^;{}()\n]*>\s+\w+\s*(?:=>|[{;=])|<mvux:FeedView|partial\s+record\s+\w+Model\b'
match: not_contains
---

---
type: regex
pattern: 'ErrorsChanged\s*\+=|(?:class|record)\s+\w+[^{\n]*:\s*[^{\n]*\bINotifyDataErrorInfo\b'
match: not_contains
---

---
type: regex
pattern: 'new\s+ObservableCollection\s*<|ObservableCollection\s*<[^>]+>\s+\w+\s*(?:=>|[{;=])|[:,]\s*INotifyPropertyChanged\b'
match: not_contains
---

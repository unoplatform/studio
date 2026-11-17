---
type: regex
pattern: 'DefaultRequestHeaders\.Authorization\s*=|\.Headers\.Authorization\s*=|Headers\.(?:Add|TryAddWithoutValidation)\s*\(\s*"Authorization"|new\s+AuthenticationHeaderValue\s*\(|new\s+BaseBearerTokenAuthenticationProvider\s*\(|class\s+\w+\s*(?:\([^)]*\))?\s*:\s*DelegatingHandler'
match: not_contains
---

---
type: llm
---
The reply explains how an agent should confirm a fix in a running Uno Platform app driven by the Uno App MCP, after editing the page model's C# and the page's XAML.
Facts the judge needs: the re-check is `uno_app_find_elements(automationId: "<save button>", until: "enabled")`.
PASS if it re-runs the failed assertion in the app (the Save button enabled, via `uno_app_find_elements` with `until`/`waitForMs`), re-entering valid input if the page was recreated.
FAIL if it declares the fix confirmed from the code alone.

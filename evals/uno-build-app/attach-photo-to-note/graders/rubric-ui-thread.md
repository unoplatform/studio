---
type: llm
---
The reply implements attaching a picked photo to a note in an Uno Platform MVUX app that ships Windows App SDK, desktop, Android, iOS and WebAssembly heads.
Facts the judge needs: MVUX commands run on a background thread, so a picker shown from a Model command must be dispatched to the UI thread (for example a service that uses the window's `DispatcherQueue`); calling the picker from a UI-thread event handler and passing the result in is also correct.
PASS if the picker runs on the UI thread, either through a dispatcher-aware service injected into the Model or from a view event handler.
FAIL if the picker is called directly inside a Model command body with no dispatching.

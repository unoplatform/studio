---
type: llm
---
The reply implements attaching a picked photo to a note in an Uno Platform MVUX app that ships Windows App SDK, desktop, Android, iOS and WebAssembly heads.
Facts the judge needs: a picked file's path is not durable (WebAssembly and mobile give temporary or sandboxed access), so the file must be copied into app storage and the note must keep that copy.
PASS if the picked file is copied into app-owned storage (for example `ApplicationData.Current.LocalFolder`) and the note persists a reference to that copy.
FAIL if it stores only the picker's original path, or keeps a `StorageFile` reference in memory only.

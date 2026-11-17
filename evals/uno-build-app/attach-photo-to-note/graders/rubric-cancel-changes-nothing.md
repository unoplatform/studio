---
type: llm
---
The reply implements attaching a picked photo to a note in an Uno Platform MVUX app that ships Windows App SDK, desktop, Android, iOS and WebAssembly heads.
PASS if cancelling the picker (a null file) leaves the note unchanged.
FAIL if a cancelled pick is not handled, or it clears or overwrites the note's photo.

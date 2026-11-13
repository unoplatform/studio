---
type: llm
---
The reply implements attaching a picked photo to a note in an Uno Platform MVUX app that ships Windows App SDK, desktop, Android, iOS and WebAssembly heads.
Facts the judge needs: `Windows.Storage.Pickers.FileOpenPicker` works on all Uno targets.
PASS if it picks the image with the shared `FileOpenPicker` for every head.
FAIL if it writes a platform-specific picker per head (Android intents, iOS `PHPickerViewController`, a JavaScript file input) where the shared `FileOpenPicker` covers them.

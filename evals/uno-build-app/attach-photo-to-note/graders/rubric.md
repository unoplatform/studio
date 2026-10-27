---
type: llm
---
The reply implements attaching a picked photo to a note in an Uno Platform MVUX app that ships Windows App SDK, desktop, Android, iOS and WebAssembly heads.
Facts the judge needs: `Windows.Storage.Pickers.FileOpenPicker` works on all Uno targets. It needs at least one `FileTypeFilter` entry; on the Windows App SDK head it must be initialized with the window handle (`WinRT.Interop.InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window))`) or it throws. MVUX commands run on a background thread, so a picker shown from a Model command must be dispatched to the UI thread (for example a service that uses the window's `DispatcherQueue`); calling the picker from a UI-thread event handler and passing the result in is also correct. A picked file's path is not durable (WebAssembly and mobile give temporary or sandboxed access), so the file must be copied into app storage and the note must keep that copy.
PASS if all hold:
- The picker sets a `FileTypeFilter` and handles the Windows App SDK window initialization (an `#if WINDOWS`-style guard is fine).
- The picker runs on the UI thread: either through a dispatcher-aware service injected into the Model, or from a view event handler; it is not called directly inside a Model command body with no dispatching.
- The picked file is copied into app-owned storage (for example `ApplicationData.Current.LocalFolder`) and the note persists a reference to that copy, not to the picker's original path; cancelling the picker changes nothing.
- File access lives in a service, not in the page's code-behind or the Model doing raw I/O inline.
FAIL if it stores only the original file path or a `StorageFile` reference in memory, ignores Windows window initialization, or uses a platform-specific picker per head when the shared `FileOpenPicker` covers them.

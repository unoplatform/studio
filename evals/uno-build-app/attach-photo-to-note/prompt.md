---
description: Attaching a picked photo to a record works on every head; the picker is configured and window-initialized, runs on the UI thread, and the file is copied into app storage.
tags: [uno-build-app, attach-file]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
I'm adding a feature to our Uno Platform notes app (MVUX, Uno.Extensions navigation; heads: Windows App SDK, Skia desktop, Android, iOS and WebAssembly). Users should be able to attach a photo to a note: tap "Attach photo" (bound to an `AttachPhoto` method on the note page's model), pick an image, and the photo shows on the note, including the next time they open the app. How should I implement this? Show the code.

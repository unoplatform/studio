---
description: Tests written inside the app turn on the HotTesting feature instead of adding packages, live in the Tests folder so the Release build still compiles, and use the control and app test attributes.
tags: [uno-testing, hot-testing]
runs: 3
max_turns: 12
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
My Uno Platform app (Uno.Sdk 7.1, `<UnoFeatures>Material;Toolkit;MVUX;Navigation</UnoFeatures>`) has a `PriceTag` user control that shows "Free" when its `Price` is 0, and a `MainPage` with a `TextBlock` named `WelcomeText`. I want tests that live in the app itself and run inside it while I work: one that checks `PriceTag` on its own, and one that starts the app and checks the welcome text is shown. Show me the project change and the test code, and where each file goes.

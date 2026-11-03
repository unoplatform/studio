---
description: The -tests unit template's icuuc77 failure is fixed by turning off the ICU data initializer generator for the app's net10.0 target, with the property made compiler-visible.
tags: [uno-kiota, testing]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
Our Uno Platform app was created with `dotnet new unoapp -preset recommended -server -tests unit` on Uno.Sdk 7.0.0-dev.115. I want service tests for our Kiota client in the unit test project, but even the test the template generated fails as soon as it touches a type from the app project: `System.TypeInitializationException ... Failed to load 'icuuc77'`, which comes from the generated `__IcuDataInitializer`. How do I fix it?

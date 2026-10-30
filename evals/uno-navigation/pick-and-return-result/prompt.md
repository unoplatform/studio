---
description: A page that lets the user pick a value returns it to the caller with the ForResult navigation overload and NavigateBackWithResultAsync.
tags: [uno-navigation, results]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
In my Uno Platform app (Uno.Extensions navigation, routes registered in App.xaml.cs) the "New shipment" page has a "Choose address" button. It should open an address picker page; when the user taps an address there, the picker closes and the new shipment page fills in the chosen `Address`. How do I get the selected address back to the first page? Show both sides plus the route registration.

---
description: Opening a details page for a tapped list item passes the entity through a DataViewMap route, received by the destination's constructor.
tags: [uno-navigation, data]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
My Uno Platform app was created from the recommended template (MVUX, Navigation and Toolkit in UnoFeatures). The Orders page shows a ListView of `Order` records. When the user taps an order I want to open an OrderDetailsPage that shows that specific order. How do I wire this up so the details page gets the order? Show the route registration, the XAML or code that triggers it, and the details model.

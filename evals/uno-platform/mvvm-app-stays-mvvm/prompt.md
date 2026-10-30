---
description: A new page in an existing MVVM Uno app keeps CommunityToolkit.Mvvm ViewModels instead of switching to MVUX.
tags: [uno-platform, mvvm]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
Our Uno Platform app uses CommunityToolkit.Mvvm: every page has a ViewModel deriving from ObservableObject. I need a new Customers page that loads the customer list from `ICustomerService.GetCustomersAsync()` when it opens and has a Refresh button. Write the view model and the page XAML.

---
description: A map of stores on an MVUX page uses Mapsui's Uno package, projects longitude/latitude, feeds the pins from the Model, and is not a WebView-embedded JavaScript map.
tags: [uno-maps, mapsui]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
I have an Uno Platform app from the recommended template (MVUX, Simple theme, Uno.Sdk 6.7.49 in global.json, Directory.Packages.props at the root, desktop and WebAssembly heads). The Stores page Model already has this:

```csharp
public partial record StoresModel(IStoreService Stores)
{
    public IListFeed<Store> All => ListFeed.Async(Stores.GetAllAsync);
}

public partial record Store(string Id, string Name, double Latitude, double Longitude);
```

I want the page to be a map with a pin per store, and tapping a pin shows the store's name above the map. Show me the package setup, the Model change, the XAML and any code it needs.
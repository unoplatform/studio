---
description: Mapsui 5.2.0 on Uno.Sdk 6.7 fails restore with NU1605 on SkiaSharp; the fix is the SDK's SkiaSharpVersion property or Mapsui 5.1.0, not a direct SkiaSharp package version or a suppressed warning.
tags: [uno-maps, mapsui]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
I added Mapsui to my Uno Platform app (Uno.Sdk 6.7.49 in global.json, Directory.Packages.props at the root) to show our field sites on a map:

```xml
<PackageVersion Include="Mapsui.Uno.WinUI" Version="5.2.0" />
```

Restore now fails:

```
error NU1605: Warning As Error: Detected package downgrade: SkiaSharp.Views.Uno.WinUI from 3.119.4 to 3.119.2. Reference the package directly from the project to select a different version.
 MyApp -> Mapsui.Uno.WinUI 5.2.0 -> SkiaSharp.Views.Uno.WinUI (>= 3.119.4)
 MyApp -> SkiaSharp.Views.Uno.WinUI (>= 3.119.2)
```

How do I fix it?
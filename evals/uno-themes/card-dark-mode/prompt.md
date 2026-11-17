---
description: Restyling a card with hard-coded colors and font sizes uses paired semantic brushes through ThemeResource and typography styles, so it works in dark mode.
tags: [uno-themes, semantic-brushes]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
My Uno Platform app (made from the recommended template) has this card on the home page and it looks broken when Windows is in dark mode: white box, nearly invisible text. Can you rewrite it so it looks right in both light and dark?

```xml
<Border Background="#FFFFFF" BorderBrush="#E0E0E0" BorderThickness="1" CornerRadius="12" Padding="16">
    <StackPanel Spacing="4">
        <TextBlock Text="Monthly report" FontSize="20" FontWeight="SemiBold" Foreground="#1C1B1F" />
        <TextBlock Text="Generated on Oct 3" FontSize="14" Foreground="#49454F" />
    </StackPanel>
</Border>
```

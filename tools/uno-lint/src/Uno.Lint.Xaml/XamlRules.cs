using System.Text.RegularExpressions;
using Uno.XamlParser.Stage0;
using Uno.XamlParser.Stage1;
using Uno.XamlParser.Stage1.Nodes;

namespace Uno.Lint.Xaml;

/// <summary>Color helpers shared by HEX and TOKENTHEME.</summary>
internal static class ColorScopes
{
    /// <summary>A keyed resource inside ThemeDictionaries, or anything nested under one, is a theme-aware token.</summary>
    public static bool IsThemeDictionaryToken(XamlElementNode element, XamlSemanticFixerContext context)
    {
        var keyedBelow = false;
        foreach (var e in new[] { element }.Concat(XamlNodes.Ancestors(element, context)))
        {
            var name = XamlNodes.LocalName(e);
            if (name == "ResourceDictionary.ThemeDictionaries")
            {
                return keyedBelow;
            }

            if (XamlNodes.Key(e) != null && name != "ResourceDictionary")
            {
                keyedBelow = true;
            }
        }

        return false;
    }

    /// <summary>The key of an app-defined brush that has one value for both themes, or null.</summary>
    public static string? SingleThemedBrushKey(XamlElementNode element, XamlSemanticFixerContext context)
    {
        var key = XamlNodes.Key(element);
        if (key == null || !key.EndsWith("Brush", StringComparison.Ordinal) || RuleLogic.IsInvariantKey(key))
        {
            return null;
        }

        var name = XamlNodes.LocalName(element);
        if (name == "StaticResource" || name == "ThemeResource" || XamlNodes.IsInside(element, context, "ResourceDictionary.ThemeDictionaries"))
        {
            return null;
        }

        return key;
    }

    /// <summary>The property a Setter or keyframe Value writes to: Setter.Property / Setter.Target, else the nearest Storyboard.TargetProperty.</summary>
    public static string? TargetPropertyOf(XamlElementNode element, XamlSemanticFixerContext context)
    {
        if (XamlNodes.LocalName(element) == "Setter" && (XamlNodes.Attribute(element, "Property") ?? XamlNodes.Attribute(element, "Target")) is { } setter)
        {
            return RuleLogic.LastSegment(setter);
        }

        foreach (var e in new[] { element }.Concat(XamlNodes.Ancestors(element, context)))
        {
            if (XamlNodes.Attributes(e).FirstOrDefault(p => XamlNodes.Name(p) == "Storyboard.TargetProperty")?.RawValue is { } target)
            {
                return RuleLogic.LastSegment(target);
            }
        }

        return null;
    }
}

/// <summary>UNOL001 HEX: a literal color (hex or named) on a color property, or as the text of a color element.</summary>
public sealed class HexRule : IXamlSemanticFixer
{
    public ValueTask<SemanticFixResult> OnElementAsync(XamlElementNode element, XamlSemanticFixerContext context, CancellationToken ct)
    {
        var document = LintDocument.Of(context);
        if (document.IsRefix || document.IsPaletteFile || document.IsThemeOverrideFile)
        {
            return UnoLintXaml.None;
        }

        if (context.IsProcessingElementStart)
        {
            if (ColorScopes.IsThemeDictionaryToken(element, context))
            {
                return UnoLintXaml.None;
            }

            var note = Note(element, context);
            foreach (var property in XamlNodes.Attributes(element))
            {
                // x:Key="Red" names a resource; it is not a color.
                if (XamlNodes.IsXamlLanguage(property, element) || property.RawValue is not { } raw || !RuleLogic.IsColorLiteral(raw))
                {
                    continue;
                }

                var name = XamlNodes.Name(property);
                if (IsColorSlot(element, name, context))
                {
                    UnoLintXaml.Report(context, Rules.Hex, $"literal {raw.Trim()} on {XamlNodes.LocalName(element)}.{name}{note}", property.LineInfo);
                }
            }
        }
        else if (document.Closing is { } frame && frame.Children.Count == 0 && RuleLogic.IsColorLiteral(frame.Text))
        {
            // <Color x:Key="Brand">#7A67F8</Color>, <SolidColorBrush>Red</SolidColorBrush>, <Setter.Value>#222</Setter.Value>
            var start = frame.Start;
            if (IsColorElement(start, context) && !ColorScopes.IsThemeDictionaryToken(start, context))
            {
                UnoLintXaml.Report(context, Rules.Hex, $"literal {frame.Text.Trim()} in <{XamlNodes.LocalName(start)}>{Note(start, context)}", start.LineInfo);
            }
        }

        return UnoLintXaml.None;
    }

    private static string Note(XamlElementNode element, XamlSemanticFixerContext context) =>
        ColorScopes.SingleThemedBrushKey(element, context) is not { } key ? string.Empty
            : XamlNodes.Identifier(key) is { } shown ? $"; {shown} is also defined once for both themes"
            : "; this brush is also defined once for both themes";

    private static bool IsColorSlot(XamlElementNode element, string property, XamlSemanticFixerContext context)
    {
        if (property == "Value" && ColorScopes.TargetPropertyOf(element, context) is { } target && RuleLogic.IsColorName(target))
        {
            return true;
        }

        // A color-named property, or the To/From/By/Value of a color animation or keyframe. Any other property of a
        // color-named control is text: <ColorPicker Header="Red"/> is a label.
        return RuleLogic.IsColorProperty(XamlNodes.LocalName(element), RuleLogic.LastSegment(property));
    }

    private static bool IsColorElement(XamlElementNode element, XamlSemanticFixerContext context)
    {
        var name = XamlNodes.LocalName(element);
        if (name == "Setter.Value")
        {
            return element.ParentIndex is int parent && context.TryGetElement(parent) is { } setter
                && ColorScopes.TargetPropertyOf(setter, context) is { } target && RuleLogic.IsColorName(target);
        }

        return RuleLogic.IsColorName(name);
    }
}

/// <summary>
/// UNOL002 TOKENTHEME: an app-defined brush with one value for both themes. Decided at the end tag, once descendants
/// are known. A brush whose color is a literal is also a HEX hit on the same element; <see cref="UnoLintXaml.Collect"/>
/// keeps only the HEX finding while HEX is enabled for the file.
/// </summary>
public sealed class TokenThemeRule : IXamlSemanticFixer
{
    public ValueTask<SemanticFixResult> OnElementAsync(XamlElementNode element, XamlSemanticFixerContext context, CancellationToken ct)
    {
        var document = LintDocument.Of(context);
        if (context.IsProcessingElementStart || document.IsThemeOverrideFile || document.Closing is not { } frame)
        {
            return UnoLintXaml.None;
        }

        var start = frame.Start;
        if (ColorScopes.SingleThemedBrushKey(start, context) is not { } key)
        {
            return UnoLintXaml.None;
        }

        // A {ThemeResource} on any attribute of the brush (Color, TintColor, FallbackColor...) or anywhere below it follows the theme.
        if (XamlNodes.Attributes(start).Any(p => p.RawValue is { } raw && UnoLintXaml.ReferencesThemeResource(raw)) || frame.ThemeResourceBelow)
        {
            return UnoLintXaml.None;
        }

        var shown = XamlNodes.Identifier(key) ?? "this brush";
        UnoLintXaml.Report(context, Rules.TokenTheme, $"{shown} has one value for both themes", start.LineInfo);
        return UnoLintXaml.None;
    }
}

/// <summary>UNOL003 ICON: inline path geometry or shapes composed in a Viewbox, outside Icons files and templates.</summary>
public sealed partial class IconRule : IXamlSemanticFixer
{
    public ValueTask<SemanticFixResult> OnElementAsync(XamlElementNode element, XamlSemanticFixerContext context, CancellationToken ct)
    {
        var document = LintDocument.Of(context);
        if (document.IsRefix || document.IsIconFile)
        {
            return UnoLintXaml.None;
        }

        if (context.IsProcessingElementStart)
        {
            var name = XamlNodes.LocalName(element);
            if ((name == "Path" || name == "PathIcon" || name == "PathGeometry" || name == "GeometryGroup") && XamlNodes.IsInside(element, context, "ControlTemplate"))
            {
                return UnoLintXaml.None;
            }

            if ((name == "Path" || name == "PathIcon") && XamlNodes.Attribute(element, "Data") is { } data && PathMiniLanguage().IsMatch(data))
            {
                UnoLintXaml.Report(context, Rules.Icon, $"inline <{name} Data> path geometry", element.LineInfo);
            }
            else if (name == "PathGeometry" || name == "GeometryGroup")
            {
                UnoLintXaml.Report(context, Rules.Icon, $"inline <{name}>", element.LineInfo);
            }
        }
        else if (document.Closing is { } frame && XamlNodes.LocalName(frame.Start) == "Viewbox" && frame.ShapesBelow.Count > 0
            && !XamlNodes.IsInside(frame.Start, context, "ControlTemplate"))
        {
            var kinds = string.Join("/", frame.ShapesBelow.Distinct());
            UnoLintXaml.Report(context, Rules.Icon, $"Viewbox composed of {frame.ShapesBelow.Count} shape(s) ({kinds})", frame.Start.LineInfo);
        }

        return UnoLintXaml.None;
    }

    [GeneratedRegex(@"^\s*(?:F[01]\s*)?[Mm]")]
    private static partial Regex PathMiniLanguage();
}

/// <summary>UNOL101 CARD: a rounded, filled or stroked framework Border. Decided at the end tag, once its children are known.</summary>
public sealed class CardRule : IXamlSemanticFixer
{
    public ValueTask<SemanticFixResult> OnElementAsync(XamlElementNode element, XamlSemanticFixerContext context, CancellationToken ct)
    {
        var document = LintDocument.Of(context);
        if (context.IsProcessingElementStart || document.Closing is not { } frame)
        {
            return UnoLintXaml.None;
        }

        var border = frame.Start;
        if (XamlNodes.LocalName(border) != "Border" || border.Name.NamespaceUri != XamlNodes.Presentation)
        {
            return UnoLintXaml.None;
        }

        if (XamlNodes.Attribute(border, "CornerRadius") is not { } cornerRadius || !RuleLogic.TryUniformRadius(cornerRadius, out var radius) || radius == 0)
        {
            return UnoLintXaml.None;
        }

        var hasBackground = XamlNodes.Attribute(border, "Background") != null;
        if (!hasBackground && XamlNodes.Attribute(border, "BorderBrush") == null && XamlNodes.Attribute(border, "Padding") == null)
        {
            return UnoLintXaml.None;
        }

        if (XamlNodes.IsInside(border, context, "ControlTemplate") || (!hasBackground && XamlNodes.IsInside(border, context, "ShadowContainer")))
        {
            return UnoLintXaml.None;
        }

        if (!double.IsNaN(radius))
        {
            var width = RuleLogic.ParseNumber(XamlNodes.Attribute(border, "Width"));
            var height = RuleLogic.ParseNumber(XamlNodes.Attribute(border, "Height"));
            if (RuleLogic.IsCircleOrPill(radius, width, height))
            {
                return UnoLintXaml.None;
            }

            var content = frame.Children.Where(n => !n.StartsWith("Border.", StringComparison.Ordinal)).ToList();
            if (radius >= RuleLogic.ChipRadius && RuleLogic.IsChipContent(content))
            {
                return UnoLintXaml.None;
            }
        }

        var name = XamlNodes.Identifier(XamlNodes.XName(border));
        UnoLintXaml.Report(context, Rules.Card, name != null ? $"<Border x:Name=\"{name}\"> drawn as a card" : "<Border> drawn as a card", border.LineInfo);
        return UnoLintXaml.None;
    }
}

/// <summary>UNOL102 BACKBAR: Navigation.Request="-" on a page with no NavigationBar. Decided when the root element closes.</summary>
public sealed class BackBarRule : IXamlSemanticFixer
{
    public ValueTask<SemanticFixResult> OnElementAsync(XamlElementNode element, XamlSemanticFixerContext context, CancellationToken ct)
    {
        var document = LintDocument.Of(context);
        if (context.IsProcessingElementStart)
        {
            if (!document.IsRefix)
            {
                var request = XamlNodes.Attributes(element).FirstOrDefault(p => XamlNodes.Name(p) == "Navigation.Request");
                if (request?.RawValue?.Trim() == "-")
                {
                    document.PendingBackRequests.Add(request.LineInfo);
                }
            }
        }
        else if (document.Closing is { } frame && frame.Start.ParentIndex == null && !document.SawNavigationBar)
        {
            foreach (var at in document.PendingBackRequests)
            {
                UnoLintXaml.Report(context, Rules.BackBar, "hand-built back button; no NavigationBar on this page", at);
            }
        }

        return UnoLintXaml.None;
    }
}

/// <summary>UNOL201 BINDVALUE: {Binding X.Value} outside item templates, the MVUX state trap.</summary>
public sealed partial class BindValueRule : IXamlSemanticFixer
{
    public ValueTask<SemanticFixResult> OnElementAsync(XamlElementNode element, XamlSemanticFixerContext context, CancellationToken ct)
    {
        var document = LintDocument.Of(context);
        if (!context.IsProcessingElementStart || document.IsRefix || XamlNodes.IsInside(element, context, "DataTemplate"))
        {
            return UnoLintXaml.None;
        }

        foreach (var property in XamlNodes.Attributes(element))
        {
            if (property.RawValue is not { } value || !value.Contains(".Value", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (Match binding in BindingMarkup().Matches(value))
            {
                var match = ValuePath().Match(binding.Groups["body"].Value);
                if (match.Success)
                {
                    var path = match.Groups["path"].Value;
                    UnoLintXaml.Report(context, Rules.BindValue, $"{{Binding {path}.Value}} on {XamlNodes.LocalName(element)}.{XamlNodes.Name(property)}; bind {path} itself", property.LineInfo);
                }
            }
        }

        return UnoLintXaml.None;
    }

    [GeneratedRegex(@"\{\s*Binding\b(?<body>(?:[^{}]|\{[^{}]*\})*)\}")]
    private static partial Regex BindingMarkup();

    [GeneratedRegex(@"^\s*(?:Path\s*=\s*)?(?<path>[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)\.Value\b|[\s,]Path\s*=\s*(?<path>[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)\.Value\b")]
    private static partial Regex ValuePath();
}

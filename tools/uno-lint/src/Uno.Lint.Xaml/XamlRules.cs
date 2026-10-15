using System.Text.RegularExpressions;
using Uno.XamlParser.Configuration;
using Uno.XamlParser.Stage0;
using Uno.XamlParser.Stage1;
using Uno.XamlParser.Stage1.Nodes;

namespace Uno.Lint.Xaml;

/// <summary>
/// The uno-lint XAML rules, written as Uno.XamlParser Stage-1 semantic fixers that only report. Any host that already
/// runs Uno.XamlParser (Studio Live, Uno.AI) enables them with one call on its pipeline options:
/// <code>services.AddXamlStreamPipeline(o => UnoLintXamlRules.AddTo(o));</code>
/// Diagnostics carry the rule id as their code (UNOL001...). They report every hit; severity, profiles,
/// .editorconfig, project gates and suppression comments are applied by the uno-lint host (<see cref="XamlParserEngine"/>).
/// </summary>
public static class UnoLintXamlRules
{
    /// <summary>Internal code for a single-themed brush whose color is a literal; HEX reports it instead when HEX is active.</summary>
    public const string TokenThemeLiteralCode = "UNOL002.literal";

    public static void AddTo(XamlStreamPipelineOptions options)
    {
        options.VisitorTypes.Add(typeof(ElementTextCapture));
        options.SemanticFixerTypes.Add(typeof(DocumentTracker));
        options.SemanticFixerTypes.Add(typeof(HexRule));
        options.SemanticFixerTypes.Add(typeof(TokenThemeRule));
        options.SemanticFixerTypes.Add(typeof(IconRule));
        options.SemanticFixerTypes.Add(typeof(CardRule));
        options.SemanticFixerTypes.Add(typeof(BackBarRule));
        options.SemanticFixerTypes.Add(typeof(BindValueRule));
    }

    internal static ValueTask<SemanticFixResult> None => ValueTask.FromResult(SemanticFixResult.None);

    internal static void Report(XamlSemanticFixerContext context, string code, string message, XamlLineInfo at) =>
        context.ReportDiagnostic(code, message, at, XamlDiagnosticSeverity.Warning);

    // ---------------------------------------------------------------- shared color logic

    /// <summary>A keyed resource inside ThemeDictionaries, or anything nested under one, is a theme-aware token.</summary>
    internal static bool IsThemeDictionaryToken(XamlElementNode element, XamlSemanticFixerContext context)
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
    internal static string? SingleThemedBrushKey(XamlElementNode element, XamlSemanticFixerContext context)
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
    internal static string? TargetPropertyOf(XamlElementNode element, XamlSemanticFixerContext context)
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
            return UnoLintXamlRules.None;
        }

        if (context.IsProcessingElementStart)
        {
            if (UnoLintXamlRules.IsThemeDictionaryToken(element, context))
            {
                return UnoLintXamlRules.None;
            }

            var note = Note(element, context);
            foreach (var property in XamlNodes.Attributes(element))
            {
                // x:Key="Red" names a resource; it is not a color.
                if (XamlNodes.IsXamlLanguage(property, element) || !RuleLogic.IsColorLiteral(property.RawValue))
                {
                    continue;
                }

                var name = XamlNodes.Name(property);
                if (IsColorSlot(element, name, context))
                {
                    UnoLintXamlRules.Report(context, Rules.Hex.Id, $"literal {property.RawValue!.Trim()} on {XamlNodes.LocalName(element)}.{name}{note}", property.LineInfo);
                }
            }
        }
        else if (document.Closing is { } frame && frame.Children.Count == 0 && RuleLogic.IsColorLiteral(frame.Text))
        {
            // <Color x:Key="Brand">#7A67F8</Color>, <SolidColorBrush>Red</SolidColorBrush>, <Setter.Value>#222</Setter.Value>
            var start = frame.Start;
            if (IsColorElement(start, context) && !UnoLintXamlRules.IsThemeDictionaryToken(start, context))
            {
                UnoLintXamlRules.Report(context, Rules.Hex.Id, $"literal {frame.Text.Trim()} in <{XamlNodes.LocalName(start)}>{Note(start, context)}", start.LineInfo);
            }
        }

        return UnoLintXamlRules.None;
    }

    private static string Note(XamlElementNode element, XamlSemanticFixerContext context) =>
        UnoLintXamlRules.SingleThemedBrushKey(element, context) is { } key ? $"; {key} is also defined once for both themes" : string.Empty;

    private static bool IsColorSlot(XamlElementNode element, string property, XamlSemanticFixerContext context)
    {
        if (property == "Value")
        {
            return UnoLintXamlRules.TargetPropertyOf(element, context) is { } target && RuleLogic.IsColorName(target);
        }

        return RuleLogic.IsColorName(RuleLogic.LastSegment(property)) || RuleLogic.IsColorName(XamlNodes.LocalName(element));
    }

    private static bool IsColorElement(XamlElementNode element, XamlSemanticFixerContext context)
    {
        var name = XamlNodes.LocalName(element);
        if (name == "Setter.Value")
        {
            return element.ParentIndex is int parent && context.TryGetElement(parent) is { } setter
                && UnoLintXamlRules.TargetPropertyOf(setter, context) is { } target && RuleLogic.IsColorName(target);
        }

        return RuleLogic.IsColorName(name);
    }
}

/// <summary>UNOL002 TOKENTHEME: an app-defined brush with one value for both themes. Decided at the end tag, once descendants are known.</summary>
public sealed class TokenThemeRule : IXamlSemanticFixer
{
    private static readonly Regex ThemeResourceRef = new(@"\{\s*ThemeResource\b", RegexOptions.Compiled);

    public ValueTask<SemanticFixResult> OnElementAsync(XamlElementNode element, XamlSemanticFixerContext context, CancellationToken ct)
    {
        var document = LintDocument.Of(context);
        if (context.IsProcessingElementStart || document.IsThemeOverrideFile || document.Closing is not { } frame)
        {
            return UnoLintXamlRules.None;
        }

        var start = frame.Start;
        if (UnoLintXamlRules.SingleThemedBrushKey(start, context) is not { } key)
        {
            return UnoLintXamlRules.None;
        }

        // Color="{ThemeResource ...}" on the brush, or a ThemeResource anywhere below it, follows the theme.
        var color = XamlNodes.Attribute(start, "Color");
        if ((color != null && ThemeResourceRef.IsMatch(color)) || frame.ThemeResourceBelow)
        {
            return UnoLintXamlRules.None;
        }

        // A literal color here is also a HEX hit; the host keeps only one of the two (HEX when it is active).
        var literal = RuleLogic.IsColorLiteral(color) || (frame.Children.Count == 0 && RuleLogic.IsColorLiteral(frame.Text));
        UnoLintXamlRules.Report(context, literal ? UnoLintXamlRules.TokenThemeLiteralCode : Rules.TokenTheme.Id, $"{key} has one value for both themes", start.LineInfo);
        return UnoLintXamlRules.None;
    }
}

/// <summary>UNOL003 ICON: inline path geometry or shapes composed in a Viewbox, outside Icons files and templates.</summary>
public sealed class IconRule : IXamlSemanticFixer
{
    private static readonly Regex PathMiniLanguage = new(@"^\s*(?:F[01]\s*)?[Mm]", RegexOptions.Compiled);

    public ValueTask<SemanticFixResult> OnElementAsync(XamlElementNode element, XamlSemanticFixerContext context, CancellationToken ct)
    {
        var document = LintDocument.Of(context);
        if (document.IsRefix || document.IsIconFile)
        {
            return UnoLintXamlRules.None;
        }

        if (context.IsProcessingElementStart)
        {
            var name = XamlNodes.LocalName(element);
            if ((name == "Path" || name == "PathIcon" || name == "PathGeometry" || name == "GeometryGroup") && XamlNodes.IsInside(element, context, "ControlTemplate"))
            {
                return UnoLintXamlRules.None;
            }

            if ((name == "Path" || name == "PathIcon") && XamlNodes.Attribute(element, "Data") is { } data && PathMiniLanguage.IsMatch(data))
            {
                UnoLintXamlRules.Report(context, Rules.Icon.Id, $"inline <{name} Data> path geometry", element.LineInfo);
            }
            else if (name == "PathGeometry" || name == "GeometryGroup")
            {
                UnoLintXamlRules.Report(context, Rules.Icon.Id, $"inline <{name}>", element.LineInfo);
            }
        }
        else if (document.Closing is { } frame && XamlNodes.LocalName(frame.Start) == "Viewbox" && frame.ShapesBelow.Count > 0
            && !XamlNodes.IsInside(frame.Start, context, "ControlTemplate"))
        {
            var kinds = string.Join("/", frame.ShapesBelow.Distinct());
            UnoLintXamlRules.Report(context, Rules.Icon.Id, $"Viewbox composed of {frame.ShapesBelow.Count} shape(s) ({kinds})", frame.Start.LineInfo);
        }

        return UnoLintXamlRules.None;
    }
}

/// <summary>UNOL101 CARD: a rounded, filled or stroked framework Border. Decided at the end tag, once its children are known.</summary>
public sealed class CardRule : IXamlSemanticFixer
{
    public ValueTask<SemanticFixResult> OnElementAsync(XamlElementNode element, XamlSemanticFixerContext context, CancellationToken ct)
    {
        var document = LintDocument.Of(context);
        if (context.IsProcessingElementStart || document.Closing is not { } frame)
        {
            return UnoLintXamlRules.None;
        }

        var border = frame.Start;
        if (XamlNodes.LocalName(border) != "Border" || border.Name.NamespaceUri != XamlNodes.Presentation)
        {
            return UnoLintXamlRules.None;
        }

        var cornerRadius = XamlNodes.Attribute(border, "CornerRadius");
        if (cornerRadius == null || !RuleLogic.TryUniformRadius(cornerRadius, out var radius) || radius == 0)
        {
            return UnoLintXamlRules.None;
        }

        var hasBackground = XamlNodes.Attribute(border, "Background") != null;
        if (!hasBackground && XamlNodes.Attribute(border, "BorderBrush") == null && XamlNodes.Attribute(border, "Padding") == null)
        {
            return UnoLintXamlRules.None;
        }

        if (XamlNodes.IsInside(border, context, "ControlTemplate") || (!hasBackground && XamlNodes.IsInside(border, context, "ShadowContainer")))
        {
            return UnoLintXamlRules.None;
        }

        if (!double.IsNaN(radius))
        {
            var width = RuleLogic.ParseNumber(XamlNodes.Attribute(border, "Width"));
            var height = RuleLogic.ParseNumber(XamlNodes.Attribute(border, "Height"));
            if (RuleLogic.IsCircleOrPill(radius, width, height))
            {
                return UnoLintXamlRules.None;
            }

            var content = frame.Children.Where(n => !n.StartsWith("Border.", StringComparison.Ordinal)).ToList();
            if (radius >= RuleLogic.ChipRadius && RuleLogic.IsChipContent(content))
            {
                return UnoLintXamlRules.None;
            }
        }

        var name = XamlNodes.XName(border);
        UnoLintXamlRules.Report(context, Rules.Card.Id, name != null ? $"<Border x:Name=\"{name}\"> drawn as a card" : "<Border> drawn as a card", border.LineInfo);
        return UnoLintXamlRules.None;
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
                UnoLintXamlRules.Report(context, Rules.BackBar.Id, "hand-built back button; no NavigationBar on this page", at);
            }
        }

        return UnoLintXamlRules.None;
    }
}

/// <summary>UNOL201 BINDVALUE: {Binding X.Value} outside item templates, the MVUX state trap.</summary>
public sealed class BindValueRule : IXamlSemanticFixer
{
    private static readonly Regex BindingMarkup = new(@"\{\s*Binding\b(?<body>(?:[^{}]|\{[^{}]*\})*)\}", RegexOptions.Compiled);
    private static readonly Regex ValuePath = new(@"^\s*(?:Path\s*=\s*)?(?<path>[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)\.Value\b|[\s,]Path\s*=\s*(?<path>[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)\.Value\b", RegexOptions.Compiled);

    public ValueTask<SemanticFixResult> OnElementAsync(XamlElementNode element, XamlSemanticFixerContext context, CancellationToken ct)
    {
        var document = LintDocument.Of(context);
        if (!context.IsProcessingElementStart || document.IsRefix || XamlNodes.IsInside(element, context, "DataTemplate"))
        {
            return UnoLintXamlRules.None;
        }

        foreach (var property in XamlNodes.Attributes(element))
        {
            var value = property.RawValue;
            if (value == null || value.IndexOf(".Value", StringComparison.Ordinal) < 0)
            {
                continue;
            }

            foreach (Match binding in BindingMarkup.Matches(value))
            {
                var match = ValuePath.Match(binding.Groups["body"].Value);
                if (match.Success)
                {
                    var path = match.Groups["path"].Value;
                    UnoLintXamlRules.Report(context, Rules.BindValue.Id, $"{{Binding {path}.Value}} on {XamlNodes.LocalName(element)}.{XamlNodes.Name(property)}; bind {path} itself", property.LineInfo);
                }
            }
        }

        return UnoLintXamlRules.None;
    }
}

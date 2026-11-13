using System.Text.RegularExpressions;
using Uno.XamlParser;
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

    /// <summary>A {ThemeResource} on any attribute of the element (Color, TintColor, FallbackColor...) or anywhere below it follows the theme.</summary>
    public static bool FollowsTheme(ElementFrame frame) =>
        XamlNodes.Attributes(frame.Start).Any(p => p.RawValue is { } raw && UnoLintXaml.ReferencesThemeResource(raw)) || frame.ThemeResourceBelow;

    /// <summary>A property element such as <c>&lt;SolidColorBrush.Color&gt;</c>.</summary>
    public static bool IsPropertyElement(XamlElementNode element) => XamlNodes.LocalName(element).Contains('.', StringComparison.Ordinal);

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
            if (document.InResourceFile && ColorScopes.IsThemeDictionaryToken(element, context))
            {
                return UnoLintXaml.None;
            }

            // DocumentTracker, registered first, already pushed this element's frame.
            var brush = ColorScopes.SingleThemedBrushKey(element, context) != null && document.Open.Count > 0 ? document.Open.Peek() : null;
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
                    Report(context, brush, $"literal {raw.Trim()} on {XamlNodes.LocalName(element)}.{name}", property.LineInfo);
                }
            }
        }
        else if (document.Closing is { } frame)
        {
            // <Color x:Key="Brand">#7A67F8</Color>, <SolidColorBrush>Red</SolidColorBrush>, <Setter.Value>#222</Setter.Value>
            var start = frame.Start;
            if (frame.Children.Count == 0 && RuleLogic.IsColorLiteral(frame.Text) && IsColorElement(start, context) && !(document.InResourceFile && ColorScopes.IsThemeDictionaryToken(start, context)))
            {
                var brush = BrushOf(document);
                Report(context, ColorScopes.SingleThemedBrushKey(brush.Start, context) != null ? brush : null, $"literal {frame.Text.Trim()} in <{XamlNodes.LocalName(start)}>", start.LineInfo);
            }

            if (frame.PendingHex.Count > 0)
            {
                var note = Note(frame, context);
                foreach (var (message, at) in frame.PendingHex)
                {
                    UnoLintXaml.Report(context, Rules.Hex, message + note, at);
                }
            }
        }

        return UnoLintXaml.None;
    }

    /// <summary>Reports now, or holds the hit on the brush's frame until its end tag decides the note.</summary>
    private static void Report(XamlSemanticFixerContext context, ElementFrame? brush, string message, XamlLineInfo at)
    {
        if (brush != null)
        {
            brush.PendingHex.Add((message, at));
        }
        else
        {
            UnoLintXaml.Report(context, Rules.Hex, message, at);
        }
    }

    /// <summary>
    /// The frame of the element a closing element's text colors: the element itself, the owner of a property element
    /// (<c>&lt;SolidColorBrush.Color&gt;</c>), or the owner around a <c>&lt;Color&gt;</c> inside one. The closing frame is
    /// still on top of the stack.
    /// </summary>
    private static ElementFrame BrushOf(LintDocument document)
    {
        var path = document.Open.Take(3).ToList();
        if (ColorScopes.IsPropertyElement(path[0].Start) && path.Count > 1)
        {
            return path[1];
        }

        return path.Count > 2 && ColorScopes.IsPropertyElement(path[1].Start) ? path[2] : path[0];
    }

    /// <summary>The single-theme note, only where TOKENTHEME would report the brush: a brush that follows the theme gets none.</summary>
    private static string Note(ElementFrame brush, XamlSemanticFixerContext context) =>
        ColorScopes.SingleThemedBrushKey(brush.Start, context) is not { } key || ColorScopes.FollowsTheme(brush) ? string.Empty
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

    /// <summary>
    /// A color element (<c>&lt;Color&gt;</c>, <c>&lt;SolidColorBrush&gt;</c>) or a property element whose property holds a
    /// color: <c>&lt;SolidColorBrush.Color&gt;</c>, or a Setter or keyframe <c>Value</c> whose target is a color.
    /// <c>&lt;ColorPicker.Header&gt;</c> is a label.
    /// </summary>
    private static bool IsColorElement(XamlElementNode element, XamlSemanticFixerContext context)
    {
        var name = XamlNodes.LocalName(element);
        var dot = name.IndexOf('.', StringComparison.Ordinal);
        if (dot < 0)
        {
            return RuleLogic.IsColorName(name);
        }

        if (element.ParentIndex is not int parent || context.TryGetElement(parent) is not { } owner)
        {
            return false;
        }

        var property = name[(dot + 1)..];
        return property == "Value" && ColorScopes.TargetPropertyOf(owner, context) is { } target
            ? RuleLogic.IsColorName(target)
            : RuleLogic.IsColorProperty(XamlNodes.LocalName(owner), property);
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

        if (ColorScopes.FollowsTheme(frame))
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

/// <summary>
/// UNOL101 CARD: a rounded, filled or stroked framework Border, or a Border style that draws one. Decided at the end
/// tag, once its children or setters are known.
/// </summary>
public sealed partial class CardRule : IXamlSemanticFixer
{
    public ValueTask<SemanticFixResult> OnElementAsync(XamlElementNode element, XamlSemanticFixerContext context, CancellationToken ct)
    {
        var document = LintDocument.Of(context);
        if (context.IsProcessingElementStart || document.Closing is not { } frame)
        {
            return UnoLintXaml.None;
        }

        if (XamlNodes.LocalName(frame.Start) == "Style")
        {
            CheckStyle(frame, context);
            return UnoLintXaml.None;
        }

        var border = frame.Start;
        if (XamlNodes.LocalName(border) != "Border" || border.Name.NamespaceUri != XmlNamespaces.Presentation)
        {
            return UnoLintXaml.None;
        }

        // Properties are read from the end-tag node, which also carries <Border.Background>-style property elements.
        bool Has(string property) => XamlNodes.Property(element, property) != null || frame.Children.Contains("Border." + property);
        if (XamlNodes.Property(element, "CornerRadius") is not { } cornerRadius || !RuleLogic.TryUniformRadius(cornerRadius, out var radius) || radius == 0)
        {
            return UnoLintXaml.None;
        }

        // A Border filled with an ImageBrush clips an image; that is what a bare Border is for.
        var hasBackground = Has("Background");
        if (frame.HasImageBackground || (!hasBackground && !Has("BorderBrush") && !Has("Padding")))
        {
            return UnoLintXaml.None;
        }

        if (XamlNodes.IsInside(border, context, "ControlTemplate") || (!hasBackground && XamlNodes.IsInside(border, context, "ShadowContainer")))
        {
            return UnoLintXaml.None;
        }

        if (!double.IsNaN(radius))
        {
            var width = RuleLogic.ParseNumber(XamlNodes.Property(element, "Width"));
            var height = RuleLogic.ParseNumber(XamlNodes.Property(element, "Height"));
            if (RuleLogic.IsCircleOrPill(radius, width, height))
            {
                return UnoLintXaml.None;
            }

            if (radius >= RuleLogic.ChipRadius && RuleLogic.IsChipContent(frame.Content))
            {
                return UnoLintXaml.None;
            }
        }

        var name = XamlNodes.Identifier(XamlNodes.XName(border));
        UnoLintXaml.Report(context, Rules.Card, name != null ? $"<Border x:Name=\"{name}\"> drawn as a card" : "<Border> drawn as a card", border.LineInfo);
        return UnoLintXaml.None;
    }

    /// <summary>
    /// A <c>&lt;Style TargetType="Border"&gt;</c> with a uniform radius and a fill plus a stroke or padding, or a fill or
    /// stroke under a key that says Card. Fill and radius alone is also a skeleton bar or a badge whose size is set
    /// where the style is used, so a style needs more than a bare Border does.
    /// </summary>
    private static void CheckStyle(ElementFrame frame, XamlSemanticFixerContext context)
    {
        var style = frame.Start;
        if (XamlNodes.Attribute(style, "TargetType")?.Trim() != "Border")
        {
            return;
        }

        var setters = frame.Setters;
        if (!setters.TryGetValue("CornerRadius", out var cornerRadius) || cornerRadius == null
            || !RuleLogic.TryUniformRadius(cornerRadius, out var radius) || radius == 0)
        {
            return;
        }

        var key = XamlNodes.Identifier(XamlNodes.Key(style));
        var hasBackground = setters.ContainsKey("Background");
        var hasStroke = setters.ContainsKey("BorderBrush");
        var namedCard = key != null && CardWord().IsMatch(CSharpAnalyzer.SplitWords(key));
        if (namedCard ? !hasBackground && !hasStroke : !hasBackground || (!hasStroke && !setters.ContainsKey("Padding")))
        {
            return;
        }

        var width = RuleLogic.ParseNumber(setters.GetValueOrDefault("Width"));
        var height = RuleLogic.ParseNumber(setters.GetValueOrDefault("Height"));
        if (!double.IsNaN(radius) && RuleLogic.IsCircleOrPill(radius, width, height))
        {
            return;
        }

        UnoLintXaml.Report(context, Rules.Card, key != null ? $"Border style \"{key}\" draws a card" : "implicit Border style draws a card", style.LineInfo);
    }

    [GeneratedRegex(@"(?i)\bcards?\b")]
    private static partial Regex CardWord();
}

/// <summary>
/// UNOL102 BACKBAR: a hand-built back button on a page with no NavigationBar: <c>Navigation.Request="-"</c>, or a button
/// that shows a back icon. Decided when the root element closes.
/// </summary>
public sealed class BackBarRule : IXamlSemanticFixer
{
    private static readonly HashSet<string> Buttons = new(StringComparer.Ordinal) { "Button", "AppBarButton", "HyperlinkButton", "RepeatButton" };

    // Segoe Fluent / MDL2 back arrows: Back, ChromeBack, BackBttnArrow42Legacy, and the Segoe UI Symbol Back.
    private static readonly HashSet<int> BackGlyphs = new() { 0xE72B, 0xE830, 0xE0A6, 0xE112 };

    public ValueTask<SemanticFixResult> OnElementAsync(XamlElementNode element, XamlSemanticFixerContext context, CancellationToken ct)
    {
        var document = LintDocument.Of(context);
        if (context.IsProcessingElementStart)
        {
            if (!document.IsRefix)
            {
                if (BackRequest(element) is { } request)
                {
                    document.PendingBackButtons.Add(new BackButton(request.LineInfo, Icon: false));
                }
                else if (BackIconButton(element, context) is { } button && !document.PendingBackButtons.Any(b => b.At.LineNumber == button.LineInfo.LineNumber && b.At.LinePosition == button.LineInfo.LinePosition))
                {
                    document.PendingBackButtons.Add(new BackButton(button.LineInfo, Icon: true));
                }
            }
        }
        else if (document.Closing is { } frame && frame.Start.ParentIndex == null)
        {
            if (!document.SawNavigationBar)
            {
                foreach (var button in document.PendingBackButtons)
                {
                    var shape = button.Icon ? "hand-built back button (back icon)" : "hand-built back button";
                    UnoLintXaml.Report(context, Rules.BackBar, $"{shape}; no NavigationBar on this page", button.At);
                }
            }

            // A fragment can hold several roots; each is its own page.
            document.PendingBackButtons.Clear();
            document.SawNavigationBar = false;
        }

        return UnoLintXaml.None;
    }

    private static XamlPropertyNode? BackRequest(XamlElementNode element) =>
        XamlNodes.Attributes(element).FirstOrDefault(p => XamlNodes.Name(p) == "Navigation.Request" && p.RawValue?.Trim() == "-");

    /// <summary>
    /// The button a back icon sits in: <c>&lt;AppBarButton Icon="Back"&gt;</c> itself, or the nearest button around a
    /// <c>SymbolIcon Symbol="Back"</c> or a back-arrow <c>FontIcon</c>. A button that already requests back navigation
    /// is reported by its request; a template draws a control's own parts, not a page header.
    /// </summary>
    private static XamlElementNode? BackIconButton(XamlElementNode element, XamlSemanticFixerContext context)
    {
        var name = XamlNodes.LocalName(element);
        XamlElementNode? button;
        if (name == "AppBarButton" && XamlNodes.Attribute(element, "Icon")?.Trim() == "Back")
        {
            button = element;
        }
        else if ((name == "SymbolIcon" && XamlNodes.Attribute(element, "Symbol")?.Trim() == "Back")
            || (name == "FontIcon" && IsBackGlyph(XamlNodes.Attribute(element, "Glyph"))))
        {
            button = XamlNodes.Ancestors(element, context).FirstOrDefault(a => Buttons.Contains(XamlNodes.LocalName(a)));
        }
        else
        {
            return null;
        }

        return button != null && BackRequest(button) == null && !XamlNodes.IsInside(element, context, "ControlTemplate") ? button : null;
    }

    private static bool IsBackGlyph(string? glyph)
    {
        var value = glyph?.Trim();
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        if (value.StartsWith("&#x", StringComparison.OrdinalIgnoreCase) && value.EndsWith(';'))
        {
            return int.TryParse(value[3..^1], System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out var code) && BackGlyphs.Contains(code);
        }

        return value.Length == 1 && BackGlyphs.Contains(value[0]);
    }
}

/// <summary>
/// UNOL105 TYPERAMP: a literal FontSize in a page or control, on an element or a Setter. Resource files are where the
/// app's own text styles live; icons size their glyph with FontSize; templates draw a control's parts.
/// </summary>
public sealed class TypeRampRule : IXamlSemanticFixer
{
    private static readonly HashSet<string> Icons = new(StringComparer.Ordinal) { "FontIcon", "SymbolIcon", "PathIcon", "BitmapIcon", "ImageIcon", "AnimatedIcon", "FontIconSource", "SymbolIconSource" };

    public ValueTask<SemanticFixResult> OnElementAsync(XamlElementNode element, XamlSemanticFixerContext context, CancellationToken ct)
    {
        var document = LintDocument.Of(context);
        if (!context.IsProcessingElementStart || document.IsRefix || document.InResourceFile || XamlNodes.IsInside(element, context, "ControlTemplate"))
        {
            return UnoLintXaml.None;
        }

        var name = XamlNodes.LocalName(element);
        if (name == "Setter")
        {
            if ((XamlNodes.Attribute(element, "Property") ?? XamlNodes.Attribute(element, "Target")) is { } target && RuleLogic.LastSegment(target) == "FontSize"
                && XamlNodes.Attribute(element, "Value") is { } value && RuleLogic.ParseNumber(value.Trim()) != null)
            {
                UnoLintXaml.Report(context, Rules.TypeRamp, $"literal FontSize {value.Trim()} in a Setter", element.LineInfo);
            }
        }
        else if (!Icons.Contains(name) && XamlNodes.Attributes(element).FirstOrDefault(p => XamlNodes.Name(p) == "FontSize") is { RawValue: { } size } property
            && RuleLogic.ParseNumber(size.Trim()) != null)
        {
            UnoLintXaml.Report(context, Rules.TypeRamp, $"literal FontSize {size.Trim()} on {name}", property.LineInfo);
        }

        return UnoLintXaml.None;
    }
}

/// <summary>
/// UNOL106 RETEMPLATE: a ControlTemplate whose target, from its TargetType or its Style's, is a platform or Toolkit
/// control. Generic hosts (Control, ContentControl, UserControl, Page, Frame, ItemsControl) have no look of their own
/// to lose, presenters (FlyoutPresenter for a drawer) are templated by design and expose few lightweight keys, and the
/// app's own controls need a template.
/// </summary>
public sealed class RetemplateRule : IXamlSemanticFixer
{
    private static readonly HashSet<string> Hosts = new(StringComparer.Ordinal) { "Control", "ContentControl", "UserControl", "Page", "Frame", "ItemsControl" };

    public ValueTask<SemanticFixResult> OnElementAsync(XamlElementNode element, XamlSemanticFixerContext context, CancellationToken ct)
    {
        var document = LintDocument.Of(context);
        if (!context.IsProcessingElementStart || document.IsRefix || XamlNodes.LocalName(element) != "ControlTemplate" || element.Name.NamespaceUri != XmlNamespaces.Presentation)
        {
            return UnoLintXaml.None;
        }

        var targetType = XamlNodes.Attribute(element, "TargetType");
        if (targetType == null && XamlNodes.Ancestors(element, context).FirstOrDefault(a => XamlNodes.LocalName(a) == "Style") is { } style)
        {
            targetType = XamlNodes.Attribute(style, "TargetType");
        }

        if (targetType?.Trim() is not { Length: > 0 } type || type.StartsWith('{'))
        {
            return UnoLintXaml.None;
        }

        var colon = type.IndexOf(':');
        var prefix = colon > 0 ? type[..colon] : null;
        var local = type[(colon + 1)..];
        if (Hosts.Contains(local) || local.EndsWith("Presenter", StringComparison.Ordinal) || XamlNodes.Identifier(local) is null)
        {
            return UnoLintXaml.None;
        }

        var stock = prefix == null
            ? element.Name.NamespaceUri == XmlNamespaces.Presentation
            : element.NamespaceScope.TryGetValue(prefix, out var uri) && IsStockNamespace(uri);
        if (stock && (prefix == null || XamlNodes.Identifier(prefix) != null))
        {
            UnoLintXaml.Report(context, Rules.Retemplate, $"ControlTemplate copied for {(prefix == null ? local : prefix + ":" + local)}", element.LineInfo);
        }

        return UnoLintXaml.None;
    }

    private static bool IsStockNamespace(string uri) =>
        uri == XmlNamespaces.Presentation
        || uri.StartsWith("using:Microsoft.UI.Xaml.Controls", StringComparison.Ordinal)
        || uri.StartsWith("using:Windows.UI.Xaml.Controls", StringComparison.Ordinal)
        || uri.StartsWith("using:Uno.Toolkit.UI", StringComparison.Ordinal);
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
                // ElementName, RelativeSource and Source bind to a control or an explicit object, not the page model.
                var body = binding.Groups["body"].Value;
                var match = ValuePath().Match(body);
                if (match.Success && !ExplicitSource().IsMatch(body))
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

    [GeneratedRegex(@"\b(?:ElementName|RelativeSource|Source)\s*=")]
    private static partial Regex ExplicitSource();
}

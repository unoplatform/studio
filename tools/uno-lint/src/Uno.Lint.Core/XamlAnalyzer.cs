using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Uno.Lint;

/// <summary>A parsed XAML file with the raw text kept for suppression comments.</summary>
public sealed class XamlFile
{
    private XamlFile(string relativePath, string text, XDocument document)
    {
        RelativePath = relativePath;
        Text = text;
        Lines = Suppression.SplitLines(text);
        Document = document;
        IsPaletteFile = FileKinds.IsPaletteFile(relativePath);
        IsIconFile = FileKinds.IsIconFile(relativePath);
        IsThemeOverrideFile = FileKinds.IsThemeOverrideFile(relativePath);
    }

    public string RelativePath { get; }
    public string Text { get; }
    public string[] Lines { get; }
    public XDocument Document { get; }
    public bool IsPaletteFile { get; }
    public bool IsIconFile { get; }
    public bool IsThemeOverrideFile { get; }

    public static bool TryParse(string relativePath, string text, out XamlFile? file)
    {
        try
        {
            var doc = XDocument.Parse(text, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
            file = new XamlFile(relativePath, text, doc);
            return true;
        }
        catch (XmlException)
        {
            file = null;
            return false;
        }
    }
}

public static class XamlAnalyzer
{
    // Color, key and geometry predicates live in RuleLogic so every XAML host shares them.
    private static readonly Regex PathMiniLanguage = new Regex(@"^\s*(?:F[01]\s*)?[Mm]", RegexOptions.Compiled);
    private static readonly Regex ThemeResourceRef = new Regex(@"\{\s*ThemeResource\b", RegexOptions.Compiled);
    private static readonly string[] Shapes = { "Ellipse", "Rectangle", "Line", "Polyline", "Polygon" };

    // {Binding X.Value} / {Binding Path=X.Value ...}: the MVUX state trap. Nested markup ({Binding X.Value, Converter={StaticResource C}}) is tolerated.
    private static readonly Regex BindingMarkup = new Regex(@"\{\s*Binding\b(?<body>(?:[^{}]|\{[^{}]*\})*)\}", RegexOptions.Compiled);
    private static readonly Regex ValuePath = new Regex(@"^\s*(?:Path\s*=\s*)?(?<path>[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)\.Value\b|[\s,]Path\s*=\s*(?<path>[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)\.Value\b", RegexOptions.Compiled);

    public static bool IsColorLiteral(string value) => RuleLogic.IsColorLiteral(value);

    /// <summary>
    /// Every finding for the file, including suppressed ones flagged as such, so the caller can count suppressions.
    /// <paramref name="isEnabled"/> decides which rules run at all.
    /// </summary>
    /// <param name="handledElsewhere">Rules another XAML host evaluates for this file; enabled, but not run here.</param>
    public static IEnumerable<Finding> Analyze(XamlFile file, ProjectContext context, Func<RuleDescriptor, bool> isEnabled, Func<RuleDescriptor, bool>? handledElsewhere = null)
    {
        var findings = new List<Finding>();
        bool Runs(RuleDescriptor rule) => isEnabled(rule) && handledElsewhere?.Invoke(rule) != true;

        // A brush defined once with a literal color is one problem, reported once under HEX (the gating rule) with a
        // note that it is also single-themed; TOKENTHEME takes it only when HEX is not active on this file, whichever
        // host runs HEX.
        var hexActive = isEnabled(Rules.Hex) && !file.IsPaletteFile && !file.IsThemeOverrideFile;
        if (hexActive && Runs(Rules.Hex))
        {
            findings.AddRange(FindHex(file));
        }

        if (Runs(Rules.TokenTheme) && !file.IsThemeOverrideFile)
        {
            findings.AddRange(FindTokenTheme(file, hexActive));
        }

        if (Runs(Rules.Icon) && !file.IsIconFile)
        {
            findings.AddRange(FindIcons(file));
        }

        if (Runs(Rules.Card))
        {
            findings.AddRange(FindCards(file));
        }

        if (Runs(Rules.BackBar))
        {
            findings.AddRange(FindBackBars(file));
        }

        if (Runs(Rules.BindValue))
        {
            findings.AddRange(FindBindValue(file));
        }

        return findings;
    }

    // ---------------------------------------------------------------- HEX

    private static IEnumerable<Finding> FindHex(XamlFile file)
    {
        foreach (var element in file.Document.Descendants())
        {
            // A keyed resource inside ThemeDictionaries is a theme-aware token, which is what the rule asks for; so is
            // anything inside one (a GradientStop in a keyed brush, a Setter in a keyed Style, a property element).
            if (IsThemeDictionaryToken(element))
            {
                continue;
            }

            var elementLine = LineOf(element);
            var singleThemed = SingleThemedBrushKey(element);
            var note = singleThemed != null ? $"; {singleThemed} is also defined once for both themes" : string.Empty;

            foreach (var attribute in element.Attributes())
            {
                if (attribute.IsNamespaceDeclaration || !IsColorLiteral(attribute.Value) || !IsColorSlot(element, attribute))
                {
                    continue;
                }

                var line = LineOf(attribute);
                yield return New(Rules.Hex, file, line, $"literal {attribute.Value.Trim()} on {element.Name.LocalName}.{attribute.Name.LocalName}{note}", elementLine);
            }

            // <Color x:Key="...">#FF1234</Color> or <SolidColorBrush>Red</SolidColorBrush>
            if (!element.HasElements && IsColorLiteral(element.Value) && IsColorElement(element))
            {
                yield return New(Rules.Hex, file, elementLine, $"literal {element.Value.Trim()} in <{element.Name.LocalName}>{note}");
            }
        }
    }

    /// <summary>True for a keyed resource inside ThemeDictionaries, or any element nested in one.</summary>
    private static bool IsThemeDictionaryToken(XElement element)
    {
        var keyedBelow = false;
        for (var e = element; e != null; e = e.Parent)
        {
            if (e.Name.LocalName == "ResourceDictionary.ThemeDictionaries")
            {
                return keyedBelow;
            }

            if (HasKey(e) && e.Name.LocalName != "ResourceDictionary")
            {
                keyedBelow = true;
            }
        }

        return false;
    }

    /// <summary>The key of an app-defined brush that has one value for both themes, or null when the element is not one.</summary>
    private static string? SingleThemedBrushKey(XElement element)
    {
        var key = KeyOf(element);
        if (key == null || !key.EndsWith("Brush", StringComparison.Ordinal) || RuleLogic.IsInvariantKey(key))
        {
            return null;
        }

        var name = element.Name.LocalName;
        if (name == "StaticResource" || name == "ThemeResource" || IsInside(element, "ResourceDictionary.ThemeDictionaries"))
        {
            return null;
        }

        return key;
    }

    /// <summary>A brush whose Color attribute or text content is a literal, which HEX reports when it runs.</summary>
    private static bool CarriesLiteralColor(XElement element)
    {
        var color = element.Attribute("Color")?.Value;
        return (color != null && IsColorLiteral(color)) || (!element.HasElements && IsColorLiteral(element.Value));
    }

    /// <summary>Whether a color-looking attribute sets a color, judged by the property name, the element, or the Setter/animation target.</summary>
    private static bool IsColorSlot(XElement element, XAttribute attribute)
    {
        var property = attribute.Name.LocalName;
        if (property == "Value")
        {
            return TargetPropertyOf(element) is string target && RuleLogic.IsColorName(target);
        }

        return RuleLogic.IsColorName(LastSegment(property)) || RuleLogic.IsColorName(element.Name.LocalName);
    }

    /// <summary>&lt;Color&gt;#..&lt;/Color&gt;, &lt;SolidColorBrush&gt;#..&lt;/SolidColorBrush&gt;, or &lt;Setter.Value&gt;#..&lt;/Setter.Value&gt; on a color property.</summary>
    private static bool IsColorElement(XElement element)
    {
        var name = element.Name.LocalName;
        if (name == "Setter.Value")
        {
            return element.Parent != null && TargetPropertyOf(element.Parent) is string target && RuleLogic.IsColorName(target);
        }

        return RuleLogic.IsColorName(name);
    }

    /// <summary>
    /// The property a Setter or keyframe Value is written to: Setter.Property (styles) or Setter.Target (VisualState
    /// setters, "Hero.Background"), else the nearest Storyboard.TargetProperty.
    /// </summary>
    private static string? TargetPropertyOf(XElement element)
    {
        var setterProperty = element.Name.LocalName == "Setter" ? element.Attribute("Property")?.Value ?? element.Attribute("Target")?.Value : null;
        if (setterProperty != null)
        {
            return LastSegment(setterProperty.Trim('(', ')'));
        }

        for (var e = element; e != null; e = e.Parent)
        {
            var target = e.Attributes().FirstOrDefault(a => a.Name.LocalName == "Storyboard.TargetProperty")?.Value;
            if (target != null)
            {
                return LastSegment(target.Trim('(', ')'));
            }
        }

        return null;
    }

    private static string LastSegment(string name) => name.Substring(name.LastIndexOf('.') + 1);

    // ---------------------------------------------------------------- TOKENTHEME

    private static IEnumerable<Finding> FindTokenTheme(XamlFile file, bool hexActive)
    {
        foreach (var element in file.Document.Descendants())
        {
            // Keyed *Brush outside ThemeDictionaries; <StaticResource x:Key="AliasBrush" ResourceKey="..."/> resolves to
            // whatever its target is and is skipped.
            var key = SingleThemedBrushKey(element);
            if (key == null)
            {
                continue;
            }

            // HEX already reports this brush, with the single-theme note, when it runs on this file.
            if (hexActive && CarriesLiteralColor(element))
            {
                continue;
            }

            // Color="{ThemeResource OnSurfaceColor}" follows the theme even though the brush is defined once.
            var color = element.Attribute("Color")?.Value;
            if (color != null && ThemeResourceRef.IsMatch(color))
            {
                continue;
            }

            // A brush whose colour comes from a nested element that references a ThemeResource also follows the theme.
            if (element.Descendants().Any(d => d.Attributes().Any(a => ThemeResourceRef.IsMatch(a.Value))))
            {
                continue;
            }

            yield return New(Rules.TokenTheme, file, LineOf(element), $"{key} has one value for both themes");
        }
    }

    // ---------------------------------------------------------------- ICON

    private static IEnumerable<Finding> FindIcons(XamlFile file)
    {
        foreach (var element in file.Document.Descendants())
        {
            if (IsInside(element, "ControlTemplate"))
            {
                continue;
            }

            var name = element.Name.LocalName;
            var line = LineOf(element);

            if (name == "Path" || name == "PathIcon")
            {
                var data = element.Attribute("Data")?.Value;
                if (data != null && PathMiniLanguage.IsMatch(data))
                {
                    yield return New(Rules.Icon, file, line, $"inline <{name} Data> path geometry");
                }

                continue;
            }

            if (name == "PathGeometry" || name == "GeometryGroup")
            {
                yield return New(Rules.Icon, file, line, $"inline <{name}>");
                continue;
            }

            if (name == "Viewbox")
            {
                var shapes = element.Descendants().Where(d => Shapes.Contains(d.Name.LocalName)).ToList();
                if (shapes.Count > 0)
                {
                    var kinds = string.Join("/", shapes.Select(s => s.Name.LocalName).Distinct());
                    yield return New(Rules.Icon, file, line, $"Viewbox composed of {shapes.Count} shape(s) ({kinds})");
                }
            }
        }
    }

    // ---------------------------------------------------------------- CARD

    private static IEnumerable<Finding> FindCards(XamlFile file)
    {
        foreach (var border in file.Document.Descendants().Where(e => e.Name.LocalName == "Border"))
        {
            var cornerRadius = border.Attribute("CornerRadius")?.Value;
            if (cornerRadius == null)
            {
                continue;
            }

            // A radius on only some corners ("8,0,0,0") is a swatch, a segment or a joined edge; a zero radius is a plain box.
            if (!TryUniformRadius(cornerRadius, out var radius) || radius == 0)
            {
                continue;
            }

            var hasBackground = border.Attribute("Background") != null;
            if (!hasBackground && border.Attribute("BorderBrush") == null && border.Attribute("Padding") == null)
            {
                continue;
            }

            // Template parts are re-templating, not hand-drawn cards.
            if (IsInside(border, "ControlTemplate"))
            {
                continue;
            }

            // Inside a ShadowContainer a Border without Background only shapes the shadow.
            if (!hasBackground && IsInside(border, "ShadowContainer"))
            {
                continue;
            }

            // Avatars and dots (a square Border whose radius makes it a circle), pills (radius >= half the explicit
            // height) and chips (a large radius around one short piece of content) are not cards; utu:Chip or a
            // PersonPicture is their home, which is a different rule.
            if (!double.IsNaN(radius) && IsCircleOrPill(border, radius))
            {
                continue;
            }

            if (!double.IsNaN(radius) && radius >= RuleLogic.ChipRadius && IsChipContent(border))
            {
                continue;
            }

            var name = border.Attributes().FirstOrDefault(a => a.Name.LocalName == "Name")?.Value;
            var label = name != null ? $"<Border x:Name=\"{name}\">" : "<Border>";
            yield return New(Rules.Card, file, LineOf(border), $"{label} drawn as a card");
        }
    }

    /// <summary>
    /// True for a uniform radius ("8", "8,8,8,8", or a non-numeric value such as a resource reference, reported as NaN);
    /// false when corners differ.
    /// </summary>
    private static bool TryUniformRadius(string cornerRadius, out double radius) => RuleLogic.TryUniformRadius(cornerRadius, out radius);

    private static bool IsCircleOrPill(XElement border, double radius) =>
        RuleLogic.IsCircleOrPill(radius, RuleLogic.ParseNumber(border.Attribute("Width")?.Value), RuleLogic.ParseNumber(border.Attribute("Height")?.Value));

    private static bool IsChipContent(XElement border) =>
        RuleLogic.IsChipContent(border.Elements().Select(e => e.Name.LocalName).Where(n => !n.StartsWith("Border.", StringComparison.Ordinal)).ToList());

    // ---------------------------------------------------------------- BACKBAR

    private static IEnumerable<Finding> FindBackBars(XamlFile file)
    {
        if (file.Document.Descendants().Any(e => e.Name.LocalName == "NavigationBar"))
        {
            yield break;
        }

        foreach (var element in file.Document.Descendants())
        {
            var request = element.Attributes().FirstOrDefault(a => a.Name.LocalName == "Navigation.Request");
            if (request == null || request.Value.Trim() != "-")
            {
                continue;
            }

            yield return New(Rules.BackBar, file, LineOf(request), "hand-built back button; no NavigationBar on this page", LineOf(element));
        }
    }

    // ---------------------------------------------------------------- BINDVALUE

    private static IEnumerable<Finding> FindBindValue(XamlFile file)
    {
        foreach (var element in file.Document.Descendants())
        {
            // Inside an item template the context is an entity, where "Item.Value" may be a real property (KeyValuePair).
            if (IsInside(element, "DataTemplate"))
            {
                continue;
            }

            var elementLine = LineOf(element);
            foreach (var attribute in element.Attributes())
            {
                if (attribute.IsNamespaceDeclaration || attribute.Value.IndexOf(".Value", StringComparison.Ordinal) < 0)
                {
                    continue;
                }

                foreach (Match binding in BindingMarkup.Matches(attribute.Value))
                {
                    var m = ValuePath.Match(binding.Groups["body"].Value);
                    if (!m.Success)
                    {
                        continue;
                    }

                    var path = m.Groups["path"].Value;
                    yield return New(Rules.BindValue, file, LineOf(attribute), $"{{Binding {path}.Value}} on {element.Name.LocalName}.{attribute.Name.LocalName}; bind {path} itself", elementLine);
                }
            }
        }
    }

    // ---------------------------------------------------------------- info stats

    public static void CollectInfo(XamlFile file, InfoStats stats, HashSet<string> definedBrushKeys, HashSet<string> referencedBrushKeys)
    {
        foreach (var element in file.Document.Descendants())
        {
            switch (element.Name.LocalName)
            {
                case "AdaptiveTrigger":
                    stats.AdaptiveTriggers++;
                    break;
                case "ControlTemplate":
                    stats.ControlTemplates++;
                    break;
            }

            var key = KeyOf(element);
            if (key != null && key.EndsWith("Brush", StringComparison.Ordinal))
            {
                definedBrushKeys.Add(key);
            }
        }

        stats.ResponsiveExtensions += Regex.Matches(file.Text, @"\{\s*utu:Responsive\b").Count;

        if (!file.IsPaletteFile)
        {
            foreach (Match m in Regex.Matches(file.Text, @"\{(?:ThemeResource|StaticResource)\s+([A-Za-z0-9_.]+Brush)\}"))
            {
                referencedBrushKeys.Add(m.Groups[1].Value);
                stats.TokenReferences++;
            }
        }
    }

    // ---------------------------------------------------------------- helpers

    /// <summary>A finding at <paramref name="line"/>, suppressed when an allow comment covers that line or any extra anchor (e.g. the element's start line).</summary>
    private static Finding New(RuleDescriptor rule, XamlFile file, int line, string message, params int[] extraAnchors)
    {
        var anchors = new int[extraAnchors.Length + 1];
        anchors[0] = line;
        Array.Copy(extraAnchors, 0, anchors, 1, extraAnchors.Length);
        return new Finding(rule, file.RelativePath, line, message, Suppression.IsSuppressed(file.Lines, rule, anchors));
    }

    private static bool IsInside(XElement element, string ancestorLocalName)
    {
        for (var parent = element.Parent; parent != null; parent = parent.Parent)
        {
            if (parent.Name.LocalName == ancestorLocalName)
            {
                return true;
            }
        }

        return false;
    }

    private static string? KeyOf(XElement element) =>
        element.Attributes().FirstOrDefault(a => a.Name.LocalName == "Key")?.Value;

    private static bool HasKey(XElement element) => KeyOf(element) != null;

    private static int LineOf(XObject node) => ((IXmlLineInfo)node).LineNumber;
}

using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Uno.Lint;

/// <summary>A parsed XAML file with the raw text kept for suppression comments.</summary>
public sealed class XamlFile
{
    // Matched against the file name's words ("BrandColors.Dark.xaml" -> "Brand Colors Dark"), so a page whose name only
    // contains the word is not exempt: ColorPickerPage, AuthTokenPage and LexiconPage are linted like any page.
    private static readonly Regex PaletteName = new Regex(@"(?i)\b(palettes?|colou?rs|tokens)\b", RegexOptions.Compiled);
    private static readonly Regex IconName = new Regex(@"(?i)\bicons\b", RegexOptions.Compiled);
    private static readonly Regex ThemeOverrideName = new Regex(@"(?i)\bcolou?r( palette)? override\b", RegexOptions.Compiled);

    private XamlFile(string relativePath, string text, XDocument document)
    {
        RelativePath = relativePath;
        Text = text;
        Lines = Suppression.SplitLines(text);
        Document = document;
        var words = FileNameWords(relativePath);
        IsPaletteFile = PaletteName.IsMatch(words);
        IsIconFile = IconName.IsMatch(words);
        IsThemeOverrideFile = ThemeOverrideName.IsMatch(words);
    }

    public string RelativePath { get; }
    public string Text { get; }
    public string[] Lines { get; }
    public XDocument Document { get; }
    public bool IsPaletteFile { get; }
    public bool IsIconFile { get; }
    public bool IsThemeOverrideFile { get; }

    private static string FileNameWords(string relativePath)
    {
        var stem = Path.GetFileNameWithoutExtension(relativePath);
        return string.Join(" ", stem.Split('.', '-', ' ').Select(CSharpAnalyzer.SplitWords));
    }

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
    private static readonly Regex HexLiteral = new Regex(@"^\s*#(?:[0-9A-Fa-f]{8}|[0-9A-Fa-f]{6}|[0-9A-Fa-f]{3,4})\s*$", RegexOptions.Compiled);

    // A property, or an element, that holds a color: Background, BorderBrush, Fill, Stroke, Tint, Color, SolidColorBrush,
    // GradientStop, ColorAnimation... Text="#404" and Tag="#123" are text that happens to look like hex.
    private static readonly Regex ColorName = new Regex(@"(?i)colou?r|brush|background|foreground|fill|stroke|tint|gradientstop", RegexOptions.Compiled);
    private static readonly Regex PathMiniLanguage = new Regex(@"^\s*(?:F[01]\s*)?[Mm]", RegexOptions.Compiled);
    private static readonly Regex InvariantKey = new Regex(@"(?i)invariant|ondark|onlight|fixed", RegexOptions.Compiled);
    private static readonly Regex ThemeResourceRef = new Regex(@"\{\s*ThemeResource\b", RegexOptions.Compiled);
    private static readonly string[] Shapes = { "Ellipse", "Rectangle", "Line", "Polyline", "Polygon" };
    private static readonly string[] ChipContent = { "TextBlock", "FontIcon", "SymbolIcon", "PathIcon", "Run" };

    // {Binding X.Value} / {Binding Path=X.Value ...}: the MVUX state trap. Nested markup ({Binding X.Value, Converter={StaticResource C}}) is tolerated.
    private static readonly Regex BindingMarkup = new Regex(@"\{\s*Binding\b(?<body>(?:[^{}]|\{[^{}]*\})*)\}", RegexOptions.Compiled);
    private static readonly Regex ValuePath = new Regex(@"^\s*(?:Path\s*=\s*)?(?<path>[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)\.Value\b|[\s,]Path\s*=\s*(?<path>[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)\.Value\b", RegexOptions.Compiled);

    /// <summary>The WinUI named colors (Microsoft.UI.Colors), minus Transparent, which is a legitimate "no color".</summary>
    private static readonly HashSet<string> NamedColors = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "AliceBlue", "AntiqueWhite", "Aqua", "Aquamarine", "Azure", "Beige", "Bisque", "Black", "BlanchedAlmond", "Blue",
        "BlueViolet", "Brown", "BurlyWood", "CadetBlue", "Chartreuse", "Chocolate", "Coral", "CornflowerBlue", "Cornsilk",
        "Crimson", "Cyan", "DarkBlue", "DarkCyan", "DarkGoldenrod", "DarkGray", "DarkGreen", "DarkKhaki", "DarkMagenta",
        "DarkOliveGreen", "DarkOrange", "DarkOrchid", "DarkRed", "DarkSalmon", "DarkSeaGreen", "DarkSlateBlue", "DarkSlateGray",
        "DarkTurquoise", "DarkViolet", "DeepPink", "DeepSkyBlue", "DimGray", "DodgerBlue", "Firebrick", "FloralWhite",
        "ForestGreen", "Fuchsia", "Gainsboro", "GhostWhite", "Gold", "Goldenrod", "Gray", "Green", "GreenYellow", "Honeydew",
        "HotPink", "IndianRed", "Indigo", "Ivory", "Khaki", "Lavender", "LavenderBlush", "LawnGreen", "LemonChiffon",
        "LightBlue", "LightCoral", "LightCyan", "LightGoldenrodYellow", "LightGray", "LightGreen", "LightPink", "LightSalmon",
        "LightSeaGreen", "LightSkyBlue", "LightSlateGray", "LightSteelBlue", "LightYellow", "Lime", "LimeGreen", "Linen",
        "Magenta", "Maroon", "MediumAquamarine", "MediumBlue", "MediumOrchid", "MediumPurple", "MediumSeaGreen",
        "MediumSlateBlue", "MediumSpringGreen", "MediumTurquoise", "MediumVioletRed", "MidnightBlue", "MintCream", "MistyRose",
        "Moccasin", "NavajoWhite", "Navy", "OldLace", "Olive", "OliveDrab", "Orange", "OrangeRed", "Orchid", "PaleGoldenrod",
        "PaleGreen", "PaleTurquoise", "PaleVioletRed", "PapayaWhip", "PeachPuff", "Peru", "Pink", "Plum", "PowderBlue", "Purple",
        "Red", "RosyBrown", "RoyalBlue", "SaddleBrown", "Salmon", "SandyBrown", "SeaGreen", "SeaShell", "Sienna", "Silver",
        "SkyBlue", "SlateBlue", "SlateGray", "Snow", "SpringGreen", "SteelBlue", "Tan", "Teal", "Thistle", "Tomato", "Turquoise",
        "Violet", "Wheat", "White", "WhiteSmoke", "Yellow", "YellowGreen",
    };

    public static bool IsColorLiteral(string value) => HexLiteral.IsMatch(value) || NamedColors.Contains(value.Trim());

    /// <summary>
    /// Every finding for the file, including suppressed ones flagged as such, so the caller can count suppressions.
    /// <paramref name="isEnabled"/> decides which rules run at all.
    /// </summary>
    public static IEnumerable<Finding> Analyze(XamlFile file, ProjectContext context, Func<RuleDescriptor, bool> isEnabled)
    {
        var findings = new List<Finding>();

        if (isEnabled(Rules.Hex) && !file.IsPaletteFile && !file.IsThemeOverrideFile)
        {
            findings.AddRange(FindHex(file));
        }

        if (isEnabled(Rules.TokenTheme) && !file.IsThemeOverrideFile)
        {
            findings.AddRange(FindTokenTheme(file));
        }

        if (isEnabled(Rules.Icon) && !file.IsIconFile)
        {
            findings.AddRange(FindIcons(file));
        }

        if (isEnabled(Rules.Card))
        {
            findings.AddRange(FindCards(file));
        }

        if (isEnabled(Rules.BackBar))
        {
            findings.AddRange(FindBackBars(file));
        }

        if (isEnabled(Rules.BindValue))
        {
            findings.AddRange(FindBindValue(file));
        }

        return MergeHexIntoTokenTheme(findings);
    }

    /// <summary>
    /// A brush defined once with a hex literal is one problem, not two: keep the TOKENTHEME finding and fold the
    /// literal into its message instead of reporting HEX on the same line as well.
    /// </summary>
    private static List<Finding> MergeHexIntoTokenTheme(List<Finding> findings)
    {
        var tokenLines = new Dictionary<int, Finding>();
        foreach (var f in findings.Where(f => f.Rule == Rules.TokenTheme))
        {
            tokenLines[f.Line] = f;
        }

        if (tokenLines.Count == 0)
        {
            return findings;
        }

        var merged = new List<Finding>(findings.Count);
        foreach (var f in findings)
        {
            if (f.Rule == Rules.Hex && tokenLines.TryGetValue(f.Line, out var token))
            {
                var literal = f.Message.Substring("literal ".Length);
                var space = literal.IndexOf(' ');
                literal = space > 0 ? literal.Substring(0, space) : literal;
                merged.Add(new Finding(Rules.TokenTheme, token.File, token.Line, token.Message + $" (color literal {literal})", token.Suppressed));
                tokenLines.Remove(f.Line);
                continue;
            }

            if (f.Rule == Rules.TokenTheme && !tokenLines.ContainsKey(f.Line))
            {
                continue; // already emitted merged
            }

            merged.Add(f);
        }

        return merged;
    }

    // ---------------------------------------------------------------- HEX

    private static IEnumerable<Finding> FindHex(XamlFile file)
    {
        foreach (var element in file.Document.Descendants())
        {
            // A keyed resource inside ThemeDictionaries is a theme-aware token, which is what the rule asks for.
            if (IsInside(element, "ResourceDictionary.ThemeDictionaries") && HasKey(element))
            {
                continue;
            }

            var elementLine = LineOf(element);
            foreach (var attribute in element.Attributes())
            {
                if (attribute.IsNamespaceDeclaration || !IsColorLiteral(attribute.Value) || !IsColorSlot(element, attribute))
                {
                    continue;
                }

                var line = LineOf(attribute);
                yield return New(Rules.Hex, file, line, $"literal {attribute.Value.Trim()} on {element.Name.LocalName}.{attribute.Name.LocalName}", elementLine);
            }

            // <Color x:Key="...">#FF1234</Color> or <SolidColorBrush>Red</SolidColorBrush>
            if (!element.HasElements && IsColorLiteral(element.Value) && IsColorElement(element))
            {
                yield return New(Rules.Hex, file, elementLine, $"literal {element.Value.Trim()} in <{element.Name.LocalName}>");
            }
        }
    }

    /// <summary>Whether a color-looking attribute sets a color, judged by the property name, the element, or the Setter/animation target.</summary>
    private static bool IsColorSlot(XElement element, XAttribute attribute)
    {
        var property = attribute.Name.LocalName;
        if (property == "Value")
        {
            return TargetPropertyOf(element) is string target && ColorName.IsMatch(target);
        }

        return ColorName.IsMatch(LastSegment(property)) || ColorName.IsMatch(element.Name.LocalName);
    }

    /// <summary>&lt;Color&gt;#..&lt;/Color&gt;, &lt;SolidColorBrush&gt;#..&lt;/SolidColorBrush&gt;, or &lt;Setter.Value&gt;#..&lt;/Setter.Value&gt; on a color property.</summary>
    private static bool IsColorElement(XElement element)
    {
        var name = element.Name.LocalName;
        if (name == "Setter.Value")
        {
            return element.Parent != null && TargetPropertyOf(element.Parent) is string target && ColorName.IsMatch(target);
        }

        return ColorName.IsMatch(name);
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

    private static IEnumerable<Finding> FindTokenTheme(XamlFile file)
    {
        foreach (var element in file.Document.Descendants())
        {
            var key = KeyOf(element);
            if (key == null || !key.EndsWith("Brush", StringComparison.Ordinal) || InvariantKey.IsMatch(key))
            {
                continue;
            }

            if (IsInside(element, "ResourceDictionary.ThemeDictionaries"))
            {
                continue;
            }

            // <StaticResource x:Key="AccentAliasBrush" ResourceKey="PrimaryBrush"/> resolves to whatever its target is.
            var name = element.Name.LocalName;
            if (name == "StaticResource" || name == "ThemeResource")
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

            if (!double.IsNaN(radius) && radius >= 12 && IsChipContent(border))
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
    private static bool TryUniformRadius(string cornerRadius, out double radius)
    {
        radius = double.NaN;
        var trimmed = cornerRadius.Trim();
        if (trimmed.StartsWith("{", StringComparison.Ordinal))
        {
            return true; // "{StaticResource CardCornerRadius}": uniform, size unknown
        }

        var parts = trimmed.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        var values = new List<double>();
        foreach (var part in parts)
        {
            if (!double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
            {
                return parts.Length == 1; // a single non-numeric token: treat as uniform, size unknown
            }

            values.Add(v);
        }

        if (values.Distinct().Count() > 1)
        {
            return false;
        }

        radius = values[0];
        return true;
    }

    private static bool IsCircleOrPill(XElement border, double radius)
    {
        var width = Num(border.Attribute("Width"));
        var height = Num(border.Attribute("Height"));

        if (width.HasValue && height.HasValue && Math.Abs(width.Value - height.Value) < 0.5 && radius * 2 >= width.Value - 0.5)
        {
            return true; // circle: avatar, dot, badge
        }

        return height.HasValue && radius * 2 >= height.Value - 0.5; // pill
    }

    private static bool IsChipContent(XElement border)
    {
        var children = border.Elements().Where(e => !e.Name.LocalName.StartsWith("Border.", StringComparison.Ordinal)).ToList();
        return children.Count == 0 || (children.Count == 1 && ChipContent.Contains(children[0].Name.LocalName));
    }

    private static double? Num(XAttribute? attribute) =>
        attribute != null && double.TryParse(attribute.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : (double?)null;

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

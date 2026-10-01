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
                merged.Add(new Finding(Rules.TokenTheme, token.File, token.Line, token.Message + $" (hex literal {literal})"));
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

            foreach (var attribute in element.Attributes())
            {
                if (attribute.IsNamespaceDeclaration || !HexLiteral.IsMatch(attribute.Value) || !IsColorSlot(element, attribute))
                {
                    continue;
                }

                var line = LineOf(attribute);
                if (Suppression.IsSuppressed(file.Lines, line, Rules.Hex))
                {
                    continue;
                }

                yield return new Finding(Rules.Hex, file.RelativePath, line, $"literal {attribute.Value.Trim()} on {element.Name.LocalName}.{attribute.Name.LocalName}");
            }

            // <Color x:Key="...">#FF1234</Color> or <SolidColorBrush>#FF1234</SolidColorBrush>
            if (!element.HasElements && HexLiteral.IsMatch(element.Value) && IsColorElement(element))
            {
                var line = LineOf(element);
                if (!Suppression.IsSuppressed(file.Lines, line, Rules.Hex))
                {
                    yield return new Finding(Rules.Hex, file.RelativePath, line, $"literal {element.Value.Trim()} in <{element.Name.LocalName}>");
                }
            }
        }
    }

    /// <summary>Whether a hex-looking attribute sets a color, judged by the property name, the element, or the Setter/animation target.</summary>
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

            var line = LineOf(element);
            if (Suppression.IsSuppressed(file.Lines, line, Rules.TokenTheme))
            {
                continue;
            }

            yield return new Finding(Rules.TokenTheme, file.RelativePath, line, $"{key} has one value for both themes");
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
                if (data != null && PathMiniLanguage.IsMatch(data) && !Suppression.IsSuppressed(file.Lines, line, Rules.Icon))
                {
                    yield return new Finding(Rules.Icon, file.RelativePath, line, $"inline <{name} Data> path geometry");
                }

                continue;
            }

            if (name == "PathGeometry" || name == "GeometryGroup")
            {
                if (!Suppression.IsSuppressed(file.Lines, line, Rules.Icon))
                {
                    yield return new Finding(Rules.Icon, file.RelativePath, line, $"inline <{name}>");
                }

                continue;
            }

            if (name == "Viewbox")
            {
                var shapes = element.Descendants().Where(d => Shapes.Contains(d.Name.LocalName)).ToList();
                if (shapes.Count > 0 && !Suppression.IsSuppressed(file.Lines, line, Rules.Icon))
                {
                    var kinds = string.Join("/", shapes.Select(s => s.Name.LocalName).Distinct());
                    yield return new Finding(Rules.Icon, file.RelativePath, line, $"Viewbox composed of {shapes.Count} shape(s) ({kinds})");
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

            // A radius on only some corners ("8,0,0,0") is a swatch, a segment or a joined edge, not a card.
            if (HasZeroCorner(cornerRadius))
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

            var line = LineOf(border);
            if (Suppression.IsSuppressed(file.Lines, line, Rules.Card))
            {
                continue;
            }

            var name = border.Attributes().FirstOrDefault(a => a.Name.LocalName == "Name")?.Value;
            var label = name != null ? $"<Border x:Name=\"{name}\">" : "<Border>";
            yield return new Finding(Rules.Card, file.RelativePath, line, $"{label} drawn as a card");
        }
    }

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

            var line = LineOf(request);
            if (Suppression.IsSuppressed(file.Lines, line, Rules.BackBar))
            {
                continue;
            }

            yield return new Finding(Rules.BackBar, file.RelativePath, line, "hand-built back button; no NavigationBar on this page");
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

    private static bool HasZeroCorner(string cornerRadius)
    {
        var parts = cornerRadius.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return false;
        }

        return parts.Any(p => double.TryParse(p, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) && v == 0);
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

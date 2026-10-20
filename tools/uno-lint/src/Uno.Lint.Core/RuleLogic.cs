using System.Globalization;
using System.Text.RegularExpressions;

namespace Uno.Lint;

/// <summary>
/// The rule predicates, independent of any document model: the XAML rules on Uno.XamlParser and the C# rules call
/// these, and so would a future analyzer, so a rule means the same thing whichever host runs it. The regular
/// expressions are interpreted, not compiled: compiling costs more startup than a hook call saves.
/// </summary>
public static class RuleLogic
{
    private static readonly Regex HexLiteral = new Regex(@"^\s*#(?:[0-9A-Fa-f]{8}|[0-9A-Fa-f]{6}|[0-9A-Fa-f]{3,4})\s*$");

    // A property, or an element, that holds a color: Background, BorderBrush, Fill, Stroke, Tint, Color, SolidColorBrush,
    // GradientStop, ColorAnimation... Text="#404" and Tag="#123" are text that happens to look like hex.
    private static readonly Regex ColorName = new Regex(@"(?i)colou?r|brush|background|foreground|fill|stroke|tint|gradientstop");
    private static readonly Regex InvariantKey = new Regex(@"(?i)invariant|ondark|onlight|fixed");
    private static readonly HashSet<string> ChipContent = new HashSet<string>(StringComparer.Ordinal) { "TextBlock", "FontIcon", "SymbolIcon", "PathIcon", "Run" };

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

    /// <summary>A hex literal or a WinUI named color (not Transparent).</summary>
    public static bool IsColorLiteral(string? value) => value != null && (HexLiteral.IsMatch(value) || NamedColors.Contains(value.Trim()));

    /// <summary>A property or element name that holds a color (Background, BorderBrush, SolidColorBrush, GradientStop...).</summary>
    public static bool IsColorName(string name) => ColorName.IsMatch(name);

    /// <summary>Properties of a color animation or keyframe that hold a color, though their names do not say so.</summary>
    private static readonly HashSet<string> ColorElementSlots = new HashSet<string>(StringComparer.Ordinal) { "To", "From", "By", "Value" };

    /// <summary>
    /// A property that holds a color: one named for a color (Background, BorderBrush, Fill, TintColor...), or the value of a
    /// color animation or keyframe (<c>&lt;ColorAnimation To="..."/&gt;</c>).
    /// </summary>
    public static bool IsColorProperty(string elementLocalName, string propertyName) =>
        IsColorName(propertyName) || (ColorElementSlots.Contains(propertyName) && IsColorName(elementLocalName));

    /// <summary>A brush key that declares its ground fixed on purpose (*Invariant, *OnDark, *OnLight, *Fixed).</summary>
    public static bool IsInvariantKey(string key) => InvariantKey.IsMatch(key);

    /// <summary>The part after the last dot: "Border.Background" -> "Background", "(Border.Background)" -> "Background".</summary>
    public static string LastSegment(string name)
    {
        var trimmed = name.Trim('(', ')');
        return trimmed.Substring(trimmed.LastIndexOf('.') + 1);
    }

    /// <summary>
    /// True for a uniform radius ("8", "8,8,8,8", or a non-numeric value such as a resource reference, reported as NaN);
    /// false when corners differ.
    /// </summary>
    public static bool TryUniformRadius(string cornerRadius, out double radius)
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

    /// <summary>A circle (equal explicit Width/Height, radius at least half) or a pill (radius at least half an explicit Height).</summary>
    public static bool IsCircleOrPill(double radius, double? width, double? height)
    {
        if (width.HasValue && height.HasValue && Math.Abs(width.Value - height.Value) < 0.5 && radius * 2 >= width.Value - 0.5)
        {
            return true; // circle: avatar, dot, badge
        }

        return height.HasValue && radius * 2 >= height.Value - 0.5; // pill
    }

    /// <summary>No content, or a single TextBlock/icon: the content of a chip, not a card.</summary>
    public static bool IsChipContent(IReadOnlyList<string> childLocalNames) =>
        childLocalNames.Count == 0 || (childLocalNames.Count == 1 && ChipContent.Contains(childLocalNames[0]));

    /// <summary>Radius from which a Border around chip content is treated as a chip.</summary>
    public const double ChipRadius = 12;

    public static double? ParseNumber(string? value) =>
        value != null && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : (double?)null;
}

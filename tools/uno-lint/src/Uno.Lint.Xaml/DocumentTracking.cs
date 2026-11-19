using Uno.XamlParser.Stage0;
using Uno.XamlParser.Stage1;
using Uno.XamlParser.Stage1.Nodes;

namespace Uno.Lint.Xaml;

/// <summary>What the rules know about an open element: its start node and what has been seen inside it so far.</summary>
internal sealed class ElementFrame
{
    public ElementFrame(XamlElementNode start) => Start = start;

    public XamlElementNode Start { get; }

    /// <summary>Local names of the direct child elements, property elements included.</summary>
    public List<string> Children { get; } = new();

    /// <summary>
    /// Local names of the content children: direct children that are not property elements, plus the children of a
    /// <c>.Child</c> or <c>.Content</c> property element (<c>&lt;Border.Child&gt;&lt;StackPanel/&gt;</c>).
    /// </summary>
    public List<string> Content { get; } = new();

    /// <summary>The element's own <c>.Background</c> property element holds an ImageBrush: the element clips an image.</summary>
    public bool HasImageBackground { get; set; }

    /// <summary>Shapes (Ellipse, Rectangle...) anywhere below, in document order.</summary>
    public List<string> ShapesBelow { get; } = new();

    /// <summary>A descendant attribute references a {ThemeResource}.</summary>
    public bool ThemeResourceBelow { get; set; }

    /// <summary>For a Style: the properties its direct Setters set, with the Value attribute (null when set by a property element).</summary>
    public Dictionary<string, string?> Setters { get; } = new(StringComparer.Ordinal);

    /// <summary>Direct text content, known once the end tag is reached.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// HEX hits on a brush that may be single-themed, held until its end tag: only then is it known whether the brush
    /// follows the theme, which decides the single-theme note.
    /// </summary>
    public List<(string Message, XamlLineInfo At)> PendingHex { get; } = new();
}

/// <summary>
/// Per-document state shared by the rules, stored on <see cref="XamlSemanticFixerContext.Items"/> (per stream) as the
/// parser's streaming rules require: only the path of open elements is kept, never the whole tree.
/// </summary>
internal sealed class LintDocument
{
    private static readonly object ItemKey = new();

    private LintDocument(string? sourceName)
    {
        var name = sourceName ?? string.Empty;
        IsPaletteFile = FileKinds.IsPaletteFile(name);
        IsIconFile = FileKinds.IsIconFile(name);
        IsThemeOverrideFile = FileKinds.IsThemeOverrideFile(name);
    }

    public bool IsPaletteFile { get; }
    public bool IsIconFile { get; }
    public bool IsThemeOverrideFile { get; }

    public Stack<ElementFrame> Open { get; } = new();

    /// <summary>
    /// True when the current start call is the collector re-running fixers on a parent after folding a property
    /// element into it. Rules ignore these: the element was already checked at its real start tag.
    /// </summary>
    public bool IsRefix { get; set; }

    /// <summary>The element whose end tag is being processed; popped at the next event so every rule sees it.</summary>
    public ElementFrame? Closing { get; set; }

    public bool SawNavigationBar { get; set; }

    /// <summary>
    /// Hand-built back buttons waiting for the end of the root element to know whether a NavigationBar exists: a
    /// <c>Navigation.Request="-"</c>, or a button showing a back icon (<see cref="BackButton.Icon"/>).
    /// </summary>
    public List<BackButton> PendingBackButtons { get; } = new();

    /// <summary>
    /// The current root element is a ResourceDictionary or the Application: a resource file, where a ThemeDictionaries
    /// entry is a theme token. A page's own ThemeDictionaries holds overrides, which are tokens only if they reference one.
    /// </summary>
    public bool InResourceFile => Open.Count > 0 && XamlNodes.LocalName(Open.Last().Start) is "ResourceDictionary" or "Application";

    public static LintDocument Of(XamlSemanticFixerContext context)
    {
        if (!context.Items.TryGetValue(ItemKey, out var state))
        {
            state = new LintDocument(context.StreamContext.SourceName);
            context.Items[ItemKey] = state;
        }

        return (LintDocument)state;
    }
}

/// <summary>A hand-built back button: where to report it, and whether it was recognized by its icon.</summary>
internal readonly record struct BackButton(XamlLineInfo At, bool Icon);

/// <summary>
/// Maintains <see cref="LintDocument"/>. Registered first among the uno-lint fixers so the rules that follow see the
/// state for the current event.
/// </summary>
public sealed class DocumentTracker : IXamlSemanticFixer
{
    private static readonly HashSet<string> Shapes = new(StringComparer.Ordinal) { "Ellipse", "Rectangle", "Line", "Polyline", "Polygon" };

    public ValueTask<SemanticFixResult> OnElementAsync(XamlElementNode element, XamlSemanticFixerContext context, CancellationToken ct)
    {
        var document = LintDocument.Of(context);

        // The previous end tag's frame stays on the stack until now so every rule could read it.
        if (document.Closing != null)
        {
            document.Open.Pop();
            document.Closing = null;
        }

        if (context.IsProcessingElementStart)
        {
            document.IsRefix = context.IsReapplyingToParent;
            if (document.IsRefix)
            {
                return UnoLintXaml.None;
            }

            var name = XamlNodes.LocalName(element);
            var referencesTheme = element.Properties.Any(p => p.RawValue is { } raw && UnoLintXaml.ReferencesThemeResource(raw));
            foreach (var ancestor in document.Open)
            {
                ancestor.ThemeResourceBelow |= referencesTheme;
                if (Shapes.Contains(name))
                {
                    ancestor.ShapesBelow.Add(name);
                }
            }

            if (document.Open.Count > 0)
            {
                var parent = document.Open.Peek();
                parent.Children.Add(name);
                if (name == "Setter" && XamlNodes.LocalName(parent.Start) == "Style" && XamlNodes.Attribute(element, "Property") is { } property)
                {
                    parent.Setters[RuleLogic.LastSegment(property)] = XamlNodes.Attribute(element, "Value");
                }

                if (!name.Contains('.', StringComparison.Ordinal))
                {
                    var parentName = XamlNodes.LocalName(parent.Start);
                    var isContentProperty = parentName.EndsWith(".Child", StringComparison.Ordinal) || parentName.EndsWith(".Content", StringComparison.Ordinal);
                    var owner = document.Open.Count > 1 ? document.Open.ElementAt(1) : null;
                    (isContentProperty && owner != null ? owner : parent).Content.Add(name);
                    if (owner != null && name == "ImageBrush" && parentName.EndsWith(".Background", StringComparison.Ordinal))
                    {
                        owner.HasImageBackground = true;
                    }
                }
            }

            document.SawNavigationBar |= name == "NavigationBar";
            document.Open.Push(new ElementFrame(element));
        }
        else
        {
            document.IsRefix = false;
            if (document.Open.Count > 0)
            {
                var frame = document.Open.Peek();
                frame.Text = context.CurrentElementText ?? string.Empty;
                document.Closing = frame;
            }
        }

        return UnoLintXaml.None;
    }
}

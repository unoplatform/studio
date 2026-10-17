using System.Text;
using System.Text.RegularExpressions;
using Uno.XamlParser.Stage0;
using Uno.XamlParser.Stage1;
using Uno.XamlParser.Stage1.Nodes;

namespace Uno.Lint.Xaml;

/// <summary>
/// Captures each element's direct text content. Stage-1 fixers never see text (only folded property-element values),
/// so <c>&lt;Color&gt;#7A67F8&lt;/Color&gt;</c> would be invisible to the rules. Registered through
/// <c>XamlStreamPipelineOptions.VisitorTypes</c>, which run before the semantic collector, so the text of an element
/// is known by the time the rules see its end tag. State is reset per stream; only the open-element path is kept.
/// </summary>
public sealed class ElementTextCapture : IXamlStreamVisitor
{
    private readonly Stack<(XamlLineInfo Start, StringBuilder Text)> _open = new();
    private (XamlLineInfo Start, string Text)? _closed;

    public ValueTask OnStreamStartedAsync(XamlStreamContext context, CancellationToken ct)
    {
        _open.Clear();
        _closed = null;
        return ValueTask.CompletedTask;
    }

    public ValueTask OnElementStartAsync(in XamlElementToken element, CancellationToken ct)
    {
        _open.Push((element.LineInfo, new StringBuilder()));
        return ValueTask.CompletedTask;
    }

    public ValueTask OnTextAsync(in XamlTextToken text, CancellationToken ct)
    {
        if (_open.Count > 0 && text.Kind is XamlTextKind.Text or XamlTextKind.CData or XamlTextKind.Whitespace or XamlTextKind.SignificantWhitespace)
        {
            _open.Peek().Text.Append(text.Text);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask OnElementEndAsync(in XamlElementToken element, CancellationToken ct)
    {
        if (_open.Count > 0)
        {
            var (start, text) = _open.Pop();
            _closed = (start, text.ToString());
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>Text of the element that just closed, when it is the element that started at <paramref name="start"/>.</summary>
    internal string? ClosedText(XamlLineInfo start) => _closed is { } closed && closed.Start == start ? closed.Text : null;
}

/// <summary>What the rules know about an open element: its start node and what has been seen inside it so far.</summary>
internal sealed class ElementFrame
{
    public ElementFrame(XamlElementNode start) => Start = start;

    public XamlElementNode Start { get; }

    /// <summary>Local names of the direct child elements, property elements included.</summary>
    public List<string> Children { get; } = new();

    /// <summary>Shapes (Ellipse, Rectangle...) anywhere below, in document order.</summary>
    public List<string> ShapesBelow { get; } = new();

    /// <summary>A descendant attribute references a {ThemeResource}.</summary>
    public bool ThemeResourceBelow { get; set; }

    /// <summary>Direct text content, known once the end tag is reached.</summary>
    public string Text { get; set; } = string.Empty;
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

    /// <summary>Start positions already seen, to tell a real start tag from the collector re-running fixers on a parent.</summary>
    public HashSet<(int Line, int Column)> Started { get; } = new();

    /// <summary>
    /// True when the current start call is the collector re-running fixers on a parent after folding a property
    /// element into it. Rules ignore these: the element was already checked at its real start tag.
    /// </summary>
    public bool IsRefix { get; set; }

    /// <summary>The element whose end tag is being processed; popped at the next event so every rule sees it.</summary>
    public ElementFrame? Closing { get; set; }

    public bool SawNavigationBar { get; set; }

    /// <summary>Back-navigation requests waiting for the end of the document to know whether a NavigationBar exists.</summary>
    public List<XamlLineInfo> PendingBackRequests { get; } = new();

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

/// <summary>
/// Maintains <see cref="LintDocument"/>. Registered first among the uno-lint fixers so the rules that follow see the
/// state for the current event.
/// </summary>
public sealed class DocumentTracker : IXamlSemanticFixer
{
    private static readonly Regex ThemeResourceRef = new(@"\{\s*ThemeResource\b", RegexOptions.Compiled);
    private static readonly HashSet<string> Shapes = new(StringComparer.Ordinal) { "Ellipse", "Rectangle", "Line", "Polyline", "Polygon" };

    private readonly ElementTextCapture _text;

    public DocumentTracker(ElementTextCapture text) => _text = text;

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
            document.IsRefix = !document.Started.Add((element.LineInfo.LineNumber, element.LineInfo.LinePosition));
            if (document.IsRefix)
            {
                return ValueTask.FromResult(SemanticFixResult.None);
            }

            var name = XamlNodes.LocalName(element);
            var referencesTheme = element.Properties.Any(p => p.RawValue != null && ThemeResourceRef.IsMatch(p.RawValue));
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
                document.Open.Peek().Children.Add(name);
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
                frame.Text = _text.ClosedText(frame.Start.LineInfo) ?? string.Empty;
                document.Closing = frame;
            }
        }

        return ValueTask.FromResult(SemanticFixResult.None);
    }
}

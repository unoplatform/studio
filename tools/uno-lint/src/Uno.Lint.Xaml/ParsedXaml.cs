using Uno.XamlParser.Stage0;
using Uno.XamlParser.Stage1.Nodes;

namespace Uno.Lint.Xaml;

/// <summary>
/// Read-only view over Uno.XamlParser's Stage-1 output with the few navigation helpers the rules need: parent and
/// children by index, attribute lookup by name, element text content. This replaces the XDocument helpers
/// (IsInside, KeyOf, TargetPropertyOf) of the built-in host.
/// </summary>
internal sealed class ParsedXaml
{
    private readonly List<int>[] _children;

    public ParsedXaml(string relativePath, string[] lines, IReadOnlyList<XamlElementNode> elements, IReadOnlyList<XamlTextNode> texts)
    {
        RelativePath = relativePath;
        Lines = lines;
        Elements = elements;
        Texts = texts;
        IsPaletteFile = FileKinds.IsPaletteFile(relativePath);
        IsThemeOverrideFile = FileKinds.IsThemeOverrideFile(relativePath);

        _children = new List<int>[elements.Count];
        for (var i = 0; i < elements.Count; i++)
        {
            _children[i] = new List<int>();
        }

        for (var i = 0; i < elements.Count; i++)
        {
            if (elements[i].ParentIndex is int parent && parent >= 0 && parent < elements.Count)
            {
                _children[parent].Add(i);
            }
        }
    }

    public string RelativePath { get; }
    public string[] Lines { get; }
    public IReadOnlyList<XamlElementNode> Elements { get; }
    public IReadOnlyList<XamlTextNode> Texts { get; }
    public bool IsPaletteFile { get; }
    public bool IsThemeOverrideFile { get; }

    public string LocalName(int index) => Elements[index].Name.LocalName;

    public int Line(int index) => Elements[index].LineInfo.LineNumber;

    public IReadOnlyList<int> Children(int index) => _children[index];

    public int? Parent(int index) => Elements[index].ParentIndex;

    public IEnumerable<int> Ancestors(int index)
    {
        for (var p = Parent(index); p is int i; p = Parent(i))
        {
            yield return i;
        }
    }

    public bool IsInside(int index, string ancestorLocalName) => Ancestors(index).Any(a => LocalName(a) == ancestorLocalName);

    /// <summary>
    /// Inline properties written as attributes, named the way XDocument names them ("Background",
    /// "Storyboard.TargetProperty"), without namespace declarations or property-element values.
    /// </summary>
    public IEnumerable<(string Name, XamlPropertyNode Node)> Attributes(int index)
    {
        foreach (var property in Elements[index].Properties)
        {
            if (property.Kind == XamlPropertyKind.PropertyElement || IsNamespaceDeclaration(property))
            {
                continue;
            }

            yield return (AttributeName(property), property);
        }
    }

    /// <summary>Value of an unprefixed attribute such as Background or CornerRadius.</summary>
    public string? Attribute(int index, string name) =>
        Attributes(index).Where(a => a.Name == name && !HasPrefix(a.Node)).Select(a => a.Node.RawValue).FirstOrDefault();

    /// <summary>x:Key, matched on the local name like the built-in host.</summary>
    public string? Key(int index) => Attributes(index).Where(a => a.Name == "Key").Select(a => a.Node.RawValue).FirstOrDefault();

    public string? XName(int index) => Attributes(index).Where(a => a.Name == "Name").Select(a => a.Node.RawValue).FirstOrDefault();

    /// <summary>Text directly inside the element (not in child elements).</summary>
    public string TextContent(int index) =>
        string.Concat(Texts.Where(t => t.ParentElementIndex == index && (t.Kind == XamlTextKind.Text || t.Kind == XamlTextKind.CData || t.Kind == XamlTextKind.SignificantWhitespace || t.Kind == XamlTextKind.Whitespace)).Select(t => t.Text));

    private static string AttributeName(XamlPropertyNode property) =>
        property.Reference.DeclaringType is { } owner ? $"{owner.LocalName}.{property.Reference.Name}" : property.Reference.Name;

    private static bool HasPrefix(XamlPropertyNode property) =>
        property.OriginalName != null && property.OriginalName.IndexOf(':') > 0 && property.Reference.DeclaringType == null;

    private static bool IsNamespaceDeclaration(XamlPropertyNode property) =>
        property.OriginalName != null && (property.OriginalName == "xmlns" || property.OriginalName.StartsWith("xmlns:", StringComparison.Ordinal));
}

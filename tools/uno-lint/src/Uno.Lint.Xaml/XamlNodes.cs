using Uno.XamlParser.Stage1;
using Uno.XamlParser.Stage1.Nodes;

namespace Uno.Lint.Xaml;

/// <summary>Reading helpers over Uno.XamlParser Stage-1 nodes, shared by the rules.</summary>
internal static class XamlNodes
{
    // Uno.XamlParser.XmlNamespaces is internal, so the two URIs the rules need are repeated here.
    public const string Presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    public const string XamlLanguage = "http://schemas.microsoft.com/winfx/2006/xaml";

    public static string LocalName(XamlElementNode element) => element.Name.LocalName;

    /// <summary>Attribute name the way XAML writes it: "Background", "Storyboard.TargetProperty", "Key" for x:Key.</summary>
    public static string Name(XamlPropertyNode property) =>
        property.Reference.DeclaringType is { } owner ? $"{owner.LocalName}.{property.Reference.Name}" : property.Reference.Name;

    public static bool IsNamespaceDeclaration(XamlPropertyNode property) =>
        property.OriginalName is { } name && (name == "xmlns" || name.StartsWith("xmlns:", StringComparison.Ordinal));

    /// <summary>Inline attributes and attached properties, without namespace declarations or folded property elements.</summary>
    public static IEnumerable<XamlPropertyNode> Attributes(XamlElementNode element) =>
        element.Properties.Where(p => p.Kind != XamlPropertyKind.PropertyElement && !IsNamespaceDeclaration(p));

    /// <summary>The value of an unprefixed attribute such as Background or CornerRadius.</summary>
    public static string? Attribute(XamlElementNode element, string name) =>
        Attributes(element).FirstOrDefault(p => Name(p) == name && !HasPrefix(p))?.RawValue;

    /// <summary>x:Key, x:Name, x:Uid...: a XAML-language attribute, resolved through the element's namespace scope.</summary>
    public static bool IsXamlLanguage(XamlPropertyNode property, XamlElementNode owner)
    {
        if (property.Reference.DeclaringType != null || property.OriginalName is not { } original)
        {
            return false;
        }

        var colon = original.IndexOf(':');
        return colon > 0 && owner.NamespaceScope.TryGetValue(original.Substring(0, colon), out var uri) && uri == XamlLanguage;
    }

    public static string? Key(XamlElementNode element) =>
        Attributes(element).FirstOrDefault(p => p.Reference.Name == "Key" && IsXamlLanguage(p, element))?.RawValue;

    public static string? XName(XamlElementNode element) =>
        Attributes(element).FirstOrDefault(p => p.Reference.DeclaringType == null && p.Reference.Name == "Name")?.RawValue;

    public static IEnumerable<XamlElementNode> Ancestors(XamlElementNode element, XamlSemanticFixerContext context)
    {
        for (var index = element.ParentIndex; index is int i && context.TryGetElement(i) is { } parent; index = parent.ParentIndex)
        {
            yield return parent;
        }
    }

    public static bool IsInside(XamlElementNode element, XamlSemanticFixerContext context, string ancestorLocalName) =>
        Ancestors(element, context).Any(a => LocalName(a) == ancestorLocalName);

    private static bool HasPrefix(XamlPropertyNode property) =>
        property.Reference.DeclaringType == null && property.OriginalName is { } name && name.IndexOf(':') > 0;
}

namespace Uno.Lint.Xaml;

/// <summary>
/// HEX and CARD over the Uno.XamlParser node model. Same predicates (<see cref="RuleLogic"/>), same messages and
/// same suppression handling as the built-in host, so findings are comparable one to one.
/// </summary>
internal static class ParserRules
{
    // ---------------------------------------------------------------- HEX

    public static IEnumerable<Finding> FindHex(ParsedXaml doc)
    {
        for (var i = 0; i < doc.Elements.Count; i++)
        {
            if (IsThemeDictionaryToken(doc, i))
            {
                continue;
            }

            var elementLine = doc.Line(i);
            var singleThemed = SingleThemedBrushKey(doc, i);
            var note = singleThemed != null ? $"; {singleThemed} is also defined once for both themes" : string.Empty;
            var localName = doc.LocalName(i);

            foreach (var (name, node) in doc.Attributes(i))
            {
                var value = node.RawValue;
                if (value == null || !RuleLogic.IsColorLiteral(value) || !IsColorSlot(doc, i, name))
                {
                    continue;
                }

                yield return New(Rules.Hex, doc, node.LineInfo.LineNumber, $"literal {value.Trim()} on {localName}.{name}{note}", elementLine);
            }

            if (doc.Children(i).Count == 0)
            {
                var content = doc.TextContent(i);
                if (RuleLogic.IsColorLiteral(content) && IsColorElement(doc, i))
                {
                    yield return New(Rules.Hex, doc, elementLine, $"literal {content.Trim()} in <{localName}>{note}");
                }
            }
        }
    }

    private static bool IsThemeDictionaryToken(ParsedXaml doc, int index)
    {
        var keyedBelow = false;
        for (int? e = index; e is int i; e = doc.Parent(i))
        {
            var name = doc.LocalName(i);
            if (name == "ResourceDictionary.ThemeDictionaries")
            {
                return keyedBelow;
            }

            if (doc.Key(i) != null && name != "ResourceDictionary")
            {
                keyedBelow = true;
            }
        }

        return false;
    }

    private static string? SingleThemedBrushKey(ParsedXaml doc, int index)
    {
        var key = doc.Key(index);
        if (key == null || !key.EndsWith("Brush", StringComparison.Ordinal) || RuleLogic.IsInvariantKey(key))
        {
            return null;
        }

        var name = doc.LocalName(index);
        if (name == "StaticResource" || name == "ThemeResource" || doc.IsInside(index, "ResourceDictionary.ThemeDictionaries"))
        {
            return null;
        }

        return key;
    }

    private static bool IsColorSlot(ParsedXaml doc, int index, string property)
    {
        if (property == "Value")
        {
            return TargetPropertyOf(doc, index) is string target && RuleLogic.IsColorName(target);
        }

        return RuleLogic.IsColorName(RuleLogic.LastSegment(property)) || RuleLogic.IsColorName(doc.LocalName(index));
    }

    private static bool IsColorElement(ParsedXaml doc, int index)
    {
        var name = doc.LocalName(index);
        if (name == "Setter.Value")
        {
            return doc.Parent(index) is int parent && TargetPropertyOf(doc, parent) is string target && RuleLogic.IsColorName(target);
        }

        return RuleLogic.IsColorName(name);
    }

    private static string? TargetPropertyOf(ParsedXaml doc, int index)
    {
        if (doc.LocalName(index) == "Setter")
        {
            var setterProperty = doc.Attribute(index, "Property") ?? doc.Attribute(index, "Target");
            if (setterProperty != null)
            {
                return RuleLogic.LastSegment(setterProperty);
            }
        }

        for (int? e = index; e is int i; e = doc.Parent(i))
        {
            var target = doc.Attributes(i).Where(a => a.Name == "Storyboard.TargetProperty").Select(a => a.Node.RawValue).FirstOrDefault();
            if (target != null)
            {
                return RuleLogic.LastSegment(target);
            }
        }

        return null;
    }

    // ---------------------------------------------------------------- CARD

    public static IEnumerable<Finding> FindCards(ParsedXaml doc)
    {
        for (var i = 0; i < doc.Elements.Count; i++)
        {
            if (doc.LocalName(i) != "Border")
            {
                continue;
            }

            var cornerRadius = doc.Attribute(i, "CornerRadius");
            if (cornerRadius == null || !RuleLogic.TryUniformRadius(cornerRadius, out var radius) || radius == 0)
            {
                continue;
            }

            var hasBackground = doc.Attribute(i, "Background") != null;
            if (!hasBackground && doc.Attribute(i, "BorderBrush") == null && doc.Attribute(i, "Padding") == null)
            {
                continue;
            }

            if (doc.IsInside(i, "ControlTemplate"))
            {
                continue;
            }

            if (!hasBackground && doc.IsInside(i, "ShadowContainer"))
            {
                continue;
            }

            if (!double.IsNaN(radius) && RuleLogic.IsCircleOrPill(radius, RuleLogic.ParseNumber(doc.Attribute(i, "Width")), RuleLogic.ParseNumber(doc.Attribute(i, "Height"))))
            {
                continue;
            }

            if (!double.IsNaN(radius) && radius >= RuleLogic.ChipRadius)
            {
                var children = doc.Children(i).Select(doc.LocalName).Where(n => !n.StartsWith("Border.", StringComparison.Ordinal)).ToList();
                if (RuleLogic.IsChipContent(children))
                {
                    continue;
                }
            }

            var name = doc.XName(i);
            var label = name != null ? $"<Border x:Name=\"{name}\">" : "<Border>";
            yield return New(Rules.Card, doc, doc.Line(i), $"{label} drawn as a card");
        }
    }

    // ---------------------------------------------------------------- helpers

    private static Finding New(RuleDescriptor rule, ParsedXaml doc, int line, string message, params int[] extraAnchors)
    {
        var anchors = new int[extraAnchors.Length + 1];
        anchors[0] = line;
        Array.Copy(extraAnchors, 0, anchors, 1, extraAnchors.Length);
        return new Finding(rule, doc.RelativePath, line, message, Suppression.IsSuppressed(doc.Lines, rule, anchors));
    }
}

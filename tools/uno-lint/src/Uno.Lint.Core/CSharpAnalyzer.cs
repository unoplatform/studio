using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Uno.Lint;

public sealed class CSharpFile
{
    public CSharpFile(string relativePath, string text, string? companionXamlText)
    {
        RelativePath = relativePath;
        Text = text;
        Lines = Suppression.SplitLines(text);
        Tree = CSharpSyntaxTree.ParseText(text);
        IsCodeBehind = relativePath.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase);
        CompanionXamlText = companionXamlText;
    }

    public string RelativePath { get; }
    public string Text { get; }
    public string[] Lines { get; }
    public SyntaxTree Tree { get; }
    public bool IsCodeBehind { get; }
    public string? CompanionXamlText { get; }
}

public static class CSharpAnalyzer
{
    private static readonly Regex OverlayName = new Regex(@"(?i)(overlay|sheet|scrim|drawer|popup|panel)$", RegexOptions.Compiled);
    private static readonly Regex SizeMember = new Regex(@"(?:^|\.)(NewSize\.(?:Width|Height)|ActualWidth|ActualHeight|Bounds\.(?:Width|Height))$", RegexOptions.Compiled);

    private static readonly HashSet<string> ControlBaseTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        "Control", "ContentControl", "UserControl", "Panel", "Button", "ToggleButton", "FrameworkElement",
        "Grid", "StackPanel", "Canvas", "Border", "ItemsControl", "SKCanvasElement",
    };

    private sealed class BuiltinEntry
    {
        public BuiltinEntry(string namePattern, string suggestion, string alreadyUsedToken, bool toolkit)
        {
            NamePattern = new Regex(namePattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            Suggestion = suggestion;
            AlreadyUsedToken = alreadyUsedToken;
            Toolkit = toolkit;
        }

        public Regex NamePattern { get; }
        public string Suggestion { get; }
        public string AlreadyUsedToken { get; }
        public bool Toolkit { get; }
    }

    // Name pattern -> suggested control, token whose presence in the companion XAML means it is already used.
    // Patterns run against the class name split into words ("StarRatingView" -> "Star Rating View") with \b on word
    // edges, so a word inside another word does not count: OperatingHoursView, ParameterEditor, DiscardBanner and
    // SpreadsheetGrid do not match rating, meter, card or sheet.
    private static readonly BuiltinEntry[] BuiltinMap =
    {
        new BuiltinEntry(@"\brating\b|\bstars?$",                           "RatingControl",                                      "RatingControl",      false),
        new BuiltinEntry(@"\b(progress|meter|gauge)\b|\bmacro\b.*\bbar$",   "ProgressBar (lightweight keys)",                     "ProgressBar",        false),
        new BuiltinEntry(@"\btab ?bar\b|\btabs$|\bsegment(ed)?\b",          "utu:TabBar (SegmentedStyle / TopTabBarStyle)",       "TabBar",             true),
        new BuiltinEntry(@"\b(nav|navigation|app|top|title|header) ?bar\b", "utu:NavigationBar",                                  "NavigationBar",      true),
        new BuiltinEntry(@"\bcard\b",                                       "utu:CardContentControl + lightweight keys",          "CardContentControl", true),
        new BuiltinEntry(@"\bchips?\b",                                     "utu:Chip / utu:ChipGroup",                           "Chip",               true),
        new BuiltinEntry(@"\b(drawer|sheet)\b",                             "Flyout + DrawerFlyoutPresenter style via a ! route", "Flyout",             true),
        new BuiltinEntry(@"\b(pips|dots|pager)\b",                          "PipsPager",                                          "Pips",               false),
        new BuiltinEntry(@"\b(toggle|switch)\b",                            "ToggleSwitch / ToggleButton",                        "Toggle",             false),
        new BuiltinEntry(@"\b(spinner|loader|busy)\b",                      "ProgressRing / utu:LoadingView",                     "Progress",           false),
    };

    // Word boundaries inside a PascalCase identifier: "StarRating" -> "Star Rating", "UIToggle" -> "UI Toggle".
    private static readonly Regex WordBoundary = new Regex(@"(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])|_", RegexOptions.Compiled);

    public static string SplitWords(string identifier) => WordBoundary.Replace(identifier, " ").Trim();

    public static IEnumerable<Finding> Analyze(CSharpFile file, ProjectContext context, Func<RuleDescriptor, bool> isEnabled)
    {
        var root = file.Tree.GetRoot();
        var findings = new List<Finding>();

        if (file.IsCodeBehind)
        {
            if (isEnabled(Rules.CodeBehind))
            {
                findings.AddRange(FindHandlers(file, root));
            }

            if (isEnabled(Rules.CodeBehind) || isEnabled(Rules.Overlay))
            {
                findings.AddRange(FindVisibilityToggles(file, root, isEnabled));
            }

            if (isEnabled(Rules.Responsive))
            {
                findings.AddRange(FindBreakpoints(file, root));
            }
        }

        if (isEnabled(Rules.Builtin))
        {
            findings.AddRange(FindBuiltinShadows(file, root, context));
        }

        return findings;
    }

    // ---------------------------------------------------------------- CODEBEHIND: handlers

    private static IEnumerable<Finding> FindHandlers(CSharpFile file, SyntaxNode root)
    {
        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            var parameters = method.ParameterList.Parameters;
            if (parameters.Count < 2 || !IsObjectType(parameters[0].Type))
            {
                continue;
            }

            var secondType = parameters[1].Type?.ToString() ?? string.Empty;
            var isHandler = secondType.EndsWith("EventArgs", StringComparison.Ordinal) ||
                            secondType.EndsWith("Args", StringComparison.Ordinal) ||
                            parameters[0].Identifier.Text == "sender";
            if (!isHandler)
            {
                continue;
            }

            var line = LineOf(method.Identifier);
            if (Suppression.IsSuppressed(file.Lines, line, Rules.CodeBehind))
            {
                continue;
            }

            yield return new Finding(Rules.CodeBehind, file.RelativePath, line, $"handler {method.Identifier.Text}");
        }
    }

    // ---------------------------------------------------------------- CODEBEHIND / OVERLAY: Visibility toggles

    private static IEnumerable<Finding> FindVisibilityToggles(CSharpFile file, SyntaxNode root, Func<RuleDescriptor, bool> isEnabled)
    {
        foreach (var assignment in root.DescendantNodes().OfType<AssignmentExpressionSyntax>())
        {
            if (!(assignment.Left is MemberAccessExpressionSyntax access) || access.Name.Identifier.Text != "Visibility")
            {
                continue;
            }

            var target = access.Expression.ToString();
            var shortName = target.Substring(target.LastIndexOf('.') + 1);
            var line = LineOf(access.Name.Identifier);

            if (OverlayName.IsMatch(shortName))
            {
                if (isEnabled(Rules.Overlay) && !Suppression.IsSuppressed(file.Lines, line, Rules.Overlay))
                {
                    yield return new Finding(Rules.Overlay, file.RelativePath, line, $"{target}.Visibility set in code-behind");
                }
            }
            else if (isEnabled(Rules.CodeBehind) && !Suppression.IsSuppressed(file.Lines, line, Rules.CodeBehind))
            {
                yield return new Finding(Rules.CodeBehind, file.RelativePath, line, $"{target}.Visibility set in code-behind");
            }
        }
    }

    // ---------------------------------------------------------------- RESPONSIVE

    private static IEnumerable<Finding> FindBreakpoints(CSharpFile file, SyntaxNode root)
    {
        foreach (var binary in root.DescendantNodes().OfType<BinaryExpressionSyntax>())
        {
            switch (binary.Kind())
            {
                case SyntaxKind.LessThanExpression:
                case SyntaxKind.LessThanOrEqualExpression:
                case SyntaxKind.GreaterThanExpression:
                case SyntaxKind.GreaterThanOrEqualExpression:
                    break;
                default:
                    continue;
            }

            var member = MatchSizeMember(binary.Left) ?? MatchSizeMember(binary.Right);
            if (member == null)
            {
                continue;
            }

            // "ActualHeight > 0" asks whether layout has run, not which breakpoint applies.
            if (IsZeroLiteral(binary.Left) || IsZeroLiteral(binary.Right))
            {
                continue;
            }

            var line = LineOf(binary.OperatorToken);
            if (Suppression.IsSuppressed(file.Lines, line, Rules.Responsive))
            {
                continue;
            }

            yield return new Finding(Rules.Responsive, file.RelativePath, line, $"breakpoint on {member} in code-behind");
        }
    }

    private static bool IsZeroLiteral(ExpressionSyntax expression) =>
        expression is LiteralExpressionSyntax literal &&
        literal.Token.Value is IConvertible value &&
        !(value is string) &&
        Math.Abs(value.ToDouble(System.Globalization.CultureInfo.InvariantCulture)) < double.Epsilon;

    private static string? MatchSizeMember(ExpressionSyntax expression)
    {
        var text = expression.ToString();
        var m = SizeMember.Match(text);
        return m.Success ? m.Groups[1].Value : null;
    }

    // ---------------------------------------------------------------- BUILTIN

    private static IEnumerable<Finding> FindBuiltinShadows(CSharpFile file, SyntaxNode root, ProjectContext context)
    {
        foreach (var cls in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            var baseType = cls.BaseList?.Types
                .Select(t => t.Type)
                .Select(LastIdentifier)
                .FirstOrDefault(n => n != null && ControlBaseTypes.Contains(n));
            if (baseType == null)
            {
                continue;
            }

            var className = cls.Identifier.Text;
            var words = SplitWords(className);
            var entry = BuiltinMap.FirstOrDefault(e => e.NamePattern.IsMatch(words));
            if (entry == null)
            {
                continue;
            }

            if (entry.Toolkit && !context.HasToolkit)
            {
                continue;
            }

            if (file.CompanionXamlText != null && file.CompanionXamlText.IndexOf(entry.AlreadyUsedToken, StringComparison.Ordinal) >= 0)
            {
                continue;
            }

            var line = LineOf(cls.Identifier);
            if (Suppression.IsSuppressed(file.Lines, line, Rules.Builtin))
            {
                continue;
            }

            yield return new Finding(Rules.Builtin, file.RelativePath, line, $"{className} : {baseType} -> {entry.Suggestion}");
        }
    }

    // ---------------------------------------------------------------- helpers

    private static bool IsObjectType(TypeSyntax? type)
    {
        if (type == null)
        {
            return false;
        }

        if (type is NullableTypeSyntax nullable)
        {
            type = nullable.ElementType;
        }

        return type is PredefinedTypeSyntax predefined && predefined.Keyword.IsKind(SyntaxKind.ObjectKeyword)
            || type.ToString() == "Object" || type.ToString() == "System.Object";
    }

    private static string? LastIdentifier(TypeSyntax type)
    {
        switch (type)
        {
            case QualifiedNameSyntax q:
                return LastIdentifier(q.Right);
            case SimpleNameSyntax s:
                return s.Identifier.Text;
            default:
                return null;
        }
    }

    private static int LineOf(SyntaxToken token) => token.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
}

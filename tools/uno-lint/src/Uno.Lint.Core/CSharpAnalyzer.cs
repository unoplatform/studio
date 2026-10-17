using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Uno.Lint;

/// <summary>
/// A C# file ready for the rules. The CLI builds it from text; an analyzer host hands over the compiler's own
/// <see cref="SyntaxTree"/> instead, so the rules never parse or read the disk themselves.
/// </summary>
public sealed class CSharpFile
{
    public CSharpFile(string relativePath, string text, string? companionXamlText)
        : this(relativePath, CSharpSyntaxTree.ParseText(text), companionXamlText)
    {
    }

    public CSharpFile(string relativePath, SyntaxTree tree, string? companionXamlText)
    {
        RelativePath = relativePath;
        Tree = tree;
        Text = tree.GetText().ToString();
        Lines = Suppression.SplitLines(Text);
        IsCodeBehind = relativePath.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase);
        IsAppCodeBehind = string.Equals(Path.GetFileName(relativePath), "App.xaml.cs", StringComparison.OrdinalIgnoreCase);
        IsPaletteFile = FileKinds.IsPaletteFile(relativePath);
        CompanionXamlText = companionXamlText;
    }

    public string RelativePath { get; }
    public string Text { get; }
    public string[] Lines { get; }
    public SyntaxTree Tree { get; }
    public bool IsCodeBehind { get; }

    /// <summary>App.xaml.cs hosts lifecycle handlers (OnLaunched, OnSuspending, OnUnhandledException), not page logic.</summary>
    public bool IsAppCodeBehind { get; }

    /// <summary>A Palette.cs or Tokens.cs: literal colors belong here, as in Palette.xaml.</summary>
    public bool IsPaletteFile { get; }

    public string? CompanionXamlText { get; }
}

public static class CSharpAnalyzer
{
    // "Panel" is the most common suffix for a plain layout container (RootPanel, ContentPanel), so it is not an overlay signal.
    private static readonly Regex OverlayName = new Regex(@"(?i)(overlay|sheet|scrim|drawer|popup)$", RegexOptions.Compiled);
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

    /// <summary>Every finding for the file, including suppressed ones flagged as such.</summary>
    public static IEnumerable<Finding> Analyze(CSharpFile file, ProjectContext context, Func<RuleDescriptor, bool> isEnabled)
    {
        var root = file.Tree.GetRoot();
        var findings = new List<Finding>();

        // CODEBEHIND is about page and control code-behind; App.xaml.cs and Application subclasses carry the
        // lifecycle handlers the template ships (OnLaunched, OnSuspending, OnUnhandledException).
        if (file.IsCodeBehind && !file.IsAppCodeBehind)
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

        if (isEnabled(Rules.Hex) && !file.IsPaletteFile)
        {
            findings.AddRange(FindColorsInCode(file, root));
        }

        return findings;
    }

    // ---------------------------------------------------------------- CODEBEHIND: handlers

    private static IEnumerable<Finding> FindHandlers(CSharpFile file, SyntaxNode root)
    {
        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            // An event handler: (object sender, SomethingEventArgs e). The second type's suffix is the signal, not the
            // parameter names; a Toolkit or platform args type always ends in EventArgs or Args.
            var parameters = method.ParameterList.Parameters;
            if (parameters.Count != 2 || !IsObjectType(parameters[0].Type))
            {
                continue;
            }

            if (method.Parent is ClassDeclarationSyntax owner && DerivesFromApplication(owner))
            {
                continue;
            }

            var secondType = parameters[1].Type?.ToString() ?? string.Empty;
            if (!secondType.EndsWith("EventArgs", StringComparison.Ordinal) && !secondType.EndsWith("Args", StringComparison.Ordinal))
            {
                continue;
            }

            yield return New(Rules.CodeBehind, file, LineOf(method.Identifier), $"handler {method.Identifier.Text}");
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
                if (isEnabled(Rules.Overlay))
                {
                    yield return New(Rules.Overlay, file, line, $"{target}.Visibility set in code-behind");
                }
            }
            else if (isEnabled(Rules.CodeBehind))
            {
                yield return New(Rules.CodeBehind, file, line, $"{target}.Visibility set in code-behind");
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

            yield return New(Rules.Responsive, file, LineOf(binary.OperatorToken), $"breakpoint on {member} in code-behind");
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

            yield return New(Rules.Builtin, file, LineOf(cls.Identifier), $"{className} : {baseType} -> {entry.Suggestion}");
        }
    }

    // ---------------------------------------------------------------- HEX in code

    /// <summary>
    /// Colors.White, Color.FromArgb(...) and ColorHelper.FromArgb(...) are as hard-coded as a hex literal in XAML.
    /// Colors.Transparent is a legitimate "no color" and is skipped.
    /// </summary>
    private static IEnumerable<Finding> FindColorsInCode(CSharpFile file, SyntaxNode root)
    {
        foreach (var access in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (LastIdentifier(access.Expression) != "Colors")
            {
                continue;
            }

            var color = access.Name.Identifier.Text;
            if (color == "Transparent" || !RuleLogic.IsColorLiteral(color))
            {
                continue;
            }

            // Colors.Red.ToString() etc. still hard-code the colour; a nested access like Foo.Colors.Red is covered by LastIdentifier.
            yield return New(Rules.Hex, file, LineOf(access.Name.Identifier), $"Colors.{color} in code");
        }

        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (!(invocation.Expression is MemberAccessExpressionSyntax access) || access.Name.Identifier.Text != "FromArgb")
            {
                continue;
            }

            var owner = LastIdentifier(access.Expression);
            if (owner != "Color" && owner != "ColorHelper")
            {
                continue;
            }

            yield return New(Rules.Hex, file, LineOf(access.Name.Identifier), $"{owner}.FromArgb(...) in code");
        }
    }

    // ---------------------------------------------------------------- helpers

    private static bool DerivesFromApplication(ClassDeclarationSyntax cls) =>
        cls.BaseList?.Types.Select(t => LastIdentifier(t.Type)).Any(n => n == "Application") == true;

    private static Finding New(RuleDescriptor rule, CSharpFile file, int line, string message) =>
        new Finding(rule, file.RelativePath, line, message, Suppression.IsSuppressed(file.Lines, line, rule));

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

    private static string? LastIdentifier(ExpressionSyntax expression)
    {
        switch (expression)
        {
            case QualifiedNameSyntax q:
                return LastIdentifier(q.Right);
            case MemberAccessExpressionSyntax m:
                return m.Name.Identifier.Text;
            case SimpleNameSyntax s:
                return s.Identifier.Text;
            case AliasQualifiedNameSyntax a:
                return a.Name.Identifier.Text;
            default:
                return null;
        }
    }

    private static int LineOf(SyntaxToken token) => token.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
}

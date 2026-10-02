namespace Uno.Lint;

/// <summary>The catalog. Ids 0xx are the recommended profile, 1xx are strict-only.</summary>
public static class Rules
{
    public static readonly RuleDescriptor Hex = new RuleDescriptor(
        "UNOL001", "HEX",
        "Hex color literal outside a palette file",
        "Use a theme role brush (PrimaryBrush, SurfaceBrush, OnSurfaceVariantBrush, OutlineBrush...; shared Uno.Themes keys valid under both SimpleTheme and Material) or a token in a palette file. Brand colors go in the palette override (ColorPaletteOverride for Material, SimpleTheme.ColorOverrideDictionary for Simple).",
        Profile.Recommended);

    public static readonly RuleDescriptor TokenTheme = new RuleDescriptor(
        "UNOL002", "TOKENTHEME",
        "App-defined brush has one value for both themes",
        "Use a theme role brush, define the key per theme in ResourceDictionary.ThemeDictionaries, or reference a {ThemeResource} color. Name it *Invariant/*OnDark/*OnLight/*Fixed only when its ground really is fixed in both themes.",
        Profile.Recommended);

    public static readonly RuleDescriptor Icon = new RuleDescriptor(
        "UNOL003", "ICON",
        "Hand-drawn icon geometry outside an Icons file",
        "Use SymbolIcon/FontIcon, or keyed path data from one open icon set (Material Symbols, Fluent System Icons) in Icons.xaml via PathIcon Data=\"{StaticResource ...}\". Illustrations get 'uno-lint: allow icon - illustration'.",
        Profile.Recommended);

    public static readonly RuleDescriptor Builtin = new RuleDescriptor(
        "UNOL004", "BUILTIN",
        "Custom control shadows a platform or Toolkit control",
        "Use the platform/Toolkit control with lightweight styling before writing a custom control; then Windows Community Toolkit 8.x.",
        Profile.Recommended);

    public static readonly RuleDescriptor Responsive = new RuleDescriptor(
        "UNOL005", "RESPONSIVE",
        "Breakpoint logic in code-behind",
        "Express every width-dependent value as {utu:Responsive} in XAML (custom thresholds via a utu:ResponsiveLayout resource) or a VisualStateManager AdaptiveTrigger. Never compare ActualWidth/NewSize in code-behind or keep width flags on a view model.",
        Profile.Recommended);

    public static readonly RuleDescriptor Card = new RuleDescriptor(
        "UNOL101", "CARD",
        "Border hand-drawn as a card",
        "Use utu:CardContentControl (Filled/Outlined/ElevatedCardContentControlStyle) tuned with lightweight keys (CardCornerRadius, CardPadding, <Variant>CardContentBackground/BorderBrush). A bare Border is for dividers and image clipping.",
        Profile.Strict, requiresToolkit: true);

    public static readonly RuleDescriptor BackBar = new RuleDescriptor(
        "UNOL102", "BACKBAR",
        "Hand-built back button on a page without a NavigationBar",
        "Use utu:NavigationBar with a MainCommand AppBarButton; back navigation and the Android back gesture come with it.",
        Profile.Strict, requiresToolkit: true);

    public static readonly RuleDescriptor CodeBehind = new RuleDescriptor(
        "UNOL103", "CODEBEHIND",
        "Event handler or Visibility toggle in page code-behind",
        "Bind to a model command (utu:CommandExtensions.Command for events without a Command property), drive visuals with utu:VisualStateManagerExtensions.States bound to state, or use nested Visibility navigation regions for tabs. Legitimate platform glue carries '// uno-lint: allow codebehind - reason'.",
        Profile.Strict);

    public static readonly RuleDescriptor Overlay = new RuleDescriptor(
        "UNOL104", "OVERLAY",
        "Overlay, sheet or drawer toggled from code-behind",
        "Navigate with uen:Navigation.Request=\"!Route\" to a Page (opens as a flyout) styled with a DrawerFlyoutPresenter style; keep the state and Reset/Apply in the model.",
        Profile.Strict);

    public static readonly IReadOnlyList<RuleDescriptor> All = new[]
    {
        Hex, TokenTheme, Icon, Builtin, Responsive, Card, BackBar, CodeBehind, Overlay,
    };

    public static RuleDescriptor? Find(string idOrName) =>
        All.FirstOrDefault(r =>
            string.Equals(r.Id, idOrName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r.Name, idOrName, StringComparison.OrdinalIgnoreCase));

    /// <summary>Effective severity for a rule under the given options and project context; None means disabled.</summary>
    public static Severity Resolve(RuleDescriptor rule, LintOptions options, ProjectContext context)
    {
        if (rule.RequiresToolkit && !context.HasToolkit)
        {
            return Severity.None;
        }

        if (options.SeverityOverrides.TryGetValue(rule.Id, out var byId))
        {
            return byId;
        }

        if (options.SeverityOverrides.TryGetValue(rule.Name, out var byName))
        {
            return byName;
        }

        return rule.MinimumProfile <= options.Profile ? Severity.Warning : Severity.None;
    }
}

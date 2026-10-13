namespace Uno.Lint;

/// <summary>The catalog. Ids 0xx are recommended style rules, 1xx are strict-only, 2xx are recommended correctness rules.</summary>
public static class Rules
{
    public static readonly RuleDescriptor Hex = new RuleDescriptor(
        "UNOL001", "HEX",
        "Hard-coded color (hex or named) outside a palette file",
        "Use a theme brush resource or a token in a palette file instead of a literal color.",
        Profile.Recommended);

    public static readonly RuleDescriptor TokenTheme = new RuleDescriptor(
        "UNOL002", "TOKENTHEME",
        "App-defined brush has one value for both themes",
        "Use a theme role brush, define the key per theme in ResourceDictionary.ThemeDictionaries, or reference a {ThemeResource} color. Name it *Invariant/*OnDark/*OnLight/*Fixed only when its ground really is fixed in both themes.",
        // Suggestion by default: the survivors in the corpus were brand and status brushes whose honest fix is a rename.
        Profile.Recommended, defaultSeverity: Severity.Suggestion);

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
        "Bind to a model command (utu:CommandExtensions.Command for events without a Command property), drive visuals with utu:VisualStateManagerExtensions.States bound to state, or use nested Visibility navigation regions for tabs.",
        Profile.Strict);

    public static readonly RuleDescriptor Overlay = new RuleDescriptor(
        "UNOL104", "OVERLAY",
        "Overlay, sheet or drawer toggled from code-behind",
        "Navigate with uen:Navigation.Request=\"!Route\" to a Page (opens as a flyout) styled with a DrawerFlyoutPresenter style; keep the state and Reset/Apply in the model.",
        Profile.Strict);

    /// <summary>First correctness rule: a bug, not a style preference. Only meaningful in MVUX projects.</summary>
    public static readonly RuleDescriptor BindValue = new RuleDescriptor(
        "UNOL201", "BINDVALUE",
        "Binding to State.Value instead of the state",
        "Bind the state itself: {Binding Name, Mode=TwoWay}. The MVUX generated ViewModel exposes IState<T>/IFeed<T> as plain properties, so '.Value' resolves to nothing: the control shows empty and writes nothing back. Inside a FeedView use {Binding Data.Name}.",
        Profile.Recommended, requiresMvux: true);

    public static readonly IReadOnlyList<RuleDescriptor> All = new[]
    {
        Hex, TokenTheme, Icon, Builtin, Responsive, Card, BackBar, CodeBehind, Overlay, BindValue,
    };

    public static RuleDescriptor? Find(string idOrName) =>
        All.FirstOrDefault(r =>
            string.Equals(r.Id, idOrName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r.Name, idOrName, StringComparison.OrdinalIgnoreCase));

    /// <summary>Effective severity for a rule under the given options and project context; None means disabled.</summary>
    public static Severity Resolve(RuleDescriptor rule, LintOptions options, ProjectContext context) =>
        Resolve(rule, options, context, filePath: null);

    /// <summary>
    /// Effective severity for a rule on one file. Order: project gates (Toolkit, MVUX), explicit overrides,
    /// .editorconfig sections that match the file, then the profile default.
    /// </summary>
    public static Severity Resolve(RuleDescriptor rule, LintOptions options, ProjectContext context, string? filePath)
    {
        if (rule.RequiresToolkit && !context.HasToolkit)
        {
            return Severity.None;
        }

        if (rule.RequiresMvux && !context.HasMvux)
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

        if (options.EditorConfig != null && options.EditorConfig.TryGetSeverity(rule.Id, filePath, out var fromConfig))
        {
            return fromConfig;
        }

        return rule.MinimumProfile <= options.Profile ? rule.DefaultSeverity : Severity.None;
    }

    /// <summary>
    /// Fix text for the project at hand. HEX names Uno.Themes role brushes only when Uno.Themes (Material, Cupertino or
    /// SimpleTheme) is referenced; a Fluent-only app gets the WinUI resource vocabulary instead.
    /// </summary>
    public static string FixFor(RuleDescriptor rule, ProjectContext context)
    {
        if (rule == Hex)
        {
            return context.HasUnoThemes
                ? "Use a theme role brush (PrimaryBrush, SurfaceBrush, OnSurfaceVariantBrush, OutlineBrush...; shared Uno.Themes keys valid under SimpleTheme, Material and Cupertino) or a token in a palette file. Brand colors go in the palette override (ColorPaletteOverride for Material, SimpleTheme.ColorOverrideDictionary for Simple)."
                : "Use a WinUI theme brush ({ThemeResource TextFillColorPrimaryBrush}, CardBackgroundFillColorDefaultBrush, AccentFillColorDefaultBrush, ControlStrokeColorDefaultBrush...) or define the color per theme in ResourceDictionary.ThemeDictionaries in a palette file. Never a literal in a page.";
        }

        if (rule == Responsive && !context.HasToolkit)
        {
            return "Express every width-dependent value through a VisualStateManager AdaptiveTrigger in XAML. Never compare ActualWidth/NewSize in code-behind or keep width flags on a view model.";
        }

        if (rule == CodeBehind && !context.HasToolkit)
        {
            return "Bind to a model or view-model command, drive visuals with VisualStateManager states bound to state, and keep code-behind to InitializeComponent plus platform glue.";
        }

        return rule.Fix;
    }
}

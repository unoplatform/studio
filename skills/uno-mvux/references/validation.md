# MVUX Validation with FluentValidation

MVUX validates the draft with `State.Validate` and the Save command with `.Validation`, through the `IValidator` that `UseValidation` registers. The results land on the draft's generated bindable (`INotifyDataErrorInfo`), and Uno's Input validation shows them on each bound input. Write no control or feed that reads errors, and never run the validator again. Verified on desktop, Uno.Sdk 7.0.5.

**Contents:** [Packages](#packages) · [Validator](#validator) · [Model](#model) · [Page](#page) · [Save](#save) · [Limits](#limits)

## Packages

- Uno.Extensions 8.0.1 (Uno.Sdk 7.0.5 or later), or `<UnoExtensionsVersion>8.0.0-dev.250</UnoExtensionsVersion>` on Uno.Sdk 7.0.0-dev. Earlier builds have no `Validate` and no `.Validation`.
- No `UnoFeatures` entry: add `PackageReference`s to `Uno.Extensions.Validation` and `Uno.Extensions.Validation.Fluent` at the `UnoExtensionsVersion`.

## Validator

**Every message is a resource key with no arguments**: the localizer passed to MVUX resolves keys but formats nothing. A rule that needs a number gets its own key (`Validation_Name_TooLong` = "Up to 160 characters.").

```csharp
public class ContactDraftValidator : AbstractValidator<ContactDraft>
{
    public ContactDraftValidator()
    {
        RuleFor(d => d.LastName).NotEmpty().WithMessage("Validation_LastNameRequired")
            .MaximumLength(50).WithMessage("Validation_LastName_TooLong");
    }
}
```

Register with `.UseValidation(configure: v => v.Validator<ContactDraft, ContactDraftValidator>())` and inject the non-generic `Uno.Extensions.Validation.IValidator`. Rules the backend publishes (required, length, range, format): see `uno-kiota`.

## Model

```csharp
public IState<ContactDraft> Draft => State.Async(this, Contacts.LoadAsync);

public IAsyncCommand Save => Command.Create(b => b
    .Given(Draft)
    .Validation(Validator, Localizer)
    .Then(Contacts.SaveAsync), nameof(Save));
```

- **Errors on Save** (above, the default for forms): `.Validation` runs before `Then`. An invalid draft skips `Then`, and the errors stay until the next Save.
- **Live errors**: add `.Validate(Validator, Localizer)` to `Draft`. It validates each change and the first value, so a new form opens with every required field in error.
- **Server refusals**: `await Draft.SetValidationAsync([new ValidationResult(Localizer["Validation_EmailTaken"], ["Email"])], ct);`. It does not localize.

## Page

In the `App` constructor, before `InitializeComponent()`: `Uno.UI.FeatureConfiguration.InputValidation.IsEnabled = true;`. Bindings created before it are never validated.

```xml
<Page xmlns:input="using:Uno.UI.Xaml.Controls" ...>
  <TextBox x:Name="LastNameBox" x:Uid="Contact_LastName" input:Validation.Mode="Auto"
           Text="{Binding Draft.LastName, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />
  <ItemsControl ItemsSource="{Binding (input:Validation.Errors), ElementName=LastNameBox}">
    <ItemsControl.ItemTemplate>
      <DataTemplate><TextBlock Text="{Binding ErrorMessage}" Foreground="{ThemeResource SystemFillColorCriticalBrush}" /></DataTemplate>
    </ItemsControl.ItemTemplate>
  </ItemsControl>
  <TextBlock x:Uid="Contact_FixFields" Visibility="{Binding Draft.HasErrors}" />
```

- **The error comes from the last object of the binding path**: `{Binding Draft.LastName}` reads `GetErrors("LastName")` on the draft bindable.
- `input:Validation.Mode` can be set once by a `Style` setter; base that style on the theme's own `TextBox` style.
- **A custom input** names its validated property with `[Uno.UI.Xaml.Controls.InputValidationProperty(nameof(Value))]`; for another control type, set `FeatureConfiguration.InputValidation.ValidationProperties[typeof(T)]`.
- **The summary** binds to `Draft.HasErrors`, never to a recomputed validity.

## Save

```xml
<Button x:Uid="Contact_Save" Command="{Binding Save}" />
```

Bind Save to the command alone: `.Validation` doesn't change `CanExecute`, so a press always shows why nothing saved. No `IsEnabled` bound to validity, no Click handler that validates.

To move focus to the first field in error, handle the command's completion in code-behind (a refused execution completes too):

```csharp
DataContextChanged += (_, _) =>
{
    if (DataContext is ContactRecordViewModel { Save: AsyncCommand save }) // Uno.Extensions.Reactive.Commands
        save.ExecutionCompleted += (_, _) => DispatcherQueue.TryEnqueue(() =>
            Descendants(this).OfType<Control>().FirstOrDefault(Validation.GetHasErrors)?.Focus(FocusState.Programmatic));
};
```

`Descendants` walks `VisualTreeHelper`; `Validation` is `Uno.UI.Xaml.Controls.Validation`.

## Limits

- **Windows App SDK ignores it**: `Validation.*` compiles there but shows nothing. A `net10.0-windows` head needs its own display.
- **No screen reader text**: errors don't reach `AutomationProperties.HelpText`.
- Item-level errors of an `IListState<T>` aren't supported.

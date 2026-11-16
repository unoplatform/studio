# MVUX Validation with FluentValidation

MVUX validates a state's value with `State.Validate` and a command's parameter with the command builder's `.Validation`, both through the `IValidator` that `UseValidation` registers; FluentValidation validators plug in through `Uno.Extensions.Validation.Fluent`. The results land on the draft's generated bindable, which implements `INotifyDataErrorInfo`: the page reads them from there. Do not run the validator a second time in an errors feed, an `IsValid` feed or an `if` in the command. Verified on desktop with Uno.Extensions 8.0.0-dev.250: localized messages under each field, Save bound to the command alone, nothing saved while invalid.

**Contents:** [Packages](#packages) · [Validator and registration](#validator-and-registration) · [Model](#model) · [Page: errors from the bindable](#page-errors-from-the-bindable) · [Save](#save)

## Packages

- **It needs Uno.Extensions 8.0.1, or 8.0.0-dev.250 or later on the 7.0 dev line.** Uno.Sdk 7.0.5 pins 8.0.1, which has both, so a project on 7.0.5 or later needs no override. Earlier dev builds don't: 8.0.0-dev.157, which Uno.Sdk 7.0.0-dev.132 pins, has no `State.Validate` and no command `.Validation`. On such an SDK, move to 7.0.5, or set `<UnoExtensionsVersion>8.0.0-dev.250</UnoExtensionsVersion>` in the app project.
- **There is no `UnoFeatures` entry for it.** Add `PackageReference`s to `Uno.Extensions.Validation` and `Uno.Extensions.Validation.Fluent`, with `PackageVersion`s in `Directory.Packages.props` at the `UnoExtensionsVersion`. FluentValidation comes with them (11.4.0).

## Validator and registration

Validate the editable draft record. **Every message is a resource key with no arguments.** `Validate` and `.Validation` take an `IStringLocalizer` and resolve each `ErrorMessage` through it; they do not format the result, and a key not found is shown as is. So a validator never builds text: no label, number or `string.Format` in a message, and no `key:argument` form. A message that needs a number gets a key per field (`Validation_Name_TooLong` = "Up to 160 characters."), written from the same rules. The label is beside the field, so the message does not repeat it.

```csharp
public class ContactDraftValidator : AbstractValidator<ContactDraft>
{
    public ContactDraftValidator()
    {
        RuleFor(d => d.LastName).NotEmpty().WithMessage("Validation_LastNameRequired")
            .MaximumLength(50).WithMessage("Validation_LastName_TooLong");
        RuleFor(d => d.Email).EmailAddress().When(d => !string.IsNullOrWhiteSpace(d.Email))
            .WithMessage("Validation_EmailFormat");
    }
}
```

In `App.xaml.cs`, on the host builder: `.UseValidation(configure: v => v.Validator<ContactDraft, ContactDraftValidator>())`, one `.Validator<TEntity, TValidator>()` per draft type. Inject the non-generic `Uno.Extensions.Validation.IValidator` into the Model: one instance validates every registered type.

Field rules taken from a server's metadata (Dataverse `EntityDefinitions`: required level, `MaxLength`, `MinValue`/`MaxValue`) are covered by the `uno-kiota` skill's Dataverse reference; their messages follow the rule above.

## Model

In `public partial record ContactRecordModel(IContactService Contacts, IValidator Validator, IStringLocalizer Localizer)`:

```csharp
public IState<ContactDraft> Draft => State.Async(this, Contacts.LoadAsync).Validate(Validator, Localizer);

public IAsyncCommand Save => Command.Create(b => b
    .Given(Draft)
    .Validation(Validator, Localizer)
    .Then(Contacts.SaveAsync), nameof(Save));
```

That is all the Model holds for validation.

- **`Validate` runs on every change of the state**, the initial value included, and cancels the previous run. It only annotates: an invalid value is still set. It throws `NotSupportedException` on a state not created by the `State` factories.
- **`.Validation` runs on each execution, right before `Then`.** With an error the action is not invoked and the execution completes without error. Its results are published on the `Given` state and replace those of `Validate`. The order is `.Given(state).When(predicate).Validation(validator).Then(action)`.
- Save is a `Command.Create` property, not a public method, so it can carry `.Validation`.

## Page: errors from the bindable

`{Binding Draft}` is the generated `Bindable<Name>ViewModel`: `GetErrors("LastName")` returns `ValidationResult`s with the localized `ErrorMessage`, `HasErrors` is true while any rule fails, and `ErrorsChanged` fires on each change. `GetErrors(null)` and `GetErrors("")` return nothing, so a summary asks field by field.

WinUI inputs do not render `INotifyDataErrorInfo`, so the page needs one small control that does. Copy `assets/FieldError.cs` into the app (set its namespace) and put one under each input:

```xml
<TextBox x:Name="LastNameBox" x:Uid="Contact_LastName"
         Text="{Binding Draft.LastName, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />
<local:FieldError Source="{Binding Draft}" Property="LastName" Input="{Binding ElementName=LastNameBox}"
                  Foreground="{ThemeResource SystemFillColorCriticalBrush}" />
```

- **`Property` is the draft's property name**, as FluentValidation reports it in `MemberNames`.
- **A message shows once its input lost focus, or after Save.** MVUX validates the whole draft from its first value, so an empty form already has an error on every required field.
- `ErrorsChanged` can come from the thread that validated; the control reads on the UI thread, and puts the message in the input's help text for screen readers.

## Save

```xml
<Button x:Uid="Contact_Save" Command="{Binding Save}" local:FieldError.RevealsErrors="True" />
```

- **Bind Save to the command alone.** `.Validation` refuses an invalid draft, and `.Validation` does not change `CanExecute`, so the button stays enabled and a press can show why nothing was saved. Do not add an `IsEnabled` bound to validity, a Click handler that validates, or a wrapper command that checks `CanExecute` first.
- **`FieldError.RevealsErrors="True"`** on the Save button shows every field's error on the page, including fields never visited, and moves focus to the first field in error. The command still runs; its `.Validation` stops the save.
- A summary above the fields, when the recipe asks for one, lists the fields whose `GetErrors(name)` is not empty after Save.

## Related Skills

- `references/state-basics.md` for the draft state
- `references/commands.md` for `Command.Create` and the command rules
- `uno-kiota`, its Dataverse reference, for field rules from server metadata

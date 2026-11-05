# MVUX Validation with FluentValidation

MVUX validates a state's value with `State.Validate` and a command's parameter with the command builder's `.Validation`, both through the `IValidator` that `UseValidation` registers; FluentValidation validators plug in through `Uno.Extensions.Validation.Fluent`. Do not hand-roll an error state and an `if` in the command instead. Verified with a desktop app on Uno.Sdk 7.0.0-dev.132 and Uno.Extensions 8.0.0-dev.250: an empty required field shows its message, Save is disabled while the draft is invalid, and invoking Save with an invalid draft saves nothing.

## Packages

- **It needs Uno.Extensions 8.0.0-dev.250 or later.** 8.0.0-dev.157, which Uno.Sdk 7.0.0-dev.132 pins, has no `State.Validate` and no command `.Validation`. Set `<UnoExtensionsVersion>8.0.0-dev.250</UnoExtensionsVersion>` in the app project.
- **There is no `UnoFeatures` entry for it.** Add `PackageReference`s to `Uno.Extensions.Validation` and `Uno.Extensions.Validation.Fluent`, with `PackageVersion`s in `Directory.Packages.props` at the `UnoExtensionsVersion`. FluentValidation comes with them (11.4.0).

## Validator and registration

Validate the editable draft record. A message can be a resource key: pass an `IStringLocalizer` to `Validate` and `.Validation` and each `ErrorMessage` is resolved through it (a key not found is kept as is; resolved messages are not formatted). Each result's `MemberNames` holds the property name.

```csharp
public class ContactDraftValidator : AbstractValidator<ContactDraft>
{
    public ContactDraftValidator()
    {
        RuleFor(d => d.LastName).NotEmpty().WithMessage("Validation_LastNameRequired")
            .MaximumLength(50).WithMessage("Validation_TooLong");
        RuleFor(d => d.Email).EmailAddress().When(d => !string.IsNullOrWhiteSpace(d.Email))
            .WithMessage("Validation_EmailFormat");
    }
}
```

In `App.xaml.cs`, on the host builder: `.UseValidation(configure: v => v.Validator<ContactDraft, ContactDraftValidator>())`, one `.Validator<TEntity, TValidator>()` per draft type. Inject the non-generic `Uno.Extensions.Validation.IValidator` into the Model: one instance validates every registered type.

Field rules taken from a server's metadata (Dataverse `EntityDefinitions`: required level, `MaxLength`, `MinValue`/`MaxValue`) are covered by the `uno-kiota` skill's Dataverse reference.

## Model

In `public partial record ContactRecordModel(IContactService Contacts, IValidator Validator, IStringLocalizer Localizer)`:

```csharp
public IState<ContactDraft> Draft => State.Async(this, Contacts.LoadAsync).Validate(Validator, Localizer);

public IAsyncCommand Save => Command.Create(b => b
    .Given(Draft)
    .Validation(Validator, Localizer)
    .Then(Contacts.SaveAsync), nameof(Save));
```

- **`Validate` runs on every change of the state**, the initial value included, and cancels the previous run. It only annotates: an invalid value is still set. It throws `NotSupportedException` on a state not created by the `State` factories.
- **`.Validation` runs on each execution, right before `Then`.** With an error the action is not invoked and the execution completes without error. Its results are published on the `Given` state and replace those of `Validate`. The order is `.Given(state).When(predicate).Validation(validator).Then(action)`.
- Save is a `Command.Create` property, not a public method, so it can carry `.Validation`.

## Trap 1: Save stays enabled

**`.Validation` does not change `CanExecute`.** The library docs say so: the command refuses to run but the button stays enabled. Bind `IsEnabled` to an `IsValid` feed computed with the same validator (below). `.When(predicate)` is the builder step that drives `CanExecute`; it takes a synchronous `Predicate<T>`, and the verified app does not use it.

## Trap 2: no control shows the messages

**The generated bindable implements `INotifyDataErrorInfo`, but no Uno control renders it**: Uno.UI has no public input-validation API, and nothing appeared next to the fields in the verified app. Show per-field messages from an errors feed that runs the same validator:

```csharp
public partial record ContactErrors(string LastName, string Email)
{
    public static readonly ContactErrors None = new(string.Empty, string.Empty);
    public bool HasAny => LastName.Length + Email.Length > 0;
}

public IFeed<ContactErrors> Errors => Draft.SelectAsync(async (draft, ct) =>
{
    if (draft is null) return ContactErrors.None;
    var results = await Validator.ValidateAsync(draft, new ValidationContext(draft), ct);
    string For(string member) => string.Join(" ", results
        .Where(r => r.MemberNames.Contains(member) && r.ErrorMessage is not null)
        .Select(r => Localizer[r.ErrorMessage!].Value));
    return new ContactErrors(For(nameof(ContactDraft.LastName)), For(nameof(ContactDraft.Email)));
});

public IFeed<bool> IsValid => Errors.Select(e => !e.HasAny);
```

`ValidationContext` is `System.ComponentModel.DataAnnotations.ValidationContext`. The feed calls the validator directly, so it resolves the keys itself.

```xml
<TextBox Text="{Binding Draft.LastName, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />
<TextBlock Text="{Binding Errors.LastName}"
           Visibility="{Binding Errors.LastName, Converter={StaticResource EmptyToCollapsed}}" />
<Button Content="Save" Command="{Binding Save}" IsEnabled="{Binding IsValid}" />
```

`EmptyToCollapsed` is the app's own string-to-`Visibility` converter. The generated ViewModel also has a public `HasErrors` (true while a validated state has results), enough for a form-level hint but not for per-field text; the verified app does not bind it.

## Related Skills

- `references/state-basics.md` for the draft state
- `references/commands.md` for `Command.Create` and the command rules
- `uno-kiota`, its Dataverse reference, for field rules from server metadata

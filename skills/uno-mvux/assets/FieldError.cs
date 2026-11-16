// FieldError: shows one field's MVUX validation error under its input.
// Copy into the app project and set the namespace. Usage: the uno-mvux skill, references/validation.md.
//
// The errors come from the draft's generated bindable ({Binding Draft}), which implements
// INotifyDataErrorInfo once the state has .Validate(validator, localizer). This control reads them;
// it never runs the validator itself.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace MyApp.Presentation;

/// <summary>
/// One field's error, read from the MVUX draft bindable. Shows once the input lost focus, or once a
/// button with <c>FieldError.RevealsErrors="True"</c> (Save) was pressed.
/// </summary>
public sealed partial class FieldError : ContentControl
{
    private INotifyDataErrorInfo? _source;
    private bool _touched;

    public FieldError()
    {
        Visibility = Visibility.Collapsed;
        IsTabStop = false;
        Loaded += (_, _) => Attach();
        Unloaded += (_, _) => Detach();
    }

    /// <summary>The object holding the errors: for <c>{Binding Draft.LastName}</c>, <c>{Binding Draft}</c>.</summary>
    public object? Source { get => GetValue(SourceProperty); set => SetValue(SourceProperty, value); }
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source), typeof(object), typeof(FieldError),
        new PropertyMetadata(null, (d, e) => ((FieldError)d).OnSourceChanged(e.NewValue)));

    /// <summary>The draft property whose errors this shows, such as <c>LastName</c>.</summary>
    public string? Property { get => (string?)GetValue(PropertyProperty); set => SetValue(PropertyProperty, value); }
    public static readonly DependencyProperty PropertyProperty = DependencyProperty.Register(
        nameof(Property), typeof(string), typeof(FieldError), new PropertyMetadata(null, (d, _) => ((FieldError)d).Refresh()));

    /// <summary>The input the error belongs to: leaving it shows the error, and Save focuses it.</summary>
    public Control? Input { get => (Control?)GetValue(InputProperty); set => SetValue(InputProperty, value); }
    public static readonly DependencyProperty InputProperty = DependencyProperty.Register(
        nameof(Input), typeof(Control), typeof(FieldError), new PropertyMetadata(null, (d, e) =>
        {
            if (e.OldValue is Control old) old.LostFocus -= ((FieldError)d).OnInputLostFocus;
            if (e.NewValue is Control input) input.LostFocus += ((FieldError)d).OnInputLostFocus;
        }));

    /// <summary>True to show the error whether or not the field was left (set by Save).</summary>
    public bool IsRevealed { get => (bool)GetValue(IsRevealedProperty); set => SetValue(IsRevealedProperty, value); }
    public static readonly DependencyProperty IsRevealedProperty = DependencyProperty.Register(
        nameof(IsRevealed), typeof(bool), typeof(FieldError), new PropertyMetadata(false, (d, _) => ((FieldError)d).Refresh()));

    /// <summary>The message shown, or null.</summary>
    public string? Message => Content as string;

    /// <summary>On the Save button: pressing it shows every FieldError on the page and focuses the first field in error.</summary>
    public static bool GetRevealsErrors(DependencyObject d) => (bool)d.GetValue(RevealsErrorsProperty);
    public static void SetRevealsErrors(DependencyObject d, bool value) => d.SetValue(RevealsErrorsProperty, value);
    public static readonly DependencyProperty RevealsErrorsProperty = DependencyProperty.RegisterAttached(
        "RevealsErrors", typeof(bool), typeof(FieldError), new PropertyMetadata(false, (d, e) =>
        {
            if (d is ButtonBase button)
            {
                button.Click -= OnRevealingClick;
                if (e.NewValue is true) button.Click += OnRevealingClick;
            }
        }));

    private static async void OnRevealingClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.XamlRoot?.Content is not { } root) return;
        var fields = Descendants(root).OfType<FieldError>().ToList();
        foreach (var field in fields) field.IsRevealed = true;
        await Task.Delay(150); // the last edit reaches the state and is validated
        fields.FirstOrDefault(f => f.CurrentError() is not null)?.Input?.Focus(FocusState.Programmatic);
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            yield return child;
            foreach (var d in Descendants(child)) yield return d;
        }
    }

    private string? CurrentError()
    {
        if (_source is null || string.IsNullOrEmpty(Property)) return null;
        foreach (var error in _source.GetErrors(Property) ?? Array.Empty<object>())
        {
            var text = error is ValidationResult r ? r.ErrorMessage : error?.ToString();
            if (!string.IsNullOrWhiteSpace(text)) return text;
        }
        return null;
    }

    private void OnSourceChanged(object? source)
    {
        Detach();
        _source = source as INotifyDataErrorInfo;
        if (IsLoaded) Attach();
    }

    private void Attach()
    {
        if (_source is not null)
        {
            _source.ErrorsChanged -= OnErrorsChanged;
            _source.ErrorsChanged += OnErrorsChanged;
        }
        Refresh();
    }

    private void Detach()
    {
        if (_source is not null) _source.ErrorsChanged -= OnErrorsChanged;
    }

    private void OnInputLostFocus(object sender, RoutedEventArgs e)
    {
        _touched = true;
        Refresh();
    }

    private void OnErrorsChanged(object? sender, DataErrorsChangedEventArgs e)
    {
        // Raised from whatever thread validated.
        if (DispatcherQueue.HasThreadAccess) Refresh(); else DispatcherQueue.TryEnqueue(Refresh);
    }

    private void Refresh()
    {
        var message = _touched || IsRevealed ? CurrentError() : null;
        Content = message;
        Visibility = message is null ? Visibility.Collapsed : Visibility.Visible;
        if (Input is not null) AutomationProperties.SetHelpText(Input, message ?? string.Empty);
    }
}

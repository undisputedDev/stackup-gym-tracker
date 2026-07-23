using System.Windows.Input;

namespace StackUp.App.Controls;

/// <summary>
/// "How progression works" overlay — shown once on the first session screen and
/// re-openable from Settings. The host page owns visibility and the close command.
/// </summary>
public partial class ProgressionExplainerView : ContentView
{
    public static readonly BindableProperty CloseCommandProperty =
        BindableProperty.Create(nameof(CloseCommand), typeof(ICommand), typeof(ProgressionExplainerView));

    public ProgressionExplainerView()
    {
        InitializeComponent();
    }

    public ICommand? CloseCommand
    {
        get => (ICommand?)GetValue(CloseCommandProperty);
        set => SetValue(CloseCommandProperty, value);
    }
}

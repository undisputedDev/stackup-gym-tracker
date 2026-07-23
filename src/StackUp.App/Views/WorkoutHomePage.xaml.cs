using StackUp.App.ViewModels;

namespace StackUp.App.Views;

public partial class WorkoutHomePage : ContentPage
{
    private readonly WorkoutHomeViewModel _vm;

    public WorkoutHomePage(WorkoutHomeViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
#if ANDROID
        Controls.DragLift.Enable(SplitsCollection);
#endif
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }

    private async void OnSplitsReorderCompleted(object? sender, EventArgs e)
    {
        await _vm.PersistSplitOrderAsync();
    }
}

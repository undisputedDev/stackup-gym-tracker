using StackUp.App.ViewModels;

namespace StackUp.App.Views;

public partial class SessionPage : ContentPage
{
    private readonly SessionViewModel _vm;

    public SessionPage(SessionViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        DeviceDisplay.Current.KeepScreenOn = true; // don't dim mid-workout
        await _vm.LoadAsync();
    }

    protected override void OnDisappearing()
    {
        DeviceDisplay.Current.KeepScreenOn = false;
        base.OnDisappearing();
    }

    private async void OnBadgeTapped(object? sender, TappedEventArgs e)
    {
        // Quick press bounce on the arrow badge; state change happens via the command.
        if (sender is View badge)
        {
            await badge.ScaleToAsync(0.82, 60, Easing.CubicOut);
            await badge.ScaleToAsync(1.0, 110, Easing.CubicOut);
        }
    }
}

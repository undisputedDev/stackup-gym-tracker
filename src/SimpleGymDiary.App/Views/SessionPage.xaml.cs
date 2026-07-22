using SimpleGymDiary.App.ViewModels;

namespace SimpleGymDiary.App.Views;

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
}

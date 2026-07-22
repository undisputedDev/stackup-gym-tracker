using SimpleGymDiary.App.ViewModels;

namespace SimpleGymDiary.App.Views;

public partial class SplitsPage : ContentPage
{
    private readonly SplitsViewModel _vm;

    public SplitsPage(SplitsViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }
}

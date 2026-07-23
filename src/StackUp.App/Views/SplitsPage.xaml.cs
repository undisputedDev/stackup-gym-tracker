using StackUp.App.ViewModels;

namespace StackUp.App.Views;

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

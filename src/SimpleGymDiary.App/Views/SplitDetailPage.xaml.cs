using SimpleGymDiary.App.ViewModels;

namespace SimpleGymDiary.App.Views;

public partial class SplitDetailPage : ContentPage
{
    private readonly SplitDetailViewModel _vm;

    public SplitDetailPage(SplitDetailViewModel vm)
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

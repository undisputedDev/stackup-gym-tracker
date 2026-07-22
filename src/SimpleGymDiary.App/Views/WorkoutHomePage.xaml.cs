using SimpleGymDiary.App.ViewModels;

namespace SimpleGymDiary.App.Views;

public partial class WorkoutHomePage : ContentPage
{
    private readonly WorkoutHomeViewModel _vm;

    public WorkoutHomePage(WorkoutHomeViewModel vm)
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

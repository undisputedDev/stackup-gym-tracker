using SimpleGymDiary.App.ViewModels;

namespace SimpleGymDiary.App.Views;

public partial class ExerciseEditPage : ContentPage
{
    private readonly ExerciseEditViewModel _vm;

    public ExerciseEditPage(ExerciseEditViewModel vm)
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

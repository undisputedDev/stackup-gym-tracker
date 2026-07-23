using SimpleGymDiary.App.ViewModels;

namespace SimpleGymDiary.App.Views;

public partial class SplitDetailPage : ContentPage
{
    private readonly SplitDetailViewModel _vm;

    public SplitDetailPage(SplitDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
#if ANDROID
        Controls.DragLift.Enable(ExercisesCollection);
#endif
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }

    private async void OnExercisesReorderCompleted(object? sender, EventArgs e)
    {
        await _vm.PersistOrderAsync();
    }
}

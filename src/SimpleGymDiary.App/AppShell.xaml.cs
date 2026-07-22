using SimpleGymDiary.App.Views;

namespace SimpleGymDiary.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Detail pages pushed onto the navigation stack.
        Routing.RegisterRoute("session", typeof(SessionPage));
        Routing.RegisterRoute("splitdetail", typeof(SplitDetailPage));
        Routing.RegisterRoute("exerciseedit", typeof(ExerciseEditPage));
    }
}

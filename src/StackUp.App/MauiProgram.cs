using CommunityToolkit.Maui;
using LiveChartsCore.SkiaSharpView.Maui;
using Microsoft.Extensions.Logging;
using Plugin.Maui.AppRating;
using StackUp.App.Services;
using StackUp.App.ViewModels;
using StackUp.App.Views;
using StackUp.Core.Data;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace StackUp.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseSkiaSharp()
            .UseLiveCharts()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Data layer: one shared async connection for the app's lifetime.
        builder.Services.AddSingleton(_ =>
            new AppDatabase(Path.Combine(FileSystem.AppDataDirectory, "gymdiary.db3")));

        builder.Services.AddSingleton(AppRating.Default);
        builder.Services.AddSingleton<ReviewPrompter>();

        // Pages + ViewModels (transient; state lives in the DB).
        builder.Services.AddTransient<WorkoutHomePage>();
        builder.Services.AddTransient<WorkoutHomeViewModel>();
        builder.Services.AddTransient<SessionPage>();
        builder.Services.AddTransient<SessionViewModel>();
        builder.Services.AddTransient<SplitsPage>();
        builder.Services.AddTransient<SplitsViewModel>();
        builder.Services.AddTransient<SplitDetailPage>();
        builder.Services.AddTransient<SplitDetailViewModel>();
        builder.Services.AddTransient<ExerciseEditPage>();
        builder.Services.AddTransient<ExerciseEditViewModel>();
        builder.Services.AddTransient<StatsPage>();
        builder.Services.AddTransient<StatsViewModel>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<SettingsViewModel>();

        return builder.Build();
    }
}

using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace StackUp.App.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : MauiWinUIApplication
{
	/// <summary>
	/// Initializes the singleton application object.  This is the first line of authored code
	/// executed, and as such is the logical equivalent of main() or WinMain().
	/// </summary>
	public App()
	{
		this.InitializeComponent();

#if DEBUG
		// Log stowed XAML exceptions (0xc000027b) with their managed stack for diagnosis.
		UnhandledException += (_, e) =>
		{
			try
			{
				var path = Path.Combine(Microsoft.Maui.Storage.FileSystem.AppDataDirectory, "crash.log");
				File.AppendAllText(path, $"[{DateTime.Now:O}] {e.Exception}\n\n");
			}
			catch
			{
				// best-effort logging only
			}
		};
#endif
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}


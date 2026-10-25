using AlerteProche.Helpers;
using AlerteProche.Services;

namespace AlerteProche;

public partial class App : Application
{
	public App(SettingsService settings, ThemeService theme)
	{
		InitializeComponent();
		Loc.Language = settings.Language;
		theme.Apply();
	}

	protected override Window CreateWindow(IActivationState? activationState) => new Window(new AppShell());
}

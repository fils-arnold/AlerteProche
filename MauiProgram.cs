using AlerteProche.Pages;
using AlerteProche.Services;
using AlerteProche.ViewModels;
using Microsoft.Extensions.Logging;
#if ANDROID
using AlerteProche.AndroidImpl;
#endif

namespace AlerteProche;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder.UseMauiApp<App>();

		// ---- Services (singletons : un seul état d'alerte pour toute l'application) ----
		var s = builder.Services;
		s.AddSingleton<SettingsService>();
		s.AddSingleton<ThemeService>();
		s.AddSingleton<ContactsRepository>();
		s.AddSingleton<HistoryRepository>();
		s.AddSingleton<ConsentRepository>();
		s.AddSingleton<EmergencyNumbersService>();
		s.AddSingleton<FeedbackService>();
		s.AddSingleton<LocationService>();
		s.AddSingleton<SmsDispatcher>();
		s.AddSingleton<AlertService>();
		s.AddSingleton<TripService>();

		if (AppConfig.UseDemoBackend) s.AddSingleton<IBackendClient, DemoBackendClient>();
		else s.AddSingleton<IBackendClient, HttpBackendClient>();

#if ANDROID
		s.AddSingleton<ISmsSender, AndroidSmsSender>();
		s.AddSingleton<IPermissionService, AndroidPermissionService>();
		s.AddSingleton<IForegroundService, AndroidForegroundController>();
#else
		s.AddSingleton<ISmsSender, ComposeSmsSender>();
		s.AddSingleton<IPermissionService, DefaultPermissionService>();
		s.AddSingleton<IForegroundService, NoForegroundService>();
#endif

		// ---- Écrans et modèles de vue ----
		s.AddTransient<SplashPage>();
		s.AddTransient<SplashViewModel>();
		s.AddTransient<LanguePage>();
		s.AddTransient<LangueViewModel>();
		s.AddTransient<PromessePage>();
		s.AddTransient<PromesseViewModel>();
		s.AddTransient<TelephonePage>();
		s.AddTransient<TelephoneViewModel>();
		s.AddTransient<CodeSmsPage>();
		s.AddTransient<CodeSmsViewModel>();
		s.AddTransient<ConsentementsPage>();
		s.AddTransient<ConsentementsViewModel>();
		s.AddTransient<MessagePage>();
		s.AddTransient<MessageViewModel>();
		s.AddTransient<AutorisationsPage>();
		s.AddTransient<AutorisationsViewModel>();
		s.AddTransient<AjoutProchePage>();
		s.AddTransient<AjoutProcheViewModel>();
		s.AddTransient<TestAlertePage>();
		s.AddTransient<TestAlerteViewModel>();
		s.AddTransient<AccueilPage>();
		s.AddTransient<AccueilViewModel>();
		s.AddTransient<CompteAReboursPage>();
		s.AddTransient<CompteAReboursViewModel>();
		s.AddTransient<AlerteActivePage>();
		s.AddTransient<AlerteActiveViewModel>();
		s.AddTransient<NumerosUrgencePage>();
		s.AddTransient<NumerosUrgenceViewModel>();
		s.AddTransient<JeRentrePage>();
		s.AddTransient<JeRentreViewModel>();
		s.AddTransient<MesProchesPage>();
		s.AddTransient<MesProchesViewModel>();
		s.AddTransient<HistoriquePage>();
		s.AddTransient<HistoriqueViewModel>();
		s.AddTransient<ReglagesPage>();
		s.AddTransient<ReglagesViewModel>();
		s.AddTransient<ProtectionFamillePage>();
		s.AddTransient<ProtectionFamilleViewModel>();
		s.AddTransient<RecuperationPage>();
		s.AddTransient<RecuperationViewModel>();

#if DEBUG
		builder.Logging.AddDebug();
#endif
		return builder.Build();
	}
}

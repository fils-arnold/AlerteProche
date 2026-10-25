using AlerteProche.Helpers;
using AlerteProche.Pages;

namespace AlerteProche;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		Routing.RegisterRoute("langue", typeof(LanguePage));
		Routing.RegisterRoute("promesse", typeof(PromessePage));
		Routing.RegisterRoute("telephone", typeof(TelephonePage));
		Routing.RegisterRoute("code", typeof(CodeSmsPage));
		Routing.RegisterRoute("consentements", typeof(ConsentementsPage));
		Routing.RegisterRoute("message", typeof(MessagePage));
		Routing.RegisterRoute("autorisations", typeof(AutorisationsPage));
		Routing.RegisterRoute("ajoutproche", typeof(AjoutProchePage));
		Routing.RegisterRoute("test", typeof(TestAlertePage));
		Routing.RegisterRoute("countdown", typeof(CompteAReboursPage));
		Routing.RegisterRoute("alerteactive", typeof(AlerteActivePage));
		Routing.RegisterRoute("urgence", typeof(NumerosUrgencePage));
		Routing.RegisterRoute("jerentre", typeof(JeRentrePage));
		Routing.RegisterRoute("protection", typeof(ProtectionFamillePage));
		Routing.RegisterRoute("recuperation", typeof(RecuperationPage));

		RefreshTitles();
	}

	public void RefreshTitles()
	{
		TabHome.Title = Loc.T("tab_home");
		TabContacts.Title = Loc.T("tab_contacts");
		TabHistory.Title = Loc.T("tab_history");
		TabSettings.Title = Loc.T("tab_settings");
	}
}

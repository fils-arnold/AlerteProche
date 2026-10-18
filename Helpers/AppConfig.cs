namespace AlerteProche;

/// <summary>Réglages de l'application. À adapter avant le pilote.</summary>
public static class AppConfig
{
	/// <summary>true : serveur simulé (code SMS 123456, statuts des proches simulés). false : appels HTTP réels.</summary>
	public const bool UseDemoBackend = true;

	/// <summary>Adresse de l'API (page de suivi, codes SMS, statuts). Utilisée seulement si UseDemoBackend = false.</summary>
	public const string ApiBaseUrl = "https://api.alerteproche.example/";

	/// <summary>Code de vérification accepté en mode démonstration.</summary>
	public const string DemoOtp = "123456";

	/// <summary>Délai avant de prévenir les contacts secondaires si le principal ne confirme pas (EF-32 : 60 à 90 s).</summary>
	public const int EscalationSeconds = 60;

	public const int MaxContacts = 3;
}

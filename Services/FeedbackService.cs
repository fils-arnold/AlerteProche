namespace AlerteProche.Services;

/// <summary>Triple retour pour chaque événement d'alerte : visuel (dans l'écran), vibration et voix (ACC-05, ACC-13).</summary>
public class FeedbackService
{
	readonly SettingsService _settings;
	public FeedbackService(SettingsService settings) => _settings = settings;

	/// <summary>Coupe temporairement voix et vibrations pendant une alerte (bouton pause).</summary>
	public bool Muted { get; set; }

	bool Silent => _settings.Discreet || Muted;

	/// <summary>Un « tick » : vibration courte et nombre dit à voix haute (compte à rebours).</summary>
	public async Task TickAsync(string spokenText, int vibrateMs = 150)
	{
		if (Silent) return;
		if (_settings.Vibration) Vibrate(vibrateMs);
		if (_settings.Voice) await SpeakAsync(spokenText);
	}

	/// <summary>Message important dit à voix haute (guidage vocal) : « Alerte envoyée », « Marie a vu l'alerte ».</summary>
	public async Task SayAsync(string text, int vibrateMs = 400)
	{
		if (Silent) return;
		if (_settings.Vibration) Vibrate(vibrateMs);
		if (_settings.VoiceGuide) await SpeakAsync(text);
	}

	static void Vibrate(int ms)
	{
		try { Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(ms)); } catch { /* appareil sans vibreur */ }
	}

	async Task SpeakAsync(string text)
	{
		try
		{
			var locales = await TextToSpeech.Default.GetLocalesAsync();
			var locale = locales.FirstOrDefault(l => l.Language.StartsWith(_settings.Language, StringComparison.OrdinalIgnoreCase));
			await TextToSpeech.Default.SpeakAsync(text, new SpeechOptions { Locale = locale });
		}
		catch { /* synthèse vocale indisponible */ }
	}
}

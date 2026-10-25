namespace AlerteProche.Services;

public class FeedbackService
{
	readonly SettingsService _settings;
	public FeedbackService(SettingsService settings) => _settings = settings;

	public bool Muted { get; set; }

	bool Silent => _settings.Discreet || Muted;

	public async Task TickAsync(string spokenText, int vibrateMs = 150)
	{
		if (Silent) return;
		if (_settings.Vibration) Vibrate(vibrateMs);
		if (_settings.Voice) await SpeakAsync(spokenText);
	}

	public async Task SayAsync(string text, int vibrateMs = 400)
	{
		if (Silent) return;
		if (_settings.Vibration) Vibrate(vibrateMs);
		if (_settings.VoiceGuide) await SpeakAsync(text);
	}

	static void Vibrate(int ms)
	{
		try { Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(ms)); } catch { }
	}

	async Task SpeakAsync(string text)
	{
		try
		{
			var locales = await TextToSpeech.Default.GetLocalesAsync();
			var locale = locales.FirstOrDefault(l => l.Language.StartsWith(_settings.Language, StringComparison.OrdinalIgnoreCase));
			await TextToSpeech.Default.SpeakAsync(text, new SpeechOptions { Locale = locale });
		}
		catch { }
	}
}

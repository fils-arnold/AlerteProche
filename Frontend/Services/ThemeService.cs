namespace AlerteProche.Services;

public class ThemeService
{
	readonly SettingsService _settings;
	public ThemeService(SettingsService settings) => _settings = settings;

	public void Apply()
	{
		var res = Application.Current?.Resources;
		if (res is null) return;

		double k = _settings.TextSize switch { 1 => 1.25, 2 => 1.5, _ => 1.0 };
		res["FontBody"] = 16 * k;
		res["FontTitle"] = 26 * k;
		res["FontSmall"] = 13 * k;
		res["FontButton"] = 18 * k;

		if (_settings.HighContrast)
		{
			res["TextPrimary"] = Colors.Black;
			res["TextMuted"] = Color.FromArgb("#1F2937");
			res["CardBorder"] = Colors.Black;
		}
		else
		{
			res["TextPrimary"] = Color.FromArgb("#111827");
			res["TextMuted"] = Color.FromArgb("#6B7280");
			res["CardBorder"] = Color.FromArgb("#D1D5DB");
		}
	}
}

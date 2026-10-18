using System.Security.Cryptography;
using System.Text;
using AlerteProche.Helpers;

namespace AlerteProche.Services;

/// <summary>Réglages de l'utilisateur, conservés sur le téléphone (Preferences).</summary>
public class SettingsService
{
	static T Get<T>(string key, T def) => Preferences.Default.Get(key, def);
	static void Set<T>(string key, T value) => Preferences.Default.Set(key, value);

	public string Language { get => Get("lang", "fr"); set => Set("lang", value); }
	/// <summary>0 = Normal, 1 = Grand, 2 = Très grand.</summary>
	public int TextSize { get => Get("text_size", 0); set => Set("text_size", value); }
	public bool HighContrast { get => Get("high_contrast", false); set => Set("high_contrast", value); }
	public bool Captions { get => Get("captions", false); set => Set("captions", value); }

	public bool Vibration { get => Get("vibration", true); set => Set("vibration", value); }
	public bool Voice { get => Get("voice", true); set => Set("voice", value); }
	public bool Sounds { get => Get("sounds", false); set => Set("sounds", value); }
	public bool VoiceGuide { get => Get("voice_guide", true); set => Set("voice_guide", value); }
	/// <summary>Mode discret : aucun son, vibration ni voix chez l'émetteur (EF-17).</summary>
	public bool Discreet { get => Get("discreet", false); set => Set("discreet", value); }

	/// <summary>Protection renforcée facultative : PIN + code de contrainte (EF-13, EF-14).</summary>
	public bool ProtectionEnabled { get => Get("protection", false); set => Set("protection", value); }
	public string PinHash { get => Get("pin_hash", ""); set => Set("pin_hash", value); }
	public string DuressHash { get => Get("duress_hash", ""); set => Set("duress_hash", value); }

	public int CountdownSeconds { get => Get("countdown", 5); set => Set("countdown", Math.Clamp(value, 3, 15)); }

	public string UserName { get => Get("user_name", ""); set => Set("user_name", value); }
	/// <summary>Numéro de l'utilisateur au format international.</summary>
	public string UserPhone { get => Get("user_phone", ""); set => Set("user_phone", value); }
	public bool OnboardingDone { get => Get("onboarding_done", false); set => Set("onboarding_done", value); }

	public string PresetMessage
	{
		get => Preferences.Default.ContainsKey("preset") ? Get("preset", "") : Loc.T("msg_default");
		set => Set("preset", value);
	}

	// Fonctions complémentaires (V1) : réglages conservés, déclenchement côté serveur
	public bool DailyCheckIn { get => Get("daily_checkin", false); set => Set("daily_checkin", value); }
	public string DailyCheckInTime { get => Get("daily_checkin_time", "18:00"); set => Set("daily_checkin_time", value); }
	public string HelperContactId { get => Get("helper_id", ""); set => Set("helper_id", value); }
	public bool MedicalEnabled { get => Get("medical", false); set => Set("medical", value); }

	// La fiche médicale est stockée dans le coffre sécurisé du téléphone (chiffré par le système).
	public async Task<string> GetMedicalAsync() => await SecureStorage.Default.GetAsync("medical_info") ?? "";
	public Task SetMedicalAsync(string v) => SecureStorage.Default.SetAsync("medical_info", v);

	public static string Hash(string code) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("alerteproche:" + code)));

	/// <summary>Suppression de toutes les données locales (suppression du compte, EF-05).</summary>
	public void ClearAll()
	{
		Preferences.Default.Clear();
		SecureStorage.Default.RemoveAll();
	}
}

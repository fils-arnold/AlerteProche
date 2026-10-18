using AlerteProche.Helpers;
using AlerteProche.Models;
using AlerteProche.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlerteProche.ViewModels;

// ============================================================ 1. Lancement
public partial class SplashViewModel : BaseViewModel
{
	readonly SettingsService _settings;
	readonly EmergencyNumbersService _numbers;
	readonly IBackendClient _backend;

	public SplashViewModel(SettingsService settings, EmergencyNumbersService numbers, IBackendClient backend)
	{
		_settings = settings; _numbers = numbers; _backend = backend;
	}

	public override async Task OnAppearingAsync()
	{
		// Numéros d'urgence mis à jour à distance, sans bloquer le lancement (EF-40)
		_ = _numbers.RefreshRemoteAsync(_backend);

		await Task.Delay(1800);
		await GoAsync(_settings.OnboardingDone ? "//main" : "langue");
	}
}

// ============================================================ 2. Choix de la langue
public partial class LangueViewModel : BaseViewModel
{
	readonly SettingsService _settings;
	readonly ThemeService _theme;

	public LangueViewModel(SettingsService settings, ThemeService theme) { _settings = settings; _theme = theme; }

	[RelayCommand] private Task French() => Choose("fr");
	[RelayCommand] private Task English() => Choose("en");

	async Task Choose(string lang)
	{
		_settings.Language = lang;
		Loc.Language = lang;
		(Shell.Current as AppShell)?.RefreshTitles();
		await GoAsync("promesse");
	}

	// Raccourcis d'accessibilité dès le premier écran
	[RelayCommand]
	private void Bigger()
	{
		_settings.TextSize = _settings.TextSize == 0 ? 1 : _settings.TextSize == 1 ? 2 : 0;
		_theme.Apply();
	}

	[RelayCommand]
	private async Task Captions()
	{
		_settings.Captions = !_settings.Captions;
		await ShowAsync(Loc.T("lang_captions"), Loc.T(_settings.Captions ? "common_on" : "common_off"));
	}

	[RelayCommand]
	private void Contrast()
	{
		_settings.HighContrast = !_settings.HighContrast;
		_theme.Apply();
	}
}

// ============================================================ 3. Ce que fait AlerteProche
public partial class PromesseViewModel : BaseViewModel
{
	[RelayCommand] private Task Continue() => GoAsync("telephone");
}

// ============================================================ 4. Numéro de téléphone (étape 1 sur 4)
public partial class TelephoneViewModel : BaseViewModel
{
	readonly SettingsService _settings;
	readonly IBackendClient _backend;
	const int Length = 9;   // numéro camerounais : 6XX XXX XXX

	public TelephoneViewModel(SettingsService settings, IBackendClient backend) { _settings = settings; _backend = backend; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Formatted))]
	private string _digits = "";

	public string Formatted
	{
		get
		{
			var d = Digits.PadRight(Length, 'X');
			return $"{d[..3]} {d[3..6]} {d[6..9]}";
		}
	}

	[RelayCommand]
	private void Digit(string d)
	{
		Error = "";
		if (Digits.Length < Length) Digits += d;
	}

	[RelayCommand]
	private void Backspace()
	{
		if (Digits.Length > 0) Digits = Digits[..^1];
	}

	[RelayCommand]
	private async Task ReceiveCode()
	{
		if (Digits.Length != Length || Digits[0] != '6') { Error = Loc.T("tel_invalid"); return; }
		IsBusy = true;
		var phone = "+237" + Digits;
		var ok = await _backend.SendOtpAsync(phone);
		IsBusy = false;
		if (!ok) { Error = Loc.T("tel_send_failed"); return; }
		_settings.UserPhone = phone;
		await GoAsync("code?mode=signup");
	}

	[RelayCommand]
	private Task Accessibility() => AccessibilityShortcuts.ShowAsync();
}

/// <summary>Menu d'accessibilité rapide (bouton rond en haut de l'écran du numéro).</summary>
public static class AccessibilityShortcuts
{
	public static async Task ShowAsync()
	{
		var settings = IPlatformApplication.Current!.Services.GetRequiredService<SettingsService>();
		var theme = IPlatformApplication.Current!.Services.GetRequiredService<ThemeService>();
		var bigger = Loc.T("lang_bigger");
		var contrast = Loc.T("lang_contrast");
		var choice = await Shell.Current.DisplayActionSheet(Loc.T("a11y_title"), Loc.T("common_cancel"), null, bigger, contrast);
		if (choice == bigger) settings.TextSize = settings.TextSize == 0 ? 1 : settings.TextSize == 1 ? 2 : 0;
		else if (choice == contrast) settings.HighContrast = !settings.HighContrast;
		else return;
		theme.Apply();
	}
}

// ============================================================ 5 et 11. Code reçu par SMS (étape 2 sur 4 ou récupération de compte)
[QueryProperty(nameof(Mode), "mode")]
public partial class CodeSmsViewModel : BaseViewModel
{
	readonly SettingsService _settings;
	readonly IBackendClient _backend;

	public CodeSmsViewModel(SettingsService settings, IBackendClient backend) { _settings = settings; _backend = backend; }

	/// <summary>"signup" (inscription) ou "recovery" (nouvel appareil).</summary>
	[ObservableProperty] private string _mode = "signup";

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Slot1), nameof(Slot2), nameof(Slot3), nameof(Slot4), nameof(Slot5), nameof(Slot6))]
	private string _code = "";

	public string Slot1 => Slot(0);
	public string Slot2 => Slot(1);
	public string Slot3 => Slot(2);
	public string Slot4 => Slot(3);
	public string Slot5 => Slot(4);
	public string Slot6 => Slot(5);
	string Slot(int i) => i < Code.Length ? Code[i].ToString() : "-";

	public bool IsRecovery => Mode == "recovery";
	partial void OnModeChanged(string value) => OnPropertyChanged(nameof(IsRecovery));

	partial void OnCodeChanged(string value)
	{
		Error = "";
		if (value.Length == 6) _ = VerifyAsync();
	}

	async Task VerifyAsync()
	{
		IsBusy = true;
		var ok = await _backend.VerifyOtpAsync(_settings.UserPhone, Code);
		IsBusy = false;
		if (!ok)
		{
			Error = Loc.T("code_wrong");
			Code = "";
			return;
		}
		if (IsRecovery)
		{
			_settings.OnboardingDone = true;                 // compte retrouvé sur ce nouvel appareil
			await GoAsync("//main/accueil");
		}
		else await GoAsync("consentements");
	}

	[RelayCommand]
	private async Task Resend()
	{
		var ok = await _backend.SendOtpAsync(_settings.UserPhone);
		await ShowAsync(Loc.T("code_title"), Loc.T(ok ? "code_resent" : "tel_send_failed"));
	}
}

// ============================================================ 21. Vos droits et consentements
public partial class ConsentementsViewModel : BaseViewModel
{
	readonly ConsentRepository _consents;
	public ConsentementsViewModel(ConsentRepository consents) => _consents = consents;

	// Cases jamais pré-cochées (EF-03, LEG-01)
	[ObservableProperty][NotifyCanExecuteChangedFor(nameof(AcceptCommand))] private bool _location;
	[ObservableProperty][NotifyCanExecuteChangedFor(nameof(AcceptCommand))] private bool _sms;
	[ObservableProperty][NotifyCanExecuteChangedFor(nameof(AcceptCommand))] private bool _retention;

	bool CanAccept() => Location && Sms && Retention;

	[RelayCommand(CanExecute = nameof(CanAccept))]
	private async Task Accept()
	{
		_consents.Record("location_during_alert", true);
		_consents.Record("sms_to_contacts", true);
		_consents.Record("short_retention", true);
		await GoAsync("message");
	}
}

// ============================================================ 6. Message préenregistré
public partial class MessageViewModel : BaseViewModel
{
	readonly SettingsService _settings;

	public MessageViewModel(SettingsService settings)
	{
		_settings = settings;
		_text = settings.PresetMessage;
		_userName = settings.UserName;
	}

	[ObservableProperty] private string _text = "";
	[ObservableProperty] private string _userName = "";
	[ObservableProperty] private bool _isEditing;

	public bool IsOnboarding => !_settings.OnboardingDone;

	[RelayCommand] private void Edit() => IsEditing = !IsEditing;

	[RelayCommand]
	private async Task Save()
	{
		if (string.IsNullOrWhiteSpace(Text)) { Error = Loc.T("msg_empty"); return; }
		_settings.PresetMessage = Text.Trim();
		_settings.UserName = UserName.Trim();
		// À l'inscription : étape suivante. Depuis les réglages : retour.
		await GoAsync(_settings.OnboardingDone ? ".." : "autorisations");
	}
}

// ============================================================ 7. Autorisations nécessaires (étape 3 sur 4)
public partial class AutorisationsViewModel : BaseViewModel
{
	readonly IPermissionService _permissions;
	public AutorisationsViewModel(IPermissionService permissions) => _permissions = permissions;

	[ObservableProperty][NotifyCanExecuteChangedFor(nameof(ContinueCommand))][NotifyPropertyChangedFor(nameof(LocationText))] private bool _locationGranted;
	[ObservableProperty][NotifyCanExecuteChangedFor(nameof(ContinueCommand))][NotifyPropertyChangedFor(nameof(SmsText))] private bool _smsGranted;
	[ObservableProperty][NotifyPropertyChangedFor(nameof(NotificationsText))] private bool _notificationsGranted;

	public string LocationText => Loc.T(LocationGranted ? "perm_done" : "perm_activate");
	public string SmsText => Loc.T(SmsGranted ? "perm_done" : "perm_activate");
	public string NotificationsText => Loc.T(NotificationsGranted ? "perm_done" : "perm_activate");

	[RelayCommand] private async Task Location() => LocationGranted = await _permissions.RequestLocationAsync();
	[RelayCommand] private async Task Sms() => SmsGranted = await _permissions.RequestSmsAsync();
	[RelayCommand] private async Task Notifications() => NotificationsGranted = await _permissions.RequestNotificationsAsync();

	// La position et les SMS sont indispensables à une alerte ; les notifications restent facultatives
	bool CanContinue() => LocationGranted && SmsGranted;

	[RelayCommand(CanExecute = nameof(CanContinue))]
	private Task Continue() => GoAsync("ajoutproche");
}

// ============================================================ 8. Ajout du premier proche (étape 4 sur 4) / ajout depuis « Mes proches »
[QueryProperty(nameof(ContactId), "id")]
public partial class AjoutProcheViewModel : BaseViewModel
{
	readonly ContactsRepository _contacts;
	readonly SmsDispatcher _sms;
	readonly SettingsService _settings;

	public AjoutProcheViewModel(ContactsRepository contacts, SmsDispatcher sms, SettingsService settings)
	{
		_contacts = contacts; _sms = sms; _settings = settings;
	}

	[ObservableProperty] private string _contactId = "";
	[ObservableProperty] private string _fullName = "";
	[ObservableProperty] private string _phone = "";

	public bool IsOnboarding => !_settings.OnboardingDone;

	partial void OnContactIdChanged(string value)
	{
		var c = _contacts.Find(value);
		if (c is null) return;                 // mode modification d'un proche existant
		FullName = c.FullName;
		Phone = c.Phone.StartsWith("+237") ? c.Phone[4..] : c.Phone;
	}

	[RelayCommand]
	private async Task SendInvitation()
	{
		Error = "";
		var digits = new string(Phone.Where(char.IsDigit).ToArray());
		if (string.IsNullOrWhiteSpace(FullName)) { Error = Loc.T("add_name_missing"); return; }
		if (digits.Length != 9 || digits[0] != '6') { Error = Loc.T("tel_invalid"); return; }
		var full = "+237" + digits;
		if (full == _settings.UserPhone) { Error = Loc.T("add_own_number"); return; }

		var existing = _contacts.Find(ContactId);
		if (existing is not null)
		{
			existing.FullName = FullName.Trim();
			existing.Phone = full;
			_contacts.Update(existing);
			await GoAsync("..");
			return;
		}

		if (!_contacts.CanAdd) { Error = Loc.T("add_limit"); return; }

		IsBusy = true;
		var contact = _contacts.Add(FullName, full);
		// Invitation par SMS : le proche doit accepter avant d'être alerté (EF-07)
		var who = string.IsNullOrWhiteSpace(_settings.UserName) ? _settings.UserPhone : _settings.UserName;
		await _sms.SendAsync(contact.Phone, Loc.T("sms_invite", who));
		IsBusy = false;

		// À l'inscription : étape suivante (test). Sinon : retour à la liste.
		await GoAsync(_settings.OnboardingDone ? ".." : "test");
	}

	[RelayCommand] private Task Help() => GoAsync("urgence");
}

// ============================================================ 13. Testez votre alerte
public partial class TestAlerteViewModel : BaseViewModel
{
	readonly AlertService _alert;
	readonly SettingsService _settings;

	public TestAlerteViewModel(AlertService alert, SettingsService settings) { _alert = alert; _settings = settings; }

	public bool IsOnboarding => !_settings.OnboardingDone;

	[ObservableProperty] private string _info = "";

	[RelayCommand]
	private async Task Launch()
	{
		IsBusy = true;
		var ok = await _alert.SendTestAsync();
		IsBusy = false;
		if (!ok) { Error = Loc.T("test_failed"); return; }
		await ShowAsync(Loc.T("test_title"), Loc.T("test_sent"));
		await Finish();
	}

	[RelayCommand]
	private Task Skip() => Finish();

	async Task Finish()
	{
		if (!_settings.OnboardingDone)
		{
			_settings.OnboardingDone = true;     // fin de la configuration : direction l'accueil
			await GoAsync("//main/accueil");
		}
		else await GoAsync("..");
	}
}

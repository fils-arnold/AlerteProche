using AlerteProche.Helpers;
using AlerteProche.Models;
using AlerteProche.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlerteProche.ViewModels;

public partial class ReglagesViewModel : BaseViewModel
{
	readonly SettingsService _settings;
	readonly ThemeService _theme;
	bool _guard;

	public ReglagesViewModel(SettingsService settings, ThemeService theme)
	{
		_settings = settings; _theme = theme;
		_guard = true;
		TextSize = settings.TextSize;
		Countdown = settings.CountdownSeconds;
		Vibration = settings.Vibration;
		Voice = settings.Voice;
		Sounds = settings.Sounds;
		Protection = settings.ProtectionEnabled;
		VoiceGuide = settings.VoiceGuide;
		Discreet = settings.Discreet;
		_guard = false;
	}

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsSize0), nameof(IsSize1), nameof(IsSize2))]
	private int _textSize;

	public bool IsSize0 => TextSize == 0;
	public bool IsSize1 => TextSize == 1;
	public bool IsSize2 => TextSize == 2;
	[ObservableProperty] private int _countdown;
	[ObservableProperty] private bool _vibration;
	[ObservableProperty] private bool _voice;
	[ObservableProperty] private bool _sounds;
	[ObservableProperty] private bool _protection;
	[ObservableProperty] private bool _voiceGuide;
	[ObservableProperty] private bool _discreet;

	partial void OnTextSizeChanged(int value) { if (_guard) return; _settings.TextSize = value; _theme.Apply(); }
	partial void OnCountdownChanged(int value) { if (_guard) return; _settings.CountdownSeconds = value; }
	partial void OnVibrationChanged(bool value) { if (_guard) return; _settings.Vibration = value; }
	partial void OnVoiceChanged(bool value) { if (_guard) return; _settings.Voice = value; }
	partial void OnSoundsChanged(bool value) { if (_guard) return; _settings.Sounds = value; }
	partial void OnVoiceGuideChanged(bool value) { if (_guard) return; _settings.VoiceGuide = value; }
	partial void OnDiscreetChanged(bool value) { if (_guard) return; _settings.Discreet = value; }
	partial void OnProtectionChanged(bool value) { if (_guard) return; _ = ToggleProtectionAsync(value); }

	async Task ToggleProtectionAsync(bool on)
	{
		if (on)
		{
			var pin = await PromptAsync(Loc.T("pin_new_title"), Loc.T("pin_new_msg"), "", Keyboard.Numeric, 6);
			if (string.IsNullOrEmpty(pin) || pin.Length < 4 || !pin.All(char.IsDigit))
			{
				Revert(false);
				if (pin is not null) await ShowAsync(Loc.T("pin_new_title"), Loc.T("pin_too_short"));
				return;
			}
			_settings.PinHash = SettingsService.Hash(pin);
			_settings.ProtectionEnabled = true;
		}
		else
		{
			var pin = await PromptAsync(Loc.T("pin_title"), Loc.T("pin_message"), "", Keyboard.Numeric, 6);
			if (pin is null || SettingsService.Hash(pin) != _settings.PinHash)
			{
				Revert(true);
				if (pin is not null) await ShowAsync(Loc.T("pin_title"), Loc.T("pin_wrong"));
				return;
			}
			_settings.ProtectionEnabled = false;
			_settings.PinHash = "";
			_settings.DuressHash = "";
		}
	}

	void Revert(bool value)
	{
		_guard = true;
		Protection = value;
		_guard = false;
	}

	[RelayCommand] private void SetTextSize(string size) => TextSize = int.Parse(size);

	[RelayCommand] private Task Save() => ShowAsync(Loc.T("set_title"), Loc.T("set_saved"));

	[RelayCommand] private Task OpenProtection() => GoAsync("protection");
	[RelayCommand] private Task OpenRecovery() => GoAsync("recuperation");
	[RelayCommand] private Task OpenNumbers() => GoAsync("urgence");
	[RelayCommand] private Task OpenMessage() => GoAsync("message");
	[RelayCommand] private Task OpenTest() => GoAsync("test");
}

public partial class ProtectionFamilleViewModel : BaseViewModel
{
	readonly SettingsService _settings;
	readonly ContactsRepository _contacts;
	readonly SmsDispatcher _sms;
	bool _guard;

	public ProtectionFamilleViewModel(SettingsService settings, ContactsRepository contacts, SmsDispatcher sms)
	{
		_settings = settings; _contacts = contacts; _sms = sms;
		_guard = true;
		DuressOn = !string.IsNullOrEmpty(settings.DuressHash);
		CheckIn = settings.DailyCheckIn;
		Medical = settings.MedicalEnabled;
		_guard = false;
		RefreshHelper();
	}

	[ObservableProperty] private bool _duressOn;
	[ObservableProperty] private bool _checkIn;
	[ObservableProperty] private bool _medical;
	[ObservableProperty] private string _helperText = "";

	public string CheckInTime => _settings.DailyCheckInTime;
	public string CheckInInfo => $"{_settings.DailyCheckInTime} - {Loc.T("prot_checkin_retry")}";

	void RefreshHelper()
	{
		var h = _contacts.Find(_settings.HelperContactId);
		HelperText = h is null ? Loc.T("prot_helper_none") : Loc.T("prot_helper_set", h.FullName);
	}

	partial void OnDuressOnChanged(bool value) { if (_guard) return; _ = ToggleDuressAsync(value); }

	async Task ToggleDuressAsync(bool on)
	{
		if (!on) { _settings.DuressHash = ""; return; }

		if (!_settings.ProtectionEnabled)
		{
			await ShowAsync(Loc.T("prot_title"), Loc.T("prot_need_pin"));
			Revert(() => DuressOn = false);
			return;
		}
		var code = await PromptAsync(Loc.T("duress_title"), Loc.T("duress_msg"), "", Keyboard.Numeric, 6);
		if (string.IsNullOrEmpty(code) || code.Length < 4 || !code.All(char.IsDigit) || SettingsService.Hash(code) == _settings.PinHash)
		{
			if (code is not null) await ShowAsync(Loc.T("duress_title"), Loc.T("duress_invalid"));
			Revert(() => DuressOn = false);
			return;
		}
		_settings.DuressHash = SettingsService.Hash(code);
	}

	void Revert(Action set) { _guard = true; set(); _guard = false; }

	[RelayCommand]
	private async Task ManageHelper()
	{
		var all = _contacts.All;
		if (all.Count == 0) { await ShowAsync(Loc.T("prot_title"), Loc.T("prot_no_contacts")); return; }

		var none = Loc.T("prot_helper_remove");
		var choice = await Shell.Current.DisplayActionSheet(Loc.T("prot_helper_pick"), Loc.T("common_cancel"), null,
			all.Select(c => c.FullName).Append(none).ToArray());
		if (choice is null || choice == Loc.T("common_cancel")) return;

		if (choice == none) _settings.HelperContactId = "";
		else
		{
			var c = all.First(x => x.FullName == choice);
			_settings.HelperContactId = c.Id;
			var who = string.IsNullOrWhiteSpace(_settings.UserName) ? _settings.UserPhone : _settings.UserName;
			await _sms.SendAsync(c.Phone, Loc.T("sms_helper", who, AppConfig.ApiBaseUrl + "helper"));
		}
		RefreshHelper();
	}

	partial void OnCheckInChanged(bool value) { if (_guard) return; _settings.DailyCheckIn = value; }

	[RelayCommand]
	private async Task EditCheckInTime()
	{
		var v = await PromptAsync(Loc.T("prot_checkin"), Loc.T("prot_checkin_time"), _settings.DailyCheckInTime, Keyboard.Default, 5);
		if (v is null) return;
		if (!TimeSpan.TryParse(v, out var t)) { await ShowAsync(Loc.T("prot_checkin"), Loc.T("prot_time_invalid")); return; }
		_settings.DailyCheckInTime = $"{t.Hours:00}:{t.Minutes:00}";
		OnPropertyChanged(nameof(CheckInTime));
		OnPropertyChanged(nameof(CheckInInfo));
	}

	partial void OnMedicalChanged(bool value)
	{
		if (_guard) return;
		_settings.MedicalEnabled = value;
		if (value) _ = EditMedicalAsync();
	}

	[RelayCommand]
	private async Task EditMedical()
	{
		await EditMedicalAsync();
	}

	async Task EditMedicalAsync()
	{
		var current = await _settings.GetMedicalAsync();
		var v = await PromptAsync(Loc.T("prot_medical"), Loc.T("prot_medical_msg"), current, Keyboard.Text, 200);
		if (v is not null) await _settings.SetMedicalAsync(v.Trim());
	}

	[RelayCommand] private Task ManagePlan() => ShowAsync(Loc.T("prot_family"), Loc.T("prot_family_info"));

	[RelayCommand] private Task SaveAndBack() => GoAsync("..");
}

public partial class RecuperationViewModel : BaseViewModel
{
	readonly SettingsService _settings;
	readonly IBackendClient _backend;
	readonly ContactsRepository _contacts;
	readonly HistoryRepository _history;
	readonly ConsentRepository _consents;

	public RecuperationViewModel(SettingsService settings, IBackendClient backend, ContactsRepository contacts,
		HistoryRepository history, ConsentRepository consents)
	{
		_settings = settings; _backend = backend; _contacts = contacts; _history = history; _consents = consents;
	}

	[ObservableProperty] private string _newPhone = "";

	static string? Normalize(string? raw)
	{
		var d = new string((raw ?? "").Where(char.IsDigit).ToArray());
		return d.Length == 9 && d[0] == '6' ? "+237" + d : null;
	}

	[RelayCommand]
	private async Task RecoverBySms()
	{
		var raw = await PromptAsync(Loc.T("rec_new_device"), Loc.T("rec_phone_prompt"), "", Keyboard.Telephone, 12);
		if (raw is null) return;
		var phone = Normalize(raw);
		if (phone is null) { await ShowAsync(Loc.T("rec_new_device"), Loc.T("tel_invalid")); return; }
		if (!await _backend.SendOtpAsync(phone)) { await ShowAsync(Loc.T("rec_new_device"), Loc.T("tel_send_failed")); return; }
		_settings.UserPhone = phone;
		await GoAsync("code?mode=recovery");
	}

	[RelayCommand]
	private async Task ChangeNumber()
	{
		var phone = Normalize(NewPhone);
		if (phone is null) { await ShowAsync(Loc.T("rec_change"), Loc.T("tel_invalid")); return; }
		if (!await _backend.SendOtpAsync(phone)) { await ShowAsync(Loc.T("rec_change"), Loc.T("tel_send_failed")); return; }

		var code = await PromptAsync(Loc.T("code_title"), Loc.T("code_subtitle"), "", Keyboard.Numeric, 6);
		if (code is null) return;
		if (!await _backend.VerifyOtpAsync(phone, code)) { await ShowAsync(Loc.T("code_title"), Loc.T("code_wrong")); return; }

		_settings.UserPhone = phone;
		NewPhone = "";
		await ShowAsync(Loc.T("rec_change"), Loc.T("rec_changed"));
	}

	[RelayCommand]
	private async Task Rights()
	{
		var access = Loc.T("rights_access");
		var fix = Loc.T("rights_fix");
		var erase = Loc.T("rights_erase");
		var choice = await Shell.Current.DisplayActionSheet(Loc.T("rec_rights"), Loc.T("common_cancel"), null, access, fix, erase);

		if (choice == access)
		{
			var summary = Loc.T("rights_summary", _settings.UserName, _settings.UserPhone, _contacts.All.Count, _history.All.Count);
			await ShowAsync(access, summary);
		}
		else if (choice == fix)
		{
			var name = await PromptAsync(fix, Loc.T("rights_fix_prompt"), _settings.UserName, Keyboard.Text, 60);
			if (name is not null) _settings.UserName = name.Trim();
		}
		else if (choice == erase)
		{
			if (!await ConfirmAsync(erase, Loc.T("rights_erase_q"), Loc.T("common_delete"), Loc.T("common_cancel"))) return;
			_history.Clear();
			_contacts.Clear();
			await ShowAsync(erase, Loc.T("rights_erased"));
		}
	}

	[RelayCommand]
	private async Task DeleteAccount()
	{
		if (!await ConfirmAsync(Loc.T("rec_delete"), Loc.T("rec_delete_q"), Loc.T("common_delete"), Loc.T("common_cancel"))) return;
		await _backend.DeleteAccountAsync(_settings.UserPhone);
		_history.Clear();
		_contacts.Clear();
		_consents.Clear();
		_settings.ClearAll();
		Loc.Language = "fr";
		await GoAsync("//splash");
	}

	[RelayCommand]
	private async Task ConsentHistory()
	{
		var all = _consents.All;
		if (all.Count == 0) { await ShowAsync(Loc.T("rec_consents"), Loc.T("consent_none")); return; }
		var lines = all.Select(c => $"{c.At:dd/MM/yyyy HH:mm}  {Loc.T("consent_" + c.Type)}  {(c.Granted ? "✔" : "✖")}");
		await ShowAsync(Loc.T("rec_consents"), string.Join("\n", lines));
	}
}

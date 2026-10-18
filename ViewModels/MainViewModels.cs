using System.Collections.ObjectModel;
using AlerteProche.Helpers;
using AlerteProche.Models;
using AlerteProche.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlerteProche.ViewModels;

// ============================================================ 9. Accueil
public partial class AccueilViewModel : BaseViewModel
{
	readonly AlertService _alert;
	readonly TripService _trip;
	readonly ContactsRepository _contacts;

	public AccueilViewModel(AlertService alert, TripService trip, ContactsRepository contacts)
	{
		_alert = alert; _trip = trip; _contacts = contacts;
		_alert.PropertyChanged += (_, _) => Refresh();
		_trip.PropertyChanged += (_, _) => Refresh();
	}

	// Si l'utilisateur a saisi le code de contrainte, rien ne trahit l'alerte encore active (EF-14)
	public bool ShowBanner => _alert.Phase == AlertPhase.Active && !_alert.DuressActive;

	public string StatusLine => ShowBanner ? Loc.T("home_alert_running")
		: _trip.IsActive ? Loc.T("home_trip_running", (int)Math.Ceiling(_trip.Remaining.TotalMinutes))
		: Loc.T("home_status");

	void Refresh()
	{
		OnPropertyChanged(nameof(ShowBanner));
		OnPropertyChanged(nameof(StatusLine));
	}

	public override Task OnAppearingAsync()
	{
		Refresh();
		return Task.CompletedTask;
	}

	[RelayCommand]
	private async Task Alert()
	{
		if (_alert.Phase == AlertPhase.Active) { await GoAsync("alerteactive"); return; }
		if (_contacts.All.Count == 0)
		{
			await ShowAsync(Loc.T("home_title"), Loc.T("home_no_contact"));
			await GoAsync("ajoutproche");
			return;
		}
		await GoAsync("countdown");
	}

	[RelayCommand] private Task Trip() => GoAsync("jerentre");
	[RelayCommand] private Task Contacts() => GoAsync("//main/proches");
	[RelayCommand] private Task Menu() => GoAsync("//main/reglages");
	[RelayCommand] private Task OpenActive() => GoAsync("alerteactive");
}

// ============================================================ 12. Compte à rebours
public partial class CompteAReboursViewModel : BaseViewModel
{
	readonly AlertService _alert;
	bool _navigating;

	public CompteAReboursViewModel(AlertService alert)
	{
		_alert = alert;
		_alert.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(AlertService.Remaining)) OnPropertyChanged(nameof(Remaining)); };
		_alert.CountdownFinished += OnFinished;
	}

	public int Remaining => _alert.Remaining;

	public override async Task OnAppearingAsync()
	{
		_navigating = false;
		await _alert.StartCountdownAsync();
	}

	async void OnFinished()
	{
		if (_navigating) return;
		_navigating = true;
		// remplace l'écran du compte à rebours par l'alerte active
		await GoAsync("../alerteactive");
	}

	public override void OnDisappearing()
	{
		// Retour système pendant le compte à rebours = annulation. Pas d'effet si l'alerte est partie.
		if (!_navigating) _alert.CancelCountdown();
		_alert.CountdownFinished -= OnFinished;
	}

	[RelayCommand]
	private async Task Cancel()
	{
		_alert.CancelCountdown();
		_navigating = true;
		await GoAsync("..");
	}
}

// ============================================================ 14. Alerte active
public partial class AlerteActiveViewModel : BaseViewModel
{
	readonly AlertService _alert;
	readonly FeedbackService _feedback;
	readonly EmergencyNumbersService _numbers;
	readonly HistoryRepository _history;

	public AlerteActiveViewModel(AlertService alert, FeedbackService feedback, EmergencyNumbersService numbers, HistoryRepository history)
	{
		_alert = alert; _feedback = feedback; _numbers = numbers; _history = history;
	}

	public ObservableCollection<ContactDelivery> Deliveries => _alert.Deliveries;
	public string PoliceNumber => _numbers.PoliceNumber;
	public string CallLabel => Loc.T("active_call", _numbers.PoliceNumber);

	[ObservableProperty] private bool _muted;

	public override async Task OnAppearingAsync()
	{
		if (_alert.Phase != AlertPhase.Active) { await GoAsync("//main/accueil"); return; }
		_alert.NoContactConfirmed -= OnNoConfirmation;
		_alert.NoContactConfirmed += OnNoConfirmation;
	}

	public override void OnDisappearing() => _alert.NoContactConfirmed -= OnNoConfirmation;

	// Personne n'a confirmé : proposer d'appeler les secours, de relancer ou d'ajouter un proche (EF-76)
	async void OnNoConfirmation()
	{
		var call = Loc.T("noconf_call");
		var resend = Loc.T("noconf_resend");
		var add = Loc.T("noconf_add");
		var choice = await Shell.Current.DisplayActionSheet(Loc.T("noconf_title"), Loc.T("common_cancel"), null, call, resend, add);
		if (choice == call) await _numbers.DialAsync(_numbers.PoliceNumber);
		else if (choice == resend) await _alert.ResendToAllAsync();
		else if (choice == add) await GoAsync("ajoutproche");
	}

	[RelayCommand]
	private async Task Safe()
	{
		string? code = null;
		if (IsProtected())
		{
			code = await PromptAsync(Loc.T("pin_title"), Loc.T("pin_message"), "", Keyboard.Numeric, 6);
			if (code is null) return;
		}

		var ok = await _alert.TryCloseAsync(code);
		if (!ok) { await ShowAsync(Loc.T("pin_title"), Loc.T("pin_wrong")); return; }

		var record = _alert.LastRecord;
		await GoAsync("//main/accueil");

		// Qualification de l'alerte en un tap (EF-75). Pas proposée si le code de contrainte a été utilisé.
		if (record is not null && !_alert.DuressActive)
		{
			var kind = await QualifyHelper.AskAsync();
			if (kind.HasValue) _history.SetKind(record.Id, kind.Value);
		}
	}

	static bool IsProtected() => IPlatformApplication.Current!.Services.GetRequiredService<SettingsService>().ProtectionEnabled;

	[RelayCommand] private Task CallEmergency() => _numbers.DialAsync(_numbers.PoliceNumber);
	[RelayCommand] private Task Numbers() => GoAsync("urgence");

	[RelayCommand]
	private void ToggleMute()
	{
		Muted = !Muted;
		_feedback.Muted = Muted;
	}
}

// ============================================================ 15. Numéros d'urgence
public partial class NumerosUrgenceViewModel : BaseViewModel
{
	readonly EmergencyNumbersService _numbers;
	public NumerosUrgenceViewModel(EmergencyNumbersService numbers) => _numbers = numbers;

	public ObservableCollection<EmergencyNumber> Numbers { get; } = new();
	public string CallLabel => Loc.T("active_call", _numbers.PoliceNumber);

	public override Task OnAppearingAsync()
	{
		Reload();
		return Task.CompletedTask;
	}

	void Reload()
	{
		Numbers.Clear();
		foreach (var n in _numbers.GetAll()) Numbers.Add(n);
		OnPropertyChanged(nameof(CallLabel));
	}

	[RelayCommand]
	private async Task Edit(EmergencyNumber item)
	{
		var value = await PromptAsync(item.Label, Loc.T("num_edit"), item.Number, Keyboard.Telephone, 12);
		if (string.IsNullOrWhiteSpace(value)) return;
		_numbers.SetOverride(item.Key, value);
		Reload();
	}

	[RelayCommand] private Task CallPolice() => _numbers.DialAsync(_numbers.PoliceNumber);

	[RelayCommand]
	private Task Guide(string which) => ShowAsync(Loc.T("guide_" + which), Loc.T("guide_" + which + "_text"));
}

// ============================================================ 17. Je rentre
public partial class JeRentreViewModel : BaseViewModel
{
	readonly TripService _trip;

	public JeRentreViewModel(TripService trip)
	{
		_trip = trip;
		_trip.PropertyChanged += (_, _) => { OnPropertyChanged(nameof(IsActive)); OnPropertyChanged(nameof(RemainingText)); };
	}

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsM15), nameof(IsM30), nameof(IsM45), nameof(IsM60))]
	private int _minutes = 30;

	public bool IsM15 => Minutes == 15;
	public bool IsM30 => Minutes == 30;
	public bool IsM45 => Minutes == 45;
	public bool IsM60 => Minutes == 60;

	public bool IsActive => _trip.IsActive;
	public string RemainingText => _trip.Remaining.TotalHours >= 1
		? $"{(int)_trip.Remaining.TotalHours}:{_trip.Remaining.Minutes:00}:{_trip.Remaining.Seconds:00}"
		: $"{_trip.Remaining.Minutes:00}:{_trip.Remaining.Seconds:00}";

	[RelayCommand] private void Choose(string minutes) => Minutes = int.Parse(minutes);

	[RelayCommand]
	private async Task Start()
	{
		_trip.Start(Minutes);
		await GoAsync("..");
	}

	[RelayCommand] private void Extend() => _trip.Extend(15);

	[RelayCommand]
	private async Task Arrived()
	{
		_trip.Stop();
		await GoAsync("..");
	}
}

// ============================================================ 16. Mes proches
public partial class MesProchesViewModel : BaseViewModel
{
	readonly ContactsRepository _contacts;
	readonly IBackendClient _backend;

	public MesProchesViewModel(ContactsRepository contacts, IBackendClient backend) { _contacts = contacts; _backend = backend; }

	public ObservableCollection<TrustedContact> Items { get; } = new();

	[ObservableProperty] private string _header = "";

	public override async Task OnAppearingAsync()
	{
		Reload();
		await _contacts.RefreshStatusesAsync(_backend);   // invitations acceptées entre-temps
		Reload();
	}

	void Reload()
	{
		Items.Clear();
		foreach (var c in _contacts.All) Items.Add(c);
		Header = $"{Loc.T("tab_contacts_title")} ({Items.Count}/{AppConfig.MaxContacts})";
	}

	[RelayCommand]
	private async Task Add()
	{
		if (!_contacts.CanAdd) { await ShowAsync(Header, Loc.T("add_limit")); return; }
		await GoAsync("ajoutproche");
	}

	[RelayCommand] private Task Edit(TrustedContact c) => GoAsync($"ajoutproche?id={c.Id}");

	[RelayCommand]
	private async Task Remove(TrustedContact c)
	{
		if (!await ConfirmAsync(c.FullName, Loc.T("contact_remove_q"), Loc.T("common_delete"), Loc.T("common_cancel"))) return;
		_contacts.Remove(c.Id);
		Reload();
	}

	[RelayCommand]
	private void MakePrimary(TrustedContact c)
	{
		_contacts.SetPrincipal(c.Id);
		Reload();
	}
}

// ============================================================ 10. Historique des alertes
public partial class HistoriqueViewModel : BaseViewModel
{
	readonly HistoryRepository _history;
	public HistoriqueViewModel(HistoryRepository history) => _history = history;

	public ObservableCollection<AlertRecord> Records { get; } = new();

	/// <summary>"all", "true" ou "test".</summary>
	[ObservableProperty] private string _filter = "all";
	[ObservableProperty] private bool _isEmpty;

	partial void OnFilterChanged(string value) => Reload();
	public override Task OnAppearingAsync() { Reload(); return Task.CompletedTask; }

	void Reload()
	{
		Records.Clear();
		IEnumerable<AlertRecord> q = _history.All;
		if (Filter == "true") q = q.Where(r => r.Kind == AlertKind.Vraie);
		if (Filter == "test") q = q.Where(r => r.Kind == AlertKind.Test);
		foreach (var r in q) Records.Add(r);
		IsEmpty = Records.Count == 0;
	}

	[RelayCommand] private void SetFilter(string f) => Filter = f;

	[RelayCommand]
	private async Task Qualify(AlertRecord r)
	{
		var kind = await QualifyHelper.AskAsync();
		if (!kind.HasValue) return;
		_history.SetKind(r.Id, kind.Value);
		Reload();
	}

	[RelayCommand]
	private async Task ClearAll()
	{
		if (!await ConfirmAsync(Loc.T("hist_title"), Loc.T("hist_clear_q"), Loc.T("common_delete"), Loc.T("common_cancel"))) return;
		_history.Clear();
		Reload();
	}
}

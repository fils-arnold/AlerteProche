using System.Collections.ObjectModel;
using System.Globalization;
using AlerteProche.Helpers;
using AlerteProche.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlerteProche.Services;

public enum AlertPhase { Idle, Countdown, Active }

/// <summary>
/// Cœur de l'application : compte à rebours, envoi des SMS avec la position, suivi des états par proche,
/// escalade vers les contacts secondaires, clôture (avec PIN et code de contrainte facultatifs) et historique.
/// </summary>
public partial class AlertService : ObservableObject
{
	readonly SettingsService _settings;
	readonly ContactsRepository _contacts;
	readonly SmsDispatcher _sms;
	readonly LocationService _location;
	readonly HistoryRepository _history;
	readonly FeedbackService _feedback;
	readonly IBackendClient _backend;
	readonly IForegroundService _foreground;

	CancellationTokenSource? _countdownCts;
	CancellationTokenSource? _monitorCts;
	bool _noConfirmRaised;
	string? _trackingUrl;

	public AlertService(SettingsService settings, ContactsRepository contacts, SmsDispatcher sms, LocationService location,
		HistoryRepository history, FeedbackService feedback, IBackendClient backend, IForegroundService foreground)
	{
		_settings = settings; _contacts = contacts; _sms = sms; _location = location;
		_history = history; _feedback = feedback; _backend = backend; _foreground = foreground;
	}

	[ObservableProperty] private AlertPhase _phase = AlertPhase.Idle;
	[ObservableProperty] private int _remaining;
	[ObservableProperty] private bool _escalated;

	public ObservableCollection<ContactDelivery> Deliveries { get; } = new();
	public string AlertId { get; private set; } = "";
	public DateTime StartedAt { get; private set; }
	public double? Latitude { get; private set; }
	public double? Longitude { get; private set; }
	/// <summary>Vrai si l'utilisateur a saisi le code de contrainte : l'alerte continue en silence (EF-14).</summary>
	public bool DuressActive { get; private set; }
	public AlertRecord? LastRecord { get; private set; }

	public bool AnyConfirmed => Deliveries.Any(d => d.State == DeliveryState.Confirme);

	public event Action? CountdownFinished;
	/// <summary>Aucun proche n'a confirmé dans le délai prévu : proposer d'appeler les secours (EF-76).</summary>
	public event Action? NoContactConfirmed;

	// ------------------------------------------------------------------ compte à rebours

	public async Task StartCountdownAsync()
	{
		if (Phase != AlertPhase.Idle) return;
		Phase = AlertPhase.Countdown;
		Remaining = _settings.CountdownSeconds;
		_countdownCts = new CancellationTokenSource();
		var ct = _countdownCts.Token;
		try
		{
			while (Remaining > 0)
			{
				_ = _feedback.TickAsync(Remaining.ToString());
				await Task.Delay(1000, ct);
				Remaining--;
			}
			await TriggerAsync();
		}
		catch (OperationCanceledException) { /* annulé par l'utilisateur */ }
	}

	/// <summary>Annulation en un tap pendant le compte à rebours (EF-12).</summary>
	public void CancelCountdown()
	{
		if (Phase != AlertPhase.Countdown) return;
		_countdownCts?.Cancel();
		Phase = AlertPhase.Idle;
	}

	/// <summary>Déclenchement immédiat, sans compte à rebours (trajet non confirmé, EF-37).</summary>
	public async Task TriggerImmediateAsync()
	{
		if (Phase != AlertPhase.Idle) return;
		await TriggerAsync();
	}

	// ------------------------------------------------------------------ envoi

	async Task TriggerAsync()
	{
		Phase = AlertPhase.Active;
		StartedAt = DateTime.Now;
		AlertId = Guid.NewGuid().ToString("N")[..12];
		DuressActive = false;
		Escalated = false;
		_noConfirmRaised = false;
		_feedback.Muted = false;
		Deliveries.Clear();

		_foreground.Start(Loc.T("fg_title"), Loc.T("fg_text"));

		var loc = await _location.GetAsync();
		Latitude = loc?.Latitude;
		Longitude = loc?.Longitude;

		_trackingUrl = await _backend.RegisterAlertAsync(
			new AlertPayload(AlertId, _settings.UserPhone, Latitude, Longitude, StartedAt, false));

		var recipients = _contacts.Recipients();
		foreach (var c in recipients)
			Deliveries.Add(new ContactDelivery { ContactId = c.Id, Name = c.FullName });

		// Contact principal d'abord ; les secondaires attendent l'escalade (EF-32)
		var first = recipients.Where(c => c.Role == ContactRole.Principal).ToList();
		if (first.Count == 0 && recipients.Count > 0) first.Add(recipients[0]);
		await NotifyAsync(first, BuildMessage());

		_ = _feedback.SayAsync(Loc.T("voice_sent"));
		CountdownFinished?.Invoke();

		_monitorCts = new CancellationTokenSource();
		_ = MonitorAsync(_monitorCts.Token);
	}

	string BuildMessage()
	{
		var name = string.IsNullOrWhiteSpace(_settings.UserName) ? _settings.UserPhone : _settings.UserName;
		string pos;
		if (_trackingUrl is not null) pos = _trackingUrl;
		else if (Latitude.HasValue && Longitude.HasValue)
			pos = $"https://maps.google.com/?q={Latitude.Value.ToString(CultureInfo.InvariantCulture)},{Longitude.Value.ToString(CultureInfo.InvariantCulture)}";
		else pos = Loc.T("sms_nopos");
		return $"{_settings.PresetMessage} ({name}, {StartedAt:HH:mm}) {pos}";
	}

	async Task NotifyAsync(IEnumerable<TrustedContact> list, string text)
	{
		foreach (var c in list)
		{
			var ok = await _sms.SendAsync(c.Phone, text);
			var d = Deliveries.FirstOrDefault(x => x.ContactId == c.Id);
			if (d is not null) d.State = ok ? DeliveryState.Envoye : DeliveryState.EnFile;
		}
	}

	// ------------------------------------------------------------------ suivi, relance, escalade

	async Task MonitorAsync(CancellationToken ct)
	{
		try
		{
			while (!ct.IsCancellationRequested)
			{
				await Task.Delay(3000, ct);

				await _sms.FlushAsync();   // renvoie les SMS restés en file d'attente

				var statuses = await _backend.GetAlertStatusesAsync(AlertId, Deliveries.Select(d => d.ContactId));
				foreach (var d in Deliveries)
				{
					if (d.State == DeliveryState.EnAttente) continue;                   // pas encore alerté
					if (!statuses.TryGetValue(d.ContactId, out var s) || s <= d.State) continue;
					d.State = s;
					if (s == DeliveryState.Vu) _ = _feedback.SayAsync(Loc.T("voice_seen", d.Name));
					if (s == DeliveryState.Confirme) _ = _feedback.SayAsync(Loc.T("voice_confirmed", d.Name));
				}
				// un SMS mis en file puis envoyé passe à « envoyé »
				foreach (var d in Deliveries.Where(d => d.State == DeliveryState.EnFile) )
					if (_sms.PendingCount == 0) d.State = DeliveryState.Envoye;

				var elapsed = (DateTime.Now - StartedAt).TotalSeconds;

				if (!Escalated && !AnyConfirmed && elapsed >= AppConfig.EscalationSeconds)
				{
					Escalated = true;
					var waiting = _contacts.Recipients().Where(c => Deliveries.Any(d => d.ContactId == c.Id && d.State == DeliveryState.EnAttente)).ToList();
					await NotifyAsync(waiting, BuildMessage());
				}

				if (!_noConfirmRaised && !AnyConfirmed && elapsed >= AppConfig.EscalationSeconds * 2)
				{
					_noConfirmRaised = true;
					NoContactConfirmed?.Invoke();
				}
			}
		}
		catch (OperationCanceledException) { }
	}

	/// <summary>Relance manuelle de tous les proches (écran « aucun contact n'a confirmé »).</summary>
	public async Task ResendToAllAsync()
	{
		if (Phase != AlertPhase.Active) return;
		await NotifyAsync(_contacts.Recipients(), BuildMessage());
		_noConfirmRaised = false;
	}

	// ------------------------------------------------------------------ clôture

	/// <summary>
	/// Clôture « je suis en sécurité ». Sans protection renforcée : clôture directe. Avec protection : le PIN clôture,
	/// le code de contrainte fait semblant de clôturer mais maintient l'alerte (EF-13, EF-14). false = code refusé.
	/// </summary>
	public async Task<bool> TryCloseAsync(string? code)
	{
		if (Phase != AlertPhase.Active) return true;

		if (_settings.ProtectionEnabled)
		{
			var h = SettingsService.Hash(code ?? "");
			if (!string.IsNullOrEmpty(_settings.DuressHash) && h == _settings.DuressHash)
			{
				DuressActive = true;      // l'écran se comporte comme si l'alerte était fermée, mais elle continue
				return true;
			}
			if (h != _settings.PinHash) return false;
		}

		await FinishAsync();
		return true;
	}

	async Task FinishAsync()
	{
		_monitorCts?.Cancel();
		var seconds = (int)(DateTime.Now - StartedAt).TotalSeconds;

		LastRecord = new AlertRecord
		{
			Id = AlertId,
			StartedAt = StartedAt,
			DurationSeconds = seconds,
			Kind = AlertKind.NonQualifiee,
			ConfirmedNames = Deliveries.Where(d => d.State == DeliveryState.Confirme).Select(d => d.Name).ToList(),
			Latitude = Latitude,
			Longitude = Longitude
		};
		_history.Add(LastRecord);

		// Les proches savent que la personne a clôturé elle-même et peuvent la rappeler (EF-34)
		var name = string.IsNullOrWhiteSpace(_settings.UserName) ? _settings.UserPhone : _settings.UserName;
		var closeText = Loc.T("sms_closed", name);
		foreach (var c in _contacts.Recipients().Where(c => Deliveries.Any(d => d.ContactId == c.Id && d.State >= DeliveryState.EnFile)))
			await _sms.SendAsync(c.Phone, closeText);

		_foreground.Stop();
		Phase = AlertPhase.Idle;
		DuressActive = false;
	}

	// ------------------------------------------------------------------ test

	/// <summary>Test d'alerte à blanc : un SMS de test au contact principal, aucune vraie alerte (A-10).</summary>
	public async Task<bool> SendTestAsync()
	{
		var contact = _contacts.Primary;
		if (contact is null) return false;
		var name = string.IsNullOrWhiteSpace(_settings.UserName) ? _settings.UserPhone : _settings.UserName;
		var ok = await _sms.SendAsync(contact.Phone, Loc.T("sms_test", name));
		_history.Add(new AlertRecord
		{
			Id = Guid.NewGuid().ToString("N")[..12],
			StartedAt = DateTime.Now,
			DurationSeconds = 1,
			Kind = AlertKind.Test
		});
		return ok;
	}
}

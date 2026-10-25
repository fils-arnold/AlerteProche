using AlerteProche.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlerteProche.Services;

public partial class TripService : ObservableObject
{
	readonly AlertService _alert;
	readonly FeedbackService _feedback;
	readonly IForegroundService _foreground;
	DateTime _deadline;
	CancellationTokenSource? _cts;

	public TripService(AlertService alert, FeedbackService feedback, IForegroundService foreground)
	{
		_alert = alert; _feedback = feedback; _foreground = foreground;
	}

	[ObservableProperty] private bool _isActive;
	[ObservableProperty] private TimeSpan _remaining;

	public void Start(int minutes)
	{
		Stop();
		_deadline = DateTime.Now.AddMinutes(minutes);
		IsActive = true;
		_foreground.Start(Loc.T("fg_trip_title"), Loc.T("fg_trip_text"));
		_cts = new CancellationTokenSource();
		_ = LoopAsync(_cts.Token);
	}

	public void Extend(int minutes) => _deadline = _deadline.AddMinutes(minutes);

	public void Stop()
	{
		_cts?.Cancel();
		_cts = null;
		if (IsActive) _foreground.Stop();
		IsActive = false;
		Remaining = TimeSpan.Zero;
	}

	async Task LoopAsync(CancellationToken ct)
	{
		try
		{
			while (!ct.IsCancellationRequested)
			{
				Remaining = _deadline - DateTime.Now;
				if (Remaining <= TimeSpan.Zero)
				{
					if (!await AskArrivalAsync(ct)) return;
					continue;
				}
				await Task.Delay(1000, ct);
			}
		}
		catch (OperationCanceledException) { }
	}

	async Task<bool> AskArrivalAsync(CancellationToken ct)
	{
		_ = _feedback.SayAsync(Loc.T("trip_warn_voice"), 800);

		var arrived = Loc.T("trip_arrived");
		var extend = Loc.T("trip_extend15");
		var sheet = Shell.Current.DisplayActionSheet(Loc.T("trip_warn_title"), null, null, arrived, extend);
		var done = await Task.WhenAny(sheet, Task.Delay(TimeSpan.FromSeconds(60), ct));

		if (done == sheet)
		{
			var answer = await sheet;
			if (answer == extend) { Extend(15); return true; }
			Stop();
			return false;
		}

		Stop();
		await _alert.TriggerImmediateAsync();
		await Shell.Current.GoToAsync("alerteactive");
		return false;
	}
}

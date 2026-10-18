namespace AlerteProche.Services;

/// <summary>Envoi direct d'un SMS (sans ouvrir l'application Messages).</summary>
public interface ISmsSender
{
	Task<bool> SendAsync(string phone, string text);
}

/// <summary>Demande des autorisations système.</summary>
public interface IPermissionService
{
	Task<bool> RequestLocationAsync();
	Task<bool> RequestSmsAsync();
	Task<bool> RequestNotificationsAsync();
}

/// <summary>Service de premier plan : garde l'application active pendant une alerte ou un trajet (EF-77, ENF-07).</summary>
public interface IForegroundService
{
	void Start(string title, string text);
	void Stop();
}

// ---- Versions de secours pour les plateformes autres qu'Android ----

public class ComposeSmsSender : ISmsSender
{
	public async Task<bool> SendAsync(string phone, string text)
	{
		try
		{
			if (!Sms.Default.IsComposeSupported) return false;
			await Sms.Default.ComposeAsync(new SmsMessage(text, phone));
			return true;
		}
		catch { return false; }
	}
}

public class DefaultPermissionService : IPermissionService
{
	public async Task<bool> RequestLocationAsync() => await Permissions.RequestAsync<Permissions.LocationWhenInUse>() == PermissionStatus.Granted;
	public Task<bool> RequestSmsAsync() => Task.FromResult(true);
	public Task<bool> RequestNotificationsAsync() => Task.FromResult(true);
}

public class NoForegroundService : IForegroundService
{
	public void Start(string title, string text) { }
	public void Stop() { }
}

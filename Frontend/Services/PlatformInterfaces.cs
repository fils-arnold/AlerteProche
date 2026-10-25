namespace AlerteProche.Services;

public interface ISmsSender
{
	Task<bool> SendAsync(string phone, string text);
}

public interface IPermissionService
{
	Task<bool> RequestLocationAsync();
	Task<bool> RequestSmsAsync();
	Task<bool> RequestNotificationsAsync();
}

public interface IForegroundService
{
	void Start(string title, string text);
	void Stop();
}

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

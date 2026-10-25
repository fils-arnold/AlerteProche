using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AlerteProche.Services;

namespace AlerteProche.AndroidImpl;

[Service(Exported = false, ForegroundServiceType = Android.Content.PM.ForegroundService.TypeLocation)]
public class AlertForegroundService : Service
{
	const string ChannelId = "alerteproche_active";
	const int NotificationId = 4017;

	public override IBinder? OnBind(Intent? intent) => null;

	public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
	{
		var title = intent?.GetStringExtra("title") ?? "AlerteProche";
		var text = intent?.GetStringExtra("text") ?? "";

		var manager = (NotificationManager?)GetSystemService(NotificationService);
		if (OperatingSystem.IsAndroidVersionAtLeast(26) && manager is not null)
			manager.CreateNotificationChannel(new NotificationChannel(ChannelId, "AlerteProche", NotificationImportance.High));

		var notification = new Notification.Builder(this, ChannelId)
			.SetContentTitle(title)
			.SetContentText(text)
			.SetSmallIcon(Android.Resource.Drawable.IcDialogAlert)
			.SetOngoing(true)
			.Build();

		if (OperatingSystem.IsAndroidVersionAtLeast(29))
			StartForeground(NotificationId, notification, Android.Content.PM.ForegroundService.TypeLocation);
		else
			StartForeground(NotificationId, notification);

		return StartCommandResult.Sticky;
	}
}

public class AndroidForegroundController : IForegroundService
{
	public void Start(string title, string text)
	{
		try
		{
			var ctx = Android.App.Application.Context;
			var intent = new Intent(ctx, typeof(AlertForegroundService));
			intent.PutExtra("title", title);
			intent.PutExtra("text", text);
			ctx.StartForegroundService(intent);
		}
		catch { }
	}

	public void Stop()
	{
		try
		{
			var ctx = Android.App.Application.Context;
			ctx.StopService(new Intent(ctx, typeof(AlertForegroundService)));
		}
		catch { }
	}
}

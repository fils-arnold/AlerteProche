using AlerteProche.Services;

namespace AlerteProche.AndroidImpl;

public class SendSmsPermission : Permissions.BasePlatformPermission
{
	public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
		new[] { (Android.Manifest.Permission.SendSms, true) };
}

public class PostNotificationsPermission : Permissions.BasePlatformPermission
{
	public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
		new[] { ("android.permission.POST_NOTIFICATIONS", true) };
}

/// <summary>Demande des autorisations, une par une, avec explication préalable (écran « Autorisations nécessaires »).</summary>
public class AndroidPermissionService : IPermissionService
{
	public async Task<bool> RequestLocationAsync()
	{
		var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
		if (status != PermissionStatus.Granted) return false;

		// Android 10+ : la position en arrière-plan se demande séparément (protection continue)
		try { await Permissions.RequestAsync<Permissions.LocationAlways>(); } catch { /* facultatif */ }
		return true;
	}

	public async Task<bool> RequestSmsAsync() => await Permissions.RequestAsync<SendSmsPermission>() == PermissionStatus.Granted;

	public async Task<bool> RequestNotificationsAsync()
	{
		if (!OperatingSystem.IsAndroidVersionAtLeast(33)) return true;
		return await Permissions.RequestAsync<PostNotificationsPermission>() == PermissionStatus.Granted;
	}
}

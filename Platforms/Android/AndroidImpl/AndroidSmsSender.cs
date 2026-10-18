#pragma warning disable CA1422, CS0618
using Android.Content;
using Android.Telephony;
using AlerteProche.Services;

namespace AlerteProche.AndroidImpl;

/// <summary>Envoi direct d'un SMS avec SmsManager : fonctionne sans internet (EF-19, EF-20).</summary>
public class AndroidSmsSender : ISmsSender
{
	public Task<bool> SendAsync(string phone, string text)
	{
		try
		{
			SmsManager? manager;
			if (OperatingSystem.IsAndroidVersionAtLeast(31))
				manager = Android.App.Application.Context.GetSystemService(Java.Lang.Class.FromType(typeof(SmsManager))) as SmsManager;
			else
				manager = SmsManager.Default;

			if (manager is null) return Task.FromResult(false);

			var parts = manager.DivideMessage(text);
			manager.SendMultipartTextMessage(phone, null, parts, null, null);
			return Task.FromResult(true);
		}
		catch
		{
			return Task.FromResult(false);   // pas de carte SIM, permission refusée... : le SMS ira en file d'attente
		}
	}
}

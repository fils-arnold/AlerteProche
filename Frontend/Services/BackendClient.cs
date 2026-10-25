using System.Net.Http.Json;
using AlerteProche.Models;

namespace AlerteProche.Services;

public record AlertPayload(string AlertId, string UserPhone, double? Lat, double? Lon, DateTime StartedAt, bool IsTest, string UserName = "");

public interface IBackendClient
{
	Task<bool> SendOtpAsync(string phone);
	Task<bool> VerifyOtpAsync(string phone, string code);

	Task<string?> RegisterAlertAsync(AlertPayload payload);
	Task<Dictionary<string, DeliveryState>> GetAlertStatusesAsync(string alertId, IEnumerable<string> contactIds);
	Task<Dictionary<string, ContactStatus>> GetInvitationStatusesAsync(IEnumerable<string> contactIds);
	Task<Dictionary<string, string>> GetEmergencyNumbersAsync();
	Task<bool> DeleteAccountAsync(string phone);
	Task<string?> RegisterContactAsync(TrustedContact contact);
	Task RemoveContactAsync(string contactId);
	Task UpdatePositionAsync(string alertId, double lat, double lon);
	Task CloseAlertAsync(string alertId);
}

public class DemoBackendClient : IBackendClient
{
	readonly Dictionary<string, DateTime> _alertStart = new();
	readonly Dictionary<string, DateTime> _inviteStart = new();

	public Task<bool> SendOtpAsync(string phone) => Task.FromResult(true);
	public Task<bool> VerifyOtpAsync(string phone, string code) => Task.FromResult(code == AppConfig.DemoOtp);
	public Task<string?> RegisterAlertAsync(AlertPayload payload) => Task.FromResult<string?>(null);

	public Task<Dictionary<string, DeliveryState>> GetAlertStatusesAsync(string alertId, IEnumerable<string> contactIds)
	{
		if (!_alertStart.ContainsKey(alertId)) _alertStart[alertId] = DateTime.Now;
		var elapsed = (DateTime.Now - _alertStart[alertId]).TotalSeconds;
		var map = new Dictionary<string, DeliveryState>();
		var i = 0;
		foreach (var id in contactIds)
		{
			map[id] = i switch
			{
				0 => elapsed > 14 ? DeliveryState.Confirme : elapsed > 3 ? DeliveryState.Delivre : DeliveryState.Envoye,
				1 => elapsed > 8 ? DeliveryState.Vu : elapsed > 3 ? DeliveryState.Delivre : DeliveryState.Envoye,
				_ => DeliveryState.Delivre
			};
			i++;
		}
		return Task.FromResult(map);
	}

	public Task<Dictionary<string, ContactStatus>> GetInvitationStatusesAsync(IEnumerable<string> contactIds)
	{
		var map = new Dictionary<string, ContactStatus>();
		foreach (var id in contactIds)
		{
			if (!_inviteStart.ContainsKey(id)) _inviteStart[id] = DateTime.Now;
			map[id] = (DateTime.Now - _inviteStart[id]).TotalSeconds > 20 ? ContactStatus.Accepte : ContactStatus.EnAttente;
		}
		return Task.FromResult(map);
	}

	public Task<Dictionary<string, string>> GetEmergencyNumbersAsync() => Task.FromResult(new Dictionary<string, string>());
	public Task<bool> DeleteAccountAsync(string phone) => Task.FromResult(true);
	public Task<string?> RegisterContactAsync(TrustedContact contact) => Task.FromResult<string?>(null);
	public Task RemoveContactAsync(string contactId) => Task.CompletedTask;
	public Task UpdatePositionAsync(string alertId, double lat, double lon) => Task.CompletedTask;
	public Task CloseAlertAsync(string alertId) => Task.CompletedTask;
}

public class HttpBackendClient : IBackendClient
{
	const string TokenKey = "api_token";
	readonly HttpClient _http = new() { BaseAddress = new Uri(AppConfig.ApiBaseUrl), Timeout = TimeSpan.FromSeconds(10) };

	public HttpBackendClient() => ApplyToken(Preferences.Default.Get(TokenKey, ""));

	void ApplyToken(string token)
	{
		_http.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(token)
			? null
			: new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
	}

	static async Task<T> Try<T>(Func<Task<T>> call, T fallback)
	{
		try { return await call(); } catch { return fallback; }
	}

	public Task<bool> SendOtpAsync(string phone) => Try(async () =>
		(await _http.PostAsJsonAsync("auth/otp", new { phone })).IsSuccessStatusCode, false);

	public Task<bool> VerifyOtpAsync(string phone, string code) => Try(async () =>
	{
		var r = await _http.PostAsJsonAsync("auth/verify", new { phone, code });
		if (!r.IsSuccessStatusCode) return false;
		var body = await r.Content.ReadFromJsonAsync<Dictionary<string, string>>();
		if (body is null || !body.TryGetValue("token", out var token) || string.IsNullOrEmpty(token)) return false;
		Preferences.Default.Set(TokenKey, token);
		ApplyToken(token);
		return true;
	}, false);

	public Task<string?> RegisterAlertAsync(AlertPayload p) => Try<string?>(async () =>
	{
		var r = await _http.PostAsJsonAsync("alerts", p);
		if (!r.IsSuccessStatusCode) return null;
		var body = await r.Content.ReadFromJsonAsync<Dictionary<string, string>>();
		return body is not null && body.TryGetValue("trackingUrl", out var u) ? u : null;
	}, null);

	public Task<Dictionary<string, DeliveryState>> GetAlertStatusesAsync(string alertId, IEnumerable<string> contactIds) =>
		Try(async () => await _http.GetFromJsonAsync<Dictionary<string, DeliveryState>>($"alerts/{alertId}/status") ?? new(), new Dictionary<string, DeliveryState>());

	public Task<Dictionary<string, ContactStatus>> GetInvitationStatusesAsync(IEnumerable<string> contactIds) =>
		Try(async () =>
		{
			var r = await _http.PostAsJsonAsync("contacts/status", contactIds.ToList());
			return await r.Content.ReadFromJsonAsync<Dictionary<string, ContactStatus>>() ?? new();
		}, new Dictionary<string, ContactStatus>());

	public Task<Dictionary<string, string>> GetEmergencyNumbersAsync() =>
		Try(async () => await _http.GetFromJsonAsync<Dictionary<string, string>>("config/emergency-numbers") ?? new(), new Dictionary<string, string>());

	public Task<bool> DeleteAccountAsync(string phone) => Try(async () =>
	{
		var ok = (await _http.DeleteAsync($"account?phone={Uri.EscapeDataString(phone)}")).IsSuccessStatusCode;
		if (ok)
		{
			Preferences.Default.Remove(TokenKey);
			ApplyToken("");
		}
		return ok;
	}, false);

	public Task<string?> RegisterContactAsync(TrustedContact contact) => Try<string?>(async () =>
	{
		var r = await _http.PostAsJsonAsync("contacts", new { id = contact.Id, fullName = contact.FullName, phone = contact.Phone });
		if (!r.IsSuccessStatusCode) return null;
		var body = await r.Content.ReadFromJsonAsync<Dictionary<string, string>>();
		return body is not null && body.TryGetValue("inviteUrl", out var u) ? u : null;
	}, null);

	public Task RemoveContactAsync(string contactId) => Try(async () =>
		(await _http.DeleteAsync($"contacts/{Uri.EscapeDataString(contactId)}")).IsSuccessStatusCode, false);

	public Task UpdatePositionAsync(string alertId, double lat, double lon) => Try(async () =>
		(await _http.PostAsJsonAsync($"alerts/{Uri.EscapeDataString(alertId)}/position", new { lat, lon })).IsSuccessStatusCode, false);

	public Task CloseAlertAsync(string alertId) => Try(async () =>
		(await _http.PostAsync($"alerts/{Uri.EscapeDataString(alertId)}/close", null)).IsSuccessStatusCode, false);
}

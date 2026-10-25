using System.Text.Json;
using AlerteProche.Models;

namespace AlerteProche.Services;

public class EmergencyNumbersService
{
	static readonly (string key, string icon, string number)[] Defaults =
	{
		("police", "👮", "117"),
		("gendarmerie", "🚓", "113"),
		("pompiers", "🔥", "118"),
		("samu", "➕", "119"),
	};

	Dictionary<string, string> _overrides = Load("num_override");
	Dictionary<string, string> _remote = Load("num_remote");

	static Dictionary<string, string> Load(string key)
	{
		try { return JsonSerializer.Deserialize<Dictionary<string, string>>(Preferences.Default.Get(key, "{}")) ?? new(); }
		catch { return new(); }
	}

	public List<EmergencyNumber> GetAll() => Defaults.Select(d => new EmergencyNumber
	{
		Key = d.key,
		Icon = d.icon,
		Number = _overrides.TryGetValue(d.key, out var o) ? o : _remote.TryGetValue(d.key, out var r) ? r : d.number
	}).ToList();

	public string PoliceNumber => GetAll().First(n => n.Key == "police").Number;

	public void SetOverride(string key, string number)
	{
		_overrides[key] = number.Trim();
		Preferences.Default.Set("num_override", JsonSerializer.Serialize(_overrides));
	}

	public async Task RefreshRemoteAsync(IBackendClient backend)
	{
		var remote = await backend.GetEmergencyNumbersAsync();
		if (remote.Count == 0) return;
		_remote = remote;
		Preferences.Default.Set("num_remote", JsonSerializer.Serialize(_remote));
	}

	public Task DialAsync(string number)
	{
		try { if (PhoneDialer.Default.IsSupported) PhoneDialer.Default.Open(number); }
		catch { }
		return Task.CompletedTask;
	}
}

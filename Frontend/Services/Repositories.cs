using System.Text.Json;
using AlerteProche.Models;

namespace AlerteProche.Services;

static class Store
{
	public static List<T> Load<T>(string key)
	{
		try
		{
			var json = Preferences.Default.Get(key, "");
			return string.IsNullOrEmpty(json) ? new List<T>() : JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
		}
		catch { return new List<T>(); }
	}

	public static void Save<T>(string key, List<T> list) => Preferences.Default.Set(key, JsonSerializer.Serialize(list));
}

public class ContactsRepository
{
	const string Key = "contacts";
	List<TrustedContact> _cache;

	public ContactsRepository() => _cache = Store.Load<TrustedContact>(Key);

	public IReadOnlyList<TrustedContact> All => _cache.Where(c => c.Status != ContactStatus.Retire).ToList();
	public bool CanAdd => All.Count < AppConfig.MaxContacts;
	public TrustedContact? Primary => All.FirstOrDefault(c => c.Role == ContactRole.Principal) ?? All.FirstOrDefault();
	public TrustedContact? Find(string id) => _cache.FirstOrDefault(c => c.Id == id);

	public TrustedContact Add(string fullName, string phone)
	{
		var c = new TrustedContact
		{
			FullName = fullName.Trim(),
			Phone = phone,
			Role = All.Any(x => x.Role == ContactRole.Principal) ? ContactRole.Secondaire : ContactRole.Principal,
			Status = ContactStatus.EnAttente
		};
		_cache.Add(c);
		Save();
		return c;
	}

	public void Update(TrustedContact c)
	{
		var i = _cache.FindIndex(x => x.Id == c.Id);
		if (i >= 0) _cache[i] = c;
		Save();
	}

	public void SetPrincipal(string id)
	{
		foreach (var c in _cache) c.Role = c.Id == id ? ContactRole.Principal : ContactRole.Secondaire;
		Save();
	}

	public void Remove(string id)
	{
		var c = Find(id);
		if (c is null) return;
		_cache.Remove(c);

		if (c.Role == ContactRole.Principal && All.Count > 0) All[0].Role = ContactRole.Principal;
		Save();
	}

	public List<TrustedContact> Recipients()
	{
		var accepted = All.Where(c => c.Status == ContactStatus.Accepte).ToList();
		var list = accepted.Count > 0 ? accepted : All.ToList();
		return list.OrderBy(c => c.Role == ContactRole.Principal ? 0 : 1).ToList();
	}

	public async Task RefreshStatusesAsync(IBackendClient backend)
	{
		var map = await backend.GetInvitationStatusesAsync(All.Select(c => c.Id));
		var changed = false;
		foreach (var c in _cache)
			if (map.TryGetValue(c.Id, out var st) && st != c.Status) { c.Status = st; changed = true; }
		if (changed) Save();
	}

	public void Clear() { _cache = new(); Save(); }
	void Save() => Store.Save(Key, _cache);
}

public class HistoryRepository
{
	const string Key = "history";
	List<AlertRecord> _cache;

	public HistoryRepository() => _cache = Store.Load<AlertRecord>(Key);

	public IReadOnlyList<AlertRecord> All => _cache.OrderByDescending(r => r.StartedAt).ToList();

	public void Add(AlertRecord r) { _cache.Add(r); Save(); }

	public void SetKind(string id, AlertKind kind)
	{
		var r = _cache.FirstOrDefault(x => x.Id == id);
		if (r is null) return;
		r.Kind = kind;
		Save();
	}

	public void Clear() { _cache = new(); Save(); }
	void Save() => Store.Save(Key, _cache);
}

public class ConsentRepository
{
	const string Key = "consents";
	List<ConsentProof> _cache;

	public ConsentRepository() => _cache = Store.Load<ConsentProof>(Key);

	public IReadOnlyList<ConsentProof> All => _cache.OrderByDescending(c => c.At).ToList();

	public void Record(string type, bool granted)
	{
		_cache.Add(new ConsentProof { Type = type, Granted = granted, At = DateTime.Now });
		Store.Save(Key, _cache);
	}

	public void Clear() { _cache = new(); Store.Save(Key, _cache); }
}

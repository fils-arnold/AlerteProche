using AlerteProche.Models;

namespace AlerteProche.Services;

/// <summary>
/// Envoi des SMS avec file d'attente locale : si l'envoi échoue (pas de réseau mobile), le SMS est gardé
/// et renvoyé dès que possible (EF-20, ENF-02 : aucun envoi perdu).
/// </summary>
public class SmsDispatcher
{
	const string Key = "sms_queue";
	readonly ISmsSender _sender;
	public SmsDispatcher(ISmsSender sender) => _sender = sender;

	public int PendingCount => Store.Load<PendingSms>(Key).Count;

	/// <summary>true = envoyé ; false = mis en file d'attente.</summary>
	public async Task<bool> SendAsync(string phone, string text)
	{
		if (await _sender.SendAsync(phone, text)) return true;
		var queue = Store.Load<PendingSms>(Key);
		queue.Add(new PendingSms { Phone = phone, Text = text });
		Store.Save(Key, queue);
		return false;
	}

	/// <summary>Renvoie les SMS en attente. Appelé régulièrement pendant une alerte.</summary>
	public async Task FlushAsync()
	{
		var queue = Store.Load<PendingSms>(Key);
		if (queue.Count == 0) return;
		var rest = new List<PendingSms>();
		foreach (var p in queue)
			if (!await _sender.SendAsync(p.Phone, p.Text)) rest.Add(p);
		Store.Save(Key, rest);
	}
}

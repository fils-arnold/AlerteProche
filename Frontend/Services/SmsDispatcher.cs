using AlerteProche.Models;

namespace AlerteProche.Services;

public class SmsDispatcher
{
	const string Key = "sms_queue";
	readonly ISmsSender _sender;
	public SmsDispatcher(ISmsSender sender) => _sender = sender;

	public int PendingCount => Store.Load<PendingSms>(Key).Count;

	public async Task<bool> SendAsync(string phone, string text)
	{
		if (await _sender.SendAsync(phone, text)) return true;
		var queue = Store.Load<PendingSms>(Key);
		queue.Add(new PendingSms { Phone = phone, Text = text });
		Store.Save(Key, queue);
		return false;
	}

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

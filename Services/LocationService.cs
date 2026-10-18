namespace AlerteProche.Services;

public class LocationService
{
	/// <summary>Position actuelle ; à défaut de GPS, dernière position connue (EF-26).</summary>
	public async Task<Location?> GetAsync()
	{
		try
		{
			var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(8));
			var l = await Geolocation.Default.GetLocationAsync(request);
			if (l is not null) return l;
		}
		catch { /* permission refusée, GPS coupé... on tente la dernière position */ }

		try { return await Geolocation.Default.GetLastKnownLocationAsync(); }
		catch { return null; }
	}
}

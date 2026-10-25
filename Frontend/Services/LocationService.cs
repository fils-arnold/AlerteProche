namespace AlerteProche.Services;

public class LocationService
{

	public async Task<Location?> GetAsync()
	{
		try
		{
			var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(8));
			var l = await Geolocation.Default.GetLocationAsync(request);
			if (l is not null) return l;
		}
		catch { }

		try { return await Geolocation.Default.GetLastKnownLocationAsync(); }
		catch { return null; }
	}
}

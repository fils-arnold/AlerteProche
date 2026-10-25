namespace AlerteProche.Helpers;

public static class Loc
{
	public static string Language { get; set; } = "fr";

	public static string T(string key)
	{
		if (Strings.Table.TryGetValue(key, out var v))
			return Language == "en" ? v.en : v.fr;
		return key;
	}

	public static string T(string key, params object[] args) => string.Format(T(key), args);
}

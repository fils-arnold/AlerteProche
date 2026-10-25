using AlerteProche.Models;

namespace AlerteProche.Helpers;

public static class QualifyHelper
{

	public static async Task<AlertKind?> AskAsync()
	{
		var later = Loc.T("qualify_later");
		var choice = await Shell.Current.DisplayActionSheet(Loc.T("qualify_title"), later, null,
			Loc.T("kind_true"), Loc.T("kind_false"), Loc.T("kind_test"));
		if (choice == Loc.T("kind_true")) return AlertKind.Vraie;
		if (choice == Loc.T("kind_false")) return AlertKind.Fausse;
		if (choice == Loc.T("kind_test")) return AlertKind.Test;
		return null;
	}
}

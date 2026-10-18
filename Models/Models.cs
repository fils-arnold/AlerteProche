using System.Globalization;
using System.Text.Json.Serialization;
using AlerteProche.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlerteProche.Models;

public enum ContactRole { Principal, Secondaire }
public enum ContactStatus { EnAttente, Accepte, Retire }

/// <summary>États d'une alerte pour chaque proche (EF-30). L'ordre sert à ne jamais « reculer » d'un état.</summary>
public enum DeliveryState { EnAttente = 0, EnFile = 1, Envoye = 2, Delivre = 3, Vu = 4, Confirme = 5 }

public enum AlertKind { NonQualifiee, Vraie, Fausse, Test }

public class TrustedContact
{
	public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
	public string FullName { get; set; } = "";
	/// <summary>Numéro au format international, par exemple +237612345678.</summary>
	public string Phone { get; set; } = "";
	public ContactRole Role { get; set; }
	public ContactStatus Status { get; set; }

	[JsonIgnore] public string PhoneDisplay => Phone.StartsWith("+237") && Phone.Length == 13
		? $"{Phone[..4]} {Phone[4..5]} {Phone[5..7]} {Phone[7..9]} {Phone[9..11]} {Phone[11..]}"
		: Phone;
	[JsonIgnore] public string RoleLabel => Loc.T(Role == ContactRole.Principal ? "role_principal" : "role_secondary");
	[JsonIgnore] public Color RoleBg => Role == ContactRole.Principal ? Color.FromArgb("#D8EFDD") : Color.FromArgb("#DCE8FB");
	[JsonIgnore] public Color RoleFg => Role == ContactRole.Principal ? Color.FromArgb("#1C9B45") : Color.FromArgb("#1D4FB5");
	[JsonIgnore] public string StatusLabel => Status switch
	{
		ContactStatus.Accepte => "✔ " + Loc.T("st_accepted"),
		ContactStatus.Retire => "✖ " + Loc.T("st_removed"),
		_ => "⏳ " + Loc.T("st_pending")
	};
	[JsonIgnore] public Color StatusColor => Status switch
	{
		ContactStatus.Accepte => Color.FromArgb("#1C9B45"),
		ContactStatus.Retire => Color.FromArgb("#6B7280"),
		_ => Color.FromArgb("#EA6F0B")
	};
}

public class AlertRecord
{
	public string Id { get; set; } = "";
	public DateTime StartedAt { get; set; }
	public int DurationSeconds { get; set; }
	public AlertKind Kind { get; set; }
	public List<string> ConfirmedNames { get; set; } = new();
	public double? Latitude { get; set; }
	public double? Longitude { get; set; }

	static CultureInfo Culture => new(Loc.Language == "en" ? "en-US" : "fr-FR");

	[JsonIgnore] public string DateText => StartedAt.ToString("d MMMM yyyy", Culture);
	[JsonIgnore] public string TimeText => StartedAt.ToString("HH:mm", Culture);
	[JsonIgnore] public string DurationText => DurationSeconds < 60 ? $"{DurationSeconds} s" : $"{DurationSeconds / 60} min {DurationSeconds % 60:00} s";
	[JsonIgnore] public string KindLabel => Kind switch
	{
		AlertKind.Vraie => "❗ " + Loc.T("kind_true"),
		AlertKind.Fausse => "⚠ " + Loc.T("kind_false"),
		AlertKind.Test => "✔ " + Loc.T("kind_test"),
		_ => Loc.T("kind_unqualified")
	};
	[JsonIgnore] public Color KindColor => Kind switch
	{
		AlertKind.Vraie => Color.FromArgb("#DC2626"),
		AlertKind.Fausse => Color.FromArgb("#EA6F0B"),
		AlertKind.Test => Color.FromArgb("#1D4FB5"),
		_ => Color.FromArgb("#6B7280")
	};
	[JsonIgnore] public string ConfirmedLabel => $"{Loc.T("hist_confirmed")} ({ConfirmedNames.Count})";
	[JsonIgnore] public string ConfirmedNamesText => ConfirmedNames.Count == 0 ? "-" : string.Join(", ", ConfirmedNames);
}

/// <summary>Ligne affichée sur l'écran « Alerte active » : un proche et son état.</summary>
public partial class ContactDelivery : ObservableObject
{
	public string ContactId { get; init; } = "";
	public string Name { get; init; } = "";

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Label), nameof(Foreground), nameof(Background))]
	private DeliveryState _state = DeliveryState.EnAttente;

	public string Label => State switch
	{
		DeliveryState.EnFile => "⏳ " + Loc.T("ds_queued"),
		DeliveryState.Envoye => "➤ " + Loc.T("ds_sent"),
		DeliveryState.Delivre => "✔ " + Loc.T("ds_delivered"),
		DeliveryState.Vu => "👁 " + Loc.T("ds_seen"),
		DeliveryState.Confirme => "✔ " + Loc.T("ds_confirmed"),
		_ => Loc.T("ds_reserve")
	};
	public Color Foreground => State switch
	{
		DeliveryState.Envoye => Color.FromArgb("#1D6FE0"),
		DeliveryState.Vu => Color.FromArgb("#6D35C4"),
		DeliveryState.Confirme => Color.FromArgb("#1C9B45"),
		DeliveryState.EnFile => Color.FromArgb("#EA6F0B"),
		_ => Color.FromArgb("#6B7280")
	};
	public Color Background => State switch
	{
		DeliveryState.Envoye => Color.FromArgb("#E3EEFC"),
		DeliveryState.Vu => Color.FromArgb("#F0E8FA"),
		DeliveryState.Confirme => Color.FromArgb("#E3F4E8"),
		DeliveryState.EnFile => Color.FromArgb("#FDEEDD"),
		_ => Color.FromArgb("#F3F4F6")
	};
}

public class EmergencyNumber
{
	public string Key { get; set; } = "";
	public string Icon { get; set; } = "";
	public string Number { get; set; } = "";
	public string Label => Loc.T("num_" + Key);
}

public class PendingSms
{
	public string Phone { get; set; } = "";
	public string Text { get; set; } = "";
}

public class ConsentProof
{
	public string Type { get; set; } = "";
	public bool Granted { get; set; }
	public DateTime At { get; set; }
}

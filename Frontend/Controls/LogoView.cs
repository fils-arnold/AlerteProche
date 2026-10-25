namespace AlerteProche.Controls;

public class LogoView : Label
{
	readonly Span _accent = new() { Text = "Proche", FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1C9B45") };

	public LogoView()
	{
		FontSize = 28;
		HorizontalOptions = LayoutOptions.Center;
		var fs = new FormattedString();
		fs.Spans.Add(new Span { Text = "Alerte", FontAttributes = FontAttributes.Bold });
		fs.Spans.Add(_accent);
		FormattedText = fs;
		SemanticProperties.SetDescription(this, "AlerteProche");
	}

	public Color Accent { get => _accent.TextColor; set => _accent.TextColor = value; }
}

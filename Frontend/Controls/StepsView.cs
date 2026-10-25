using Microsoft.Maui.Controls.Shapes;

namespace AlerteProche.Controls;

public class StepsView : HorizontalStackLayout
{
	public static readonly BindableProperty CurrentProperty = BindableProperty.Create(
		nameof(Current), typeof(int), typeof(StepsView), 1, propertyChanged: (b, _, _) => ((StepsView)b).Build());

	public int Current { get => (int)GetValue(CurrentProperty); set => SetValue(CurrentProperty, value); }

	public StepsView()
	{
		Spacing = 14;
		HorizontalOptions = LayoutOptions.Center;
		Build();
	}

	void Build()
	{
		Children.Clear();
		for (var i = 1; i <= 4; i++)
		{
			var active = i == Current;
			var done = i < Current;
			Children.Add(new Border
			{
				WidthRequest = 40,
				HeightRequest = 40,
				StrokeThickness = 0,
				StrokeShape = new RoundRectangle { CornerRadius = 20 },
				BackgroundColor = active || done ? Color.FromArgb("#1C9B45") : Color.FromArgb("#E5E7EB"),
				Content = new Label
				{
					Text = done ? "✔" : i.ToString(),
					FontAttributes = FontAttributes.Bold,
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center,
					TextColor = active || done ? Colors.White : Color.FromArgb("#6B7280")
				}
			});
		}
	}
}

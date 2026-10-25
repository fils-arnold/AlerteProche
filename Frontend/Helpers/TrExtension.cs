using Microsoft.Maui.Controls.Xaml;

namespace AlerteProche.Helpers;

[ContentProperty(nameof(Key))]
public class TrExtension : IMarkupExtension<string>
{
	public string Key { get; set; } = "";

	public string ProvideValue(IServiceProvider serviceProvider) => Loc.T(Key);

	object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
}

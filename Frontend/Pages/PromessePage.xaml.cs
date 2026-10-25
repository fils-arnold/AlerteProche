using AlerteProche.ViewModels;

namespace AlerteProche.Pages;

public partial class PromessePage : ContentPage
{
	readonly PromesseViewModel _vm;

	public PromessePage(PromesseViewModel vm)
	{
		InitializeComponent();
		BindingContext = _vm = vm;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _vm.OnAppearingAsync();
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		_vm.OnDisappearing();
	}
}

using AlerteProche.ViewModels;

namespace AlerteProche.Pages;

public partial class JeRentrePage : ContentPage
{
	readonly JeRentreViewModel _vm;

	public JeRentrePage(JeRentreViewModel vm)
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

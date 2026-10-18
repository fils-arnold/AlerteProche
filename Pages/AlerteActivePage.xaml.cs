using AlerteProche.ViewModels;

namespace AlerteProche.Pages;

public partial class AlerteActivePage : ContentPage
{
	readonly AlerteActiveViewModel _vm;

	public AlerteActivePage(AlerteActiveViewModel vm)
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

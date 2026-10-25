using AlerteProche.ViewModels;

namespace AlerteProche.Pages;

public partial class RecuperationPage : ContentPage
{
	readonly RecuperationViewModel _vm;

	public RecuperationPage(RecuperationViewModel vm)
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

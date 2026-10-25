using AlerteProche.ViewModels;

namespace AlerteProche.Pages;

public partial class AccueilPage : ContentPage
{
	readonly AccueilViewModel _vm;

	public AccueilPage(AccueilViewModel vm)
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

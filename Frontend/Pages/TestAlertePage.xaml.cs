using AlerteProche.ViewModels;

namespace AlerteProche.Pages;

public partial class TestAlertePage : ContentPage
{
	readonly TestAlerteViewModel _vm;

	public TestAlertePage(TestAlerteViewModel vm)
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

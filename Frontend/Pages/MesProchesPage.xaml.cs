using AlerteProche.ViewModels;

namespace AlerteProche.Pages;

public partial class MesProchesPage : ContentPage
{
	readonly MesProchesViewModel _vm;

	public MesProchesPage(MesProchesViewModel vm)
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

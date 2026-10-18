using AlerteProche.ViewModels;

namespace AlerteProche.Pages;

public partial class CodeSmsPage : ContentPage
{
	readonly CodeSmsViewModel _vm;

	public CodeSmsPage(CodeSmsViewModel vm)
	{
		InitializeComponent();
		BindingContext = _vm = vm;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _vm.OnAppearingAsync();
		Dispatcher.Dispatch(() => CodeEntry.Focus());
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		_vm.OnDisappearing();
	}
}

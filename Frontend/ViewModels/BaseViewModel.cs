using AlerteProche.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlerteProche.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasError))]
	private string _error = "";

	[ObservableProperty] private bool _isBusy;

	public bool HasError => !string.IsNullOrEmpty(Error);

	public virtual Task OnAppearingAsync() => Task.CompletedTask;

	public virtual void OnDisappearing() { }

	[RelayCommand]
	private Task Back() => Shell.Current.GoToAsync("..");

	protected static Task GoAsync(string route) => Shell.Current.GoToAsync(route);

	protected static Task ShowAsync(string title, string message) =>
		Shell.Current.DisplayAlert(title, message, Loc.T("common_ok"));

	protected static Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) =>
		Shell.Current.DisplayAlert(title, message, accept, cancel);

	protected static Task<string?> PromptAsync(string title, string message, string initial = "", Keyboard? keyboard = null, int maxLength = -1) =>
		Shell.Current.DisplayPromptAsync(title, message, Loc.T("common_ok"), Loc.T("common_cancel"), null, maxLength, keyboard ?? Keyboard.Default, initial);
}

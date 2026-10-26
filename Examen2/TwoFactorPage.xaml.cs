using Examen2.ViewModels;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace Examen2;

public partial class TwoFactorPage : ContentPage
{
    private readonly User _currentUser;
    private readonly string _expectedCode;

    public TwoFactorPage(User user, string expectedCode)
    {
        InitializeComponent();
        _currentUser = user;
        _expectedCode = expectedCode;

        LblEmailInfo.Text = $"Hemos enviado un código de verificación de 6 dígitos a:\n{_currentUser.Email}";
    }

    private async void OnVerifyClicked(object sender, EventArgs e)
    {
        string inputCode = TxtCode.Text?.Trim();

        if (inputCode == _expectedCode)
        {
            var mainViewModel = Handler.MauiContext.Services.GetService<MainViewModel>();
            mainViewModel.SetCurrentUser(_currentUser);

            var mainPage = Handler.MauiContext.Services.GetService<MainPage>();

            if (Application.Current?.Windows.Count > 0)
            {
                Application.Current.Windows[0].Page = new NavigationPage(mainPage);
            }
        }
        else
        {
            await DisplayAlert("Código incorrecto", "El código ingresado no coincide.", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
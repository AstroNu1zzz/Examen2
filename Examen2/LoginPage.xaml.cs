using Examen2.Data;
using Examen2.Services;
using Examen2.ViewModels;
using Microsoft.Maui.Storage;

namespace Examen2;

public partial class LoginPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly EmailService _emailService;
    private readonly MainViewModel _mainViewModel;

    public LoginPage(DatabaseService databaseService, MainViewModel mainViewModel)
    {
        InitializeComponent();
        _databaseService = databaseService;
        _mainViewModel = mainViewModel;
        _emailService = new EmailService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Comprobación de sesión guardada al cargar la vista
        int savedUserId = Preferences.Default.Get("LoggedInUserId", -1);
        if (savedUserId != -1)
        {
            var user = await _databaseService.GetUserByIdAsync(savedUserId);
            if (user != null)
            {
                _mainViewModel.SetCurrentUser(user);
                var mainPage = Handler.MauiContext.Services.GetService<MainPage>();

                if (Application.Current?.Windows.Count > 0 && mainPage != null)
                {
                    Application.Current.Windows[0].Page = new NavigationPage(mainPage);
                }
            }
        }
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        string userInput = TxtUser.Text?.Trim();
        string passwordInput = TxtPassword.Text;

        if (string.IsNullOrEmpty(userInput) || string.IsNullOrEmpty(passwordInput))
        {
            await DisplayAlert("Error", "Ingresa usuario y contraseña.", "OK");
            return;
        }

        User user = await _databaseService.ValidateUserAsync(userInput, passwordInput);

        if (user != null)
        {
            string generatedCode = Random.Shared.Next(100000, 999999).ToString();

            var btn = (Button)sender;
            btn.IsEnabled = false;
            btn.Text = "Enviando código...";

            bool emailSent = await _emailService.Send2FACodeAsync(user.Email, generatedCode);

            btn.IsEnabled = true;
            btn.Text = "Siguiente";

            if (emailSent)
            {
                await DisplayAlert("Código enviado", $"Hemos enviado un código de verificación real a {user.Email}", "Continuar");
                await Navigation.PushAsync(new TwoFactorPage(user, generatedCode));
            }
            else
            {
                await DisplayAlert("Error de envío", "No se pudo enviar el correo de verificación. Revisa la configuración SMTP o tu conexión.", "OK");
            }
        }
        else
        {
            await DisplayAlert("Error de autenticación", "Usuario o contraseña incorrectos.", "OK");
        }
    }

    private async void OnRegisterNavigationClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegisterPage(_databaseService));
    }
}
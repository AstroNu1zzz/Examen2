using Examen2.Data;
using Examen2.Services;
using Examen2.ViewModels;

namespace Examen2;

public partial class LoginPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly EmailService _emailService;
    private readonly MainViewModel _mainViewModel;

    public LoginPage(DatabaseService databaseService = null, EmailService emailService = null, MainViewModel mainViewModel = null)
    {
        InitializeComponent();
        _databaseService = databaseService ?? Application.Current?.Handler?.MauiContext?.Services.GetService<DatabaseService>();
        _emailService = emailService ?? Application.Current?.Handler?.MauiContext?.Services.GetService<EmailService>();
        _mainViewModel = mainViewModel ?? Application.Current?.Handler?.MauiContext?.Services.GetService<MainViewModel>();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        string usernameOrEmail = UsernameEntry.Text?.Trim();
        string password = PasswordEntry.Text?.Trim();

        if (string.IsNullOrEmpty(usernameOrEmail) || string.IsNullOrEmpty(password))
        {
            await DisplayAlert("Campo requerido", "Por favor ingresa tu usuario/correo y contraseña.", "OK");
            return;
        }

        LoginButton.IsEnabled = false;

        try
        {
            // 1. Consultar credenciales en Supabase
            var user = await _databaseService.ValidateUserAsync(usernameOrEmail, password);

            if (user == null)
            {
                await DisplayAlert("Error de acceso", "Usuario o contraseña incorrectos.", "OK");
                LoginButton.IsEnabled = true;
                return;
            }

            // 2. Evaluar el estado de 2FA configurado en la base de datos
            if (user.Is2FAEnabled)
            {
                // Si 2FA está ACTIVADO: Generar código, enviar por correo y navegar a TwoFactorPage
                string verificationCode = new Random().Next(100000, 999999).ToString();
                await _emailService.Send2FACodeAsync(user.Email, verificationCode);

                LoginButton.IsEnabled = true;
                await Navigation.PushAsync(new TwoFactorPage(user, verificationCode, _databaseService, _mainViewModel));
            }
            else
            {
                // Si 2FA está DESACTIVADO: Entrar directo a MainPage
                Preferences.Default.Set("IsLoggedIn", true);
                Preferences.Default.Set("CurrentUserId", user.Id);

                _mainViewModel.SetCurrentUser(user);

                LoginButton.IsEnabled = true;
                Application.Current.MainPage = new NavigationPage(new MainPage(_databaseService, _mainViewModel));
            }
        }
        catch (Exception ex)
        {
            LoginButton.IsEnabled = true;
            await DisplayAlert("Error inesperado", $"No se pudo iniciar sesión: {ex.Message}", "OK");
        }
    }

    private async void OnRegisterNavigationClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegisterPage(_databaseService));
    }
}
using System;
using Examen2.Data;
using Examen2.Services;
using Examen2.ViewModels;

namespace Examen2;

public partial class LoginPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly EmailService _emailService;
    private readonly MainViewModel _mainViewModel;

    public LoginPage(DatabaseService databaseService, EmailService emailService, MainViewModel mainViewModel)
    {
        InitializeComponent();
        _databaseService = databaseService;
        _emailService = emailService;
        _mainViewModel = mainViewModel;
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(UsernameEntry.Text) || string.IsNullOrWhiteSpace(PasswordEntry.Text))
        {
            await DisplayAlert("Atención", "Por favor ingresa usuario y contraseña.", "OK");
            return;
        }

        LoginButton.Text = "Enviando código...";
        LoginButton.IsEnabled = false;

        try
        {
            var user = await _databaseService.GetUserByUsernameOrEmailAsync(UsernameEntry.Text.Trim());

            if (user == null || user.Password != PasswordEntry.Text)
            {
                await DisplayAlert("Error", "Usuario o contraseña incorrectos.", "OK");
                return;
            }

            string verificationCode = new Random().Next(100000, 999999).ToString();

            var (isSent, errorDetail) = await _emailService.Send2FACodeAsync(user.Email, verificationCode);

            if (isSent)
            {
                await Navigation.PushAsync(new TwoFactorPage(user, verificationCode, _databaseService, _mainViewModel));
            }
            else
            {
                // Despliega el error real en la pantalla del celular
                await DisplayAlert("Fallo en SMTP", $"Detalle técnico:\n{errorDetail}", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Ocurrió un problema: {ex.Message}", "OK");
        }
        finally
        {
            LoginButton.Text = "Siguiente";
            LoginButton.IsEnabled = true;
        }
    }

    private async void OnRegisterNavigationClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegisterPage(_databaseService));
    }
}
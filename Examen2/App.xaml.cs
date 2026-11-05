using System;
using Examen2.Data;
using Examen2.Services;
using Examen2.ViewModels;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace Examen2;

public partial class App : Application
{
    private readonly DatabaseService _databaseService;
    private readonly EmailService _emailService;
    private readonly MainViewModel _mainViewModel;

    public App(DatabaseService databaseService, EmailService emailService, MainViewModel mainViewModel)
    {
        InitializeComponent();

        _databaseService = databaseService;
        _emailService = emailService;
        _mainViewModel = mainViewModel;

        // 1. Asignación inmediata para evitar que Android suspenda la app en el Splash Screen
        MainPage = new NavigationPage(new LoginPage(_databaseService, _emailService, _mainViewModel));

        // 2. Verificación asíncrona sin bloqueos síncronos (.GetAwaiter().GetResult())
        CheckSessionAsync();
    }

    private async void CheckSessionAsync()
    {
        try
        {
            bool isLoggedIn = Preferences.Default.Get("IsLoggedIn", false);
            int userId = Preferences.Default.Get("CurrentUserId", -1);
            DateTime last2FA = Preferences.Default.Get("Last2FAVerifiedAt", DateTime.MinValue);

            bool is2FAExpired = (DateTime.UtcNow - last2FA).TotalDays > 30;

            if (isLoggedIn && userId != -1 && !is2FAExpired)
            {
                // Consulta asíncrona real a SQLite sin congelar la UI
                var user = await _databaseService.GetUserByIdAsync(userId);

                if (user != null)
                {
                    _mainViewModel.CurrentUser = user;

                    // Redirección en el hilo principal una vez confirmada la sesión
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        MainPage = new NavigationPage(new MainPage(_databaseService, _mainViewModel));
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[App Session Check Error]: {ex.Message}");
        }
    }
}
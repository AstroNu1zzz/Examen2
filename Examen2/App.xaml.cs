using System;
using System.Threading.Tasks;
using Examen2.Data;
using Examen2.Services;
using Examen2.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace Examen2;

public partial class App : Application
{
    public App(DatabaseService databaseService, EmailService emailService, MainViewModel mainViewModel)
    {
        InitializeComponent();

        bool isLoggedIn = Preferences.Default.Get("IsLoggedIn", false);
        int userId = Preferences.Default.Get("CurrentUserId", -1);
        DateTime last2FA = Preferences.Default.Get("Last2FAVerifiedAt", DateTime.MinValue);

        // Configurar expiración de 2FA (ejemplo: pedir 2FA cada 30 días)
        bool is2FAExpired = (DateTime.UtcNow - last2FA).TotalDays > 30;

        if (isLoggedIn && userId != -1 && !is2FAExpired)
        {
            // Cargar usuario e ir directo a MainPage sin pasar por Login ni 2FA
            Task.Run(async () =>
            {
                var user = await databaseService.GetUserByIdAsync(userId);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    mainViewModel.CurrentUser = user;
                    MainPage = new NavigationPage(new MainPage(databaseService, mainViewModel));
                });
            });
        }
        else
        {
            // Redirigir a inicio de sesión si no hay sesión o venció el 2FA
            MainPage = new NavigationPage(new LoginPage(databaseService, emailService, mainViewModel));
        }
    }
}
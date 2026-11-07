using Examen2.Data;
using Examen2.ViewModels;

namespace Examen2;

public partial class SettingsPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly MainViewModel _mainViewModel;
    private bool _isInitializing;

    public SettingsPage(DatabaseService databaseService = null, MainViewModel mainViewModel = null)
    {
        InitializeComponent();
        _databaseService = databaseService ?? Application.Current?.Handler?.MauiContext?.Services.GetService<DatabaseService>();
        _mainViewModel = mainViewModel ?? Application.Current?.Handler?.MauiContext?.Services.GetService<MainViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadUserSettingsAsync();
    }

    private async Task LoadUserSettingsAsync()
    {
        if (_mainViewModel?.CurrentUser == null) return;

        _isInitializing = true;

        // Cargar el usuario fresco de la BD para obtener el estado actual de 2FA
        var freshUser = await _databaseService.GetUserByIdAsync(_mainViewModel.CurrentUser.Id);
        if (freshUser != null)
        {
            _mainViewModel.CurrentUser.Is2FAEnabled = freshUser.Is2FAEnabled;
            Switch2FA.IsToggled = freshUser.Is2FAEnabled;
        }

        _isInitializing = false;
    }

    private async void On2FAToggled(object sender, ToggledEventArgs e)
    {
        // Ignorar el evento mientras se carga el valor inicial del Switch
        if (_isInitializing || _mainViewModel?.CurrentUser == null) return;

        bool newState = e.Value;

        // Si intenta desactivar 2FA, pedir confirmación previa
        if (!newState)
        {
            bool confirm = await DisplayAlert(
                "Desactivar 2FA",
                "¿Estás seguro de que deseas desactivar la verificación en dos pasos? Tu cuenta será menos segura.",
                "Sí, desactivar",
                "Cancelar");

            if (!confirm)
            {
                // Revertir el Switch sin volver a disparar la lógica
                _isInitializing = true;
                Switch2FA.IsToggled = true;
                _isInitializing = false;
                return;
            }
        }

        // Actualizar en la base de datos de Supabase
        bool success = await _databaseService.Update2FAStatusAsync(_mainViewModel.CurrentUser.Id, newState);

        if (success)
        {
            _mainViewModel.CurrentUser.Is2FAEnabled = newState;
            string statusMsg = newState ? "Autenticación de dos factores activada." : "Autenticación de dos factores desactivada.";
            await DisplayAlert("Seguridad actualizada", statusMsg, "OK");
        }
        else
        {
            await DisplayAlert("Error", "No se pudo actualizar la configuración de 2FA.", "OK");

            // Revertir el Switch si falló la actualización
            _isInitializing = true;
            Switch2FA.IsToggled = !newState;
            _isInitializing = false;
        }
    }
}
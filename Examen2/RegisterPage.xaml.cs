using Examen2.Data;
using Examen2.ViewModels;

namespace Examen2;

public partial class RegisterPage : ContentPage
{
    private readonly DatabaseService _databaseService;

    public RegisterPage(DatabaseService databaseService)
    {
        InitializeComponent();
        _databaseService = databaseService;
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        string username = TxtUsername.Text?.Trim();
        string email = TxtEmail.Text?.Trim();
        string password = TxtPassword.Text;
        string confirmPassword = TxtConfirmPassword.Text;

        // Validaciones básicas
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) ||
            string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
        {
            await DisplayAlert("Campos incompletos", "Por favor completa todos los campos.", "OK");
            return;
        }

        if (!email.Contains("@") || !email.Contains("."))
        {
            await DisplayAlert("Correo inválido", "Ingresa un correo electrónico válido.", "OK");
            return;
        }

        if (password != confirmPassword)
        {
            await DisplayAlert("Error de contraseña", "Las contraseñas no coinciden.", "OK");
            return;
        }

        // Crear objeto de usuario
        var newUser = new User
        {
            Username = username,
            Email = email,
            Password = password
        };

        // Guardar en SQLite
        bool success = await _databaseService.RegisterUserAsync(newUser);

        if (success)
        {
            await DisplayAlert("Registro exitoso", "Tu cuenta ha sido creada. Ya puedes iniciar sesión.", "OK");
            await Navigation.PopAsync(); // Regresar a LoginPage
        }
        else
        {
            await DisplayAlert("Error", "El nombre de usuario o correo ya se encuentra registrado.", "OK");
        }
    }

    private async void OnBackToLoginClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
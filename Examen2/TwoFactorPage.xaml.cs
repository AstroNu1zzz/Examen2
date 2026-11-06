using Examen2.Data;
using Examen2.ViewModels;
using User = Examen2.Data.User;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace Examen2;

public partial class TwoFactorPage : ContentPage
{
    private readonly User _user;
    private readonly string _expectedCode;
    private readonly DatabaseService _databaseService;
    private readonly MainViewModel _mainViewModel;

    public TwoFactorPage(User user, string verificationCode, DatabaseService databaseService, MainViewModel mainViewModel)
    {
        InitializeComponent();
        _user = user;
        _expectedCode = verificationCode;
        _databaseService = databaseService;
        _mainViewModel = mainViewModel;
    }

    private async void OnVerifyClicked(object sender, EventArgs e)
    {
        if (TxtCode.Text?.Trim() == _expectedCode)
        {
            Preferences.Default.Set("IsLoggedIn", true);
            Preferences.Default.Set("CurrentUserId", _user.Id);
            Preferences.Default.Set("Last2FAVerifiedAt", DateTime.UtcNow);

            _mainViewModel.CurrentUser = _user;
            await Navigation.PushAsync(new MainPage(_databaseService, _mainViewModel));
        }
        else
        {
            await DisplayAlert("Error", "Código de verificación incorrecto.", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
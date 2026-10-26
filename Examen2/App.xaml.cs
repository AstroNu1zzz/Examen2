namespace Examen2;

public partial class App : Application
{
    public App(LoginPage loginPage)
    {
        InitializeComponent();

        // Carga síncrona de la página inicial
        MainPage = new NavigationPage(loginPage);
    }
}
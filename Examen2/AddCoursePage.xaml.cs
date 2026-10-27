using Examen2.Data;
using Examen2.ViewModels;

namespace Examen2;

public partial class AddCoursePage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly MainViewModel _mainViewModel;

    public AddCoursePage(DatabaseService databaseService, MainViewModel mainViewModel)
    {
        InitializeComponent();
        _databaseService = databaseService;
        _mainViewModel = mainViewModel;

        PkrThemeColor.SelectedIndex = 0;
    }

    private async void OnCreateCourseClicked(object sender, EventArgs e)
    {
        string courseName = TxtCourseName.Text?.Trim();

        if (string.IsNullOrEmpty(courseName))
        {
            await DisplayAlert("Campo requerido", "Por favor ingresa el nombre de la clase.", "OK");
            return;
        }

        if (_mainViewModel.CurrentUser == null)
        {
            await DisplayAlert("Error", "No hay una sesión activa de usuario.", "OK");
            return;
        }

        string selectedColor = PkrThemeColor.SelectedIndex switch
        {
            1 => "#1967D2",
            2 => "#455A64",
            3 => "#673AB7",
            4 => "#D93025",
            _ => "#1E8E3E"
        };

        var newCourse = new Course
        {
            CourseName = courseName,
            ThemeColor = selectedColor
        };

        bool success = await _databaseService.AddCourseAsync(newCourse, _mainViewModel.CurrentUser);

        if (success)
        {
            await _mainViewModel.RefreshCoursesAsync();
            await Navigation.PopAsync();
        }
        else
        {
            await DisplayAlert("Error", "No se pudo crear la clase.", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
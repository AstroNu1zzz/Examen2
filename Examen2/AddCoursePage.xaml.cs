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

        PkrThemeColor.SelectedIndex = 0; // Selecciona verde por defecto
    }

    private async void OnCreateCourseClicked(object sender, EventArgs e)
    {
        string courseName = TxtCourseName.Text?.Trim();
        string teacherName = TxtTeacherName.Text?.Trim();

        if (string.IsNullOrEmpty(courseName))
        {
            await DisplayAlert("Campo requerido", "Por favor ingresa el nombre de la clase.", "OK");
            return;
        }

        // Mapeo de colores según la opción seleccionada
        string selectedColor = PkrThemeColor.SelectedIndex switch
        {
            1 => "#1967D2", // Azul
            2 => "#455A64", // Gris Oscuro
            3 => "#673AB7", // Morado
            4 => "#D93025", // Rojo
            _ => "#1E8E3E"  // Verde
        };

        var newCourse = new Course
        {
            CourseName = courseName,
            TeacherName = string.IsNullOrEmpty(teacherName) ? "Sin profesor asignado" : teacherName,
            ThemeColor = selectedColor
        };

        bool success = await _databaseService.AddCourseAsync(newCourse);

        if (success)
        {
            await _mainViewModel.RefreshCoursesAsync(); // Actualiza la lista en el MainPage
            await Navigation.PopAsync();
        }
        else
        {
            await DisplayAlert("Error", "No se pudo crear la clase en la base de datos.", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
using Examen2.Data;
using Examen2.ViewModels;

namespace Examen2;

public partial class AddCoursePage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly MainViewModel _mainViewModel;

    // Ruta por defecto si el usuario no elige una imagen
    private string _selectedImagePath = "default_course_bg.jpg";

    public AddCoursePage(DatabaseService databaseService, MainViewModel mainViewModel)
    {
        InitializeComponent();
        _databaseService = databaseService;
        _mainViewModel = mainViewModel;
    }

    private async void OnPickImageClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await MediaPicker.Default.PickPhotoAsync();

            if (result != null)
            {
                // Copiar la imagen a la carpeta de datos de la app para que sea persistente
                string localPath = Path.Combine(FileSystem.AppDataDirectory, result.FileName);

                using (var stream = await result.OpenReadAsync())
                using (var newStream = File.OpenWrite(localPath))
                {
                    await stream.CopyToAsync(newStream);
                }

                _selectedImagePath = localPath;

                // Mostrar la vista previa
                ImgBannerPreview.Source = ImageSource.FromFile(localPath);
                ImgBannerPreview.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo cargar la imagen: {ex.Message}", "OK");
        }
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

        var newCourse = new Course
        {
            CourseName = courseName,
            BannerImagePath = _selectedImagePath // Guardamos la ruta de la imagen
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
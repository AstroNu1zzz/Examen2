using Examen2.Data;
using Examen2.ViewModels;

namespace Examen2;

public partial class CourseDetailPage : ContentPage
{
    private readonly Course _course;
    private DatabaseService _databaseService;
    private MainViewModel _mainViewModel;

    public CourseDetailPage(Course course)
    {
        InitializeComponent();
        _course = course;
        BindingContext = _course;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Inicialización segura de servicios
        var services = Application.Current?.Handler?.MauiContext?.Services;
        _databaseService ??= services?.GetService<DatabaseService>();
        _mainViewModel ??= services?.GetService<MainViewModel>();

        await LoadContentAsync();
        await LoadTeachersAsync();
    }

    private async Task LoadContentAsync()
    {
        if (_databaseService != null)
        {
            var items = await _databaseService.GetCourseContentItemsAsync(_course.Id);

            // Tablón muestra todo (Anuncios, Materiales y Tareas)
            LstTablonItems.ItemsSource = items;

            // Trabajos muestra solo Tareas y Materiales
            LstTrabajoItems.ItemsSource = items.Where(i => i.ItemType == "Tarea" || i.ItemType == "Material").ToList();
        }
    }

    private async Task LoadTeachersAsync()
    {
        if (_databaseService != null)
        {
            var teachers = await _databaseService.GetCourseTeachersAsync(_course.Id);
            LstTeachers.ItemsSource = teachers;
        }
    }

    // --- CAMBIO DE PESTAÑAS (NAVEGACIÓN INFERIOR) ---
    private void OnTablonTabClicked(object sender, EventArgs e)
    {
        ViewTablon.IsVisible = true;
        ViewTrabajo.IsVisible = false;
        ViewPersonas.IsVisible = false;

        BtnTabTablon.TextColor = Color.FromArgb("#1A73E8");
        BtnTabTrabajo.TextColor = Color.FromArgb("#5F6368");
        BtnTabPersonas.TextColor = Color.FromArgb("#5F6368");
    }

    private void OnTrabajoTabClicked(object sender, EventArgs e)
    {
        ViewTablon.IsVisible = false;
        ViewTrabajo.IsVisible = true;
        ViewPersonas.IsVisible = false;

        BtnTabTablon.TextColor = Color.FromArgb("#5F6368");
        BtnTabTrabajo.TextColor = Color.FromArgb("#1A73E8");
        BtnTabPersonas.TextColor = Color.FromArgb("#5F6368");
    }

    private void OnPersonasTabClicked(object sender, EventArgs e)
    {
        ViewTablon.IsVisible = false;
        ViewTrabajo.IsVisible = false;
        ViewPersonas.IsVisible = true;

        BtnTabTablon.TextColor = Color.FromArgb("#5F6368");
        BtnTabTrabajo.TextColor = Color.FromArgb("#5F6368");
        BtnTabPersonas.TextColor = Color.FromArgb("#1A73E8");
    }

    // --- MENÚ PARA CREAR CONTENIDO (ANUNCIO / MATERIAL / TAREA) ---
    private async void OnCreateContentClicked(object sender, EventArgs e)
    {
        if (_databaseService == null || _mainViewModel == null)
            return;

        string action = await DisplayActionSheet("Crear en la clase", "Cancelar", null, "Anuncio", "Material", "Tarea");

        if (action == "Anuncio" || action == "Material" || action == "Tarea")
        {
            await Navigation.PushAsync(new CreateContentPage(_course.Id, action, _databaseService, _mainViewModel));
        }
    }

    private async void OnAddTeacherClicked(object sender, EventArgs e)
    {
        if (_databaseService == null)
            return;

        string input = await DisplayPromptAsync("Añadir Profesor",
            "Ingresa el correo o usuario del profesor:",
            "Añadir", "Cancelar");

        if (string.IsNullOrWhiteSpace(input)) return;

        var result = await _databaseService.AddTeacherToCourseAsync(_course.Id, input.Trim());
        await DisplayAlert("Resultado", result.Message, "OK");

        if (result.Success)
        {
            await LoadTeachersAsync();
        }
    }
}
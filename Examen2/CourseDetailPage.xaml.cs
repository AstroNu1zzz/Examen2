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

            // 1. Tablón (Todo)
            LstTablonItems.ItemsSource = items;

            // 2. Trabajos (Agrupados por Unidad)
            var trabajos = items.Where(i => i.ItemType == "Tarea" || i.ItemType == "Material").ToList();
            var units = await _databaseService.GetCourseUnitsAsync(_course.Id);

            var groupedList = new List<UnitGroup>();

            foreach (var unit in units)
            {
                var unitItems = trabajos.Where(t => t.UnitId == unit.Id).ToList();
                if (unitItems.Any())
                {
                    groupedList.Add(new UnitGroup(unit.Name, unitItems));
                }
            }

            // Agregar los que no tienen unidad asignada
            var unassigned = trabajos.Where(t => t.UnitId == null).ToList();
            if (unassigned.Any())
            {
                groupedList.Add(new UnitGroup("Otras actividades", unassigned));
            }

            LstTrabajoItems.ItemsSource = groupedList;
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

    private async void OnContentItemTapped(object sender, EventArgs e)
    {
        var border = sender as Border;
        var item = border?.BindingContext as ClassContentItem;

        if (item == null) return;

        // Los anuncios ya se muestran completos; Materiales y Tareas abren la pantalla de detalle
        if (item.ItemType == "Material" || item.ItemType == "Tarea")
        {
            await Navigation.PushAsync(new ContentDetailPage(item, _databaseService, _mainViewModel));
        }
    }

    private async void OnOpenAttachmentDirectly(object sender, EventArgs e)
    {
        var btn = sender as Button;
        var item = btn?.BindingContext as ClassContentItem;

        if (item != null && !string.IsNullOrEmpty(item.AttachedFilePath) && File.Exists(item.AttachedFilePath))
        {
            await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(item.AttachedFilePath)
            });
        }
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
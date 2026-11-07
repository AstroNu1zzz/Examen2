using System.Collections.ObjectModel;
using Examen2.Data;
using Examen2.ViewModels;

namespace Examen2;

public partial class CourseDetailPage : ContentPage
{
    public Course Course { get; set; }
    public ObservableCollection<FeedPostViewModel> FeedPosts { get; set; } = new();
    public ObservableCollection<FeedPostViewModel> AssignmentPosts { get; set; } = new();

    private DatabaseService _databaseService;
    private MainViewModel _mainViewModel;
    private bool _isTeacher;

    public CourseDetailPage(Course course, DatabaseService databaseService = null, MainViewModel mainViewModel = null)
    {
        InitializeComponent();
        Course = course;
        _databaseService = databaseService;
        _mainViewModel = mainViewModel;
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        EnsureServices();
        await LoadCourseContentAsync();
    }

    private void EnsureServices()
    {
        _databaseService ??= Handler?.MauiContext?.Services.GetService<DatabaseService>()
                           ?? Application.Current?.Handler?.MauiContext?.Services.GetService<DatabaseService>();

        _mainViewModel ??= Handler?.MauiContext?.Services.GetService<MainViewModel>()
                         ?? Application.Current?.Handler?.MauiContext?.Services.GetService<MainViewModel>();
    }

    private async Task LoadCourseContentAsync()
    {
        EnsureServices();

        if (_databaseService == null || Course == null) return;

        // 1. Determinar el rol del usuario en la clase
        if (_mainViewModel?.CurrentUser != null)
        {
            string role = await _databaseService.GetUserRoleInCourseAsync(Course.Id, _mainViewModel.CurrentUser.Id);
            _isTeacher = (role == "Teacher");
        }
        else
        {
            _isTeacher = false;
        }

        // 2. Ajustar visibilidad de elementos según el rol (Profesor vs Alumno)
        if (FindByName("FabAdd") is VisualElement fabAdd)
        {
            fabAdd.IsVisible = _isTeacher;
        }

        if (FindByName("BtnAddTeacher") is VisualElement btnAddTeacher)
        {
            btnAddTeacher.IsVisible = _isTeacher;
        }

        if (FindByName("BorderCreatePost") is VisualElement borderCreatePost)
        {
            borderCreatePost.IsVisible = _isTeacher;
        }

        // 3. Cargar publicaciones y tareas del Tablón
        var items = await _databaseService.GetCourseContentItemsAsync(Course.Id);

        FeedPosts.Clear();
        AssignmentPosts.Clear();

        foreach (var item in items)
        {
            var vm = new FeedPostViewModel(item);
            FeedPosts.Add(vm);

            if (item.ItemType == "Tarea")
            {
                AssignmentPosts.Add(vm);
            }
        }

        // 4. Cargar la lista de profesores y alumnos para la pestaña "Personas"
        var teachers = await _databaseService.GetCourseTeachersAsync(Course.Id);
        if (FindByName("CvTeachers") is CollectionView cvTeachers)
        {
            cvTeachers.ItemsSource = teachers;
        }

        var students = await _databaseService.GetCourseStudentsAsync(Course.Id);
        if (FindByName("CvStudents") is CollectionView cvStudents)
        {
            cvStudents.ItemsSource = students;
        }
    }

    // NAVEGACIÓN DE PESTAÑAS
    private void OnTabNovedadesTapped(object sender, EventArgs e)
    {
        SetTab(ViewNovedades, TabNovedadesLabel, TabNovedadesIndicator);
    }

    private void OnTabTrabajosTapped(object sender, EventArgs e)
    {
        SetTab(ViewTrabajos, TabTrabajosLabel, TabTrabajosIndicator);
    }

    private void OnTabPersonasTapped(object sender, EventArgs e)
    {
        SetTab(ViewPersonas, TabPersonasLabel, TabPersonasIndicator);
    }

    private void SetTab(ScrollView activeView, Label activeLabel, BoxView activeIndicator)
    {
        ViewNovedades.IsVisible = (activeView == ViewNovedades);
        ViewTrabajos.IsVisible = (activeView == ViewTrabajos);
        ViewPersonas.IsVisible = (activeView == ViewPersonas);

        Color inactiveColor = Color.FromArgb("#64748B");
        Color activeColor = Color.FromArgb("#5EEAD4");

        TabNovedadesLabel.TextColor = inactiveColor;
        TabNovedadesIndicator.Color = Colors.Transparent;
        TabTrabajosLabel.TextColor = inactiveColor;
        TabTrabajosIndicator.Color = Colors.Transparent;
        TabPersonasLabel.TextColor = inactiveColor;
        TabPersonasIndicator.Color = Colors.Transparent;

        activeLabel.TextColor = activeColor;
        activeIndicator.Color = activeColor;
    }

    // ACCIONES DE BOTONES
    private async void OnBackButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnAddTeacherClicked(object sender, EventArgs e)
    {
        EnsureServices();

        if (!_isTeacher)
        {
            await DisplayAlert("Acceso Restringido", "Solo los profesores pueden añadir a otros profesores.", "OK");
            return;
        }

        string input = await DisplayPromptAsync(
            "Añadir Profesor",
            "Ingresa el nombre de usuario o correo del profesor:",
            accept: "Añadir",
            cancel: "Cancelar",
            placeholder: "ejemplo@correo.com");

        if (string.IsNullOrWhiteSpace(input)) return;

        var (success, message) = await _databaseService.AddTeacherToCourseAsync(Course.Id, input.Trim());

        await DisplayAlert(success ? "Éxito" : "Atención", message, "OK");

        if (success)
        {
            // Actualizar la lista en pantalla
            var teachers = await _databaseService.GetCourseTeachersAsync(Course.Id);
            if (FindByName("CvTeachers") is CollectionView cvTeachers)
            {
                cvTeachers.ItemsSource = teachers;
            }
        }
    }

    private async void OnVideoCallClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Videollamada", "Iniciando sesión de videollamada para la clase...", "OK");
    }

    private async void OnCourseInfoClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Información del Curso", $"{Course.CourseName}\nCódigo de clase: {Course.CourseCode}\nProfesor: {Course.TeacherName}", "OK");
    }

    private async void OnCreatePostTapped(object sender, EventArgs e)
    {
        await ShowCreateOptionsAsync();
    }

    private async void OnCreatePostOptionsClicked(object sender, EventArgs e)
    {
        await ShowCreateOptionsAsync();
    }

    private async Task ShowCreateOptionsAsync()
    {
        EnsureServices();

        if (!_isTeacher)
        {
            await DisplayAlert("Acceso Restringido", "Solo el profesor de la clase puede publicar tareas o anuncios.", "OK");
            return;
        }

        if (_databaseService == null || _mainViewModel == null || Course == null)
        {
            await DisplayAlert("Error", "No se pudo acceder al servicio de datos.", "OK");
            return;
        }

        string action = await DisplayActionSheet(
            "¿Qué deseas publicar?",
            "Cancelar",
            null,
            "📋  Crear Tarea",
            "📖  Crear Material",
            "📢  Crear Anuncio");

        string selectedType = action switch
        {
            "📋  Crear Tarea" => "Tarea",
            "📖  Crear Material" => "Material",
            "📢  Crear Anuncio" => "Anuncio",
            _ => null
        };

        if (selectedType != null)
        {
            await Navigation.PushAsync(new CreateContentPage(Course.Id, selectedType, _databaseService, _mainViewModel));
        }
    }

    private async void OnFeedPostTapped(object sender, EventArgs e)
    {
        EnsureServices();
        var border = sender as Border;
        var vm = border?.BindingContext as FeedPostViewModel;

        // Si es un Anuncio, se ignora el toque y no navega al detalle
        if (vm == null || vm.SourceItem.ItemType == "Anuncio") return;

        await Navigation.PushAsync(new ContentDetailPage(vm.SourceItem, _databaseService, _mainViewModel));
    }

    private void OnPostOptionsClicked(object sender, EventArgs e)
    {
        // Opciones secundarias del post
    }
}

// MODELO DE VISTA PARA FEED DE PUBLICACIONES (CON GRADIENTES DINÁMICOS)
public class FeedPostViewModel
{
    public ClassContentItem SourceItem { get; }

    public string Title => $"{SourceItem.ItemType}: {SourceItem.Title}";
    public string Description => SourceItem.Description;

    // Se evalúa verdadero ÚNICAMENTE si es un Anuncio y tiene contenido
    public bool HasDescription => SourceItem.ItemType == "Anuncio" && !string.IsNullOrWhiteSpace(SourceItem.Description);

    public string DateFormatted => SourceItem.CreatedAt.ToString("dd 'de' MMM.");
    public bool IsAssignment => SourceItem.ItemType == "Tarea";
    public string DueDateText => SourceItem.DueDate.HasValue ? $"Fecha de entrega: {SourceItem.DueDate.Value:dd/MM HH:mm}" : "";
    public string CommentsCountText => "Agregar un comentario de clase";

    public string IconSymbol => SourceItem.ItemType switch
    {
        "Tarea" => "📋",
        "Material" => "📖",
        _ => "📢"
    };

    public Color IconBgColor => SourceItem.ItemType switch
    {
        "Tarea" => Color.FromArgb("#D93025"),
        "Material" => Color.FromArgb("#1A73E8"),
        _ => Color.FromArgb("#1E8E3E")
    };

    public Brush CardGradientBrush
    {
        get
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1)
            };

            if (SourceItem.ItemType == "Tarea") // ROJO
            {
                brush.GradientStops.Add(new GradientStop(Color.FromArgb("#FADBD8"), 0.0f));
                brush.GradientStops.Add(new GradientStop(Color.FromArgb("#FFFFFF"), 0.8f));
            }
            else if (SourceItem.ItemType == "Material") // AZUL
            {
                brush.GradientStops.Add(new GradientStop(Color.FromArgb("#D6EAF8"), 0.0f));
                brush.GradientStops.Add(new GradientStop(Color.FromArgb("#FFFFFF"), 0.8f));
            }
            else // ANUNCIO / AVISO - VERDE
            {
                brush.GradientStops.Add(new GradientStop(Color.FromArgb("#D1F2EB"), 0.0f));
                brush.GradientStops.Add(new GradientStop(Color.FromArgb("#FFFFFF"), 0.8f));
            }

            return brush;
        }
    }

    public FeedPostViewModel(ClassContentItem item)
    {
        SourceItem = item;
    }
}
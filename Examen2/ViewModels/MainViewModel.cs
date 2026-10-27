using Examen2.Data;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using SQLite;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Examen2.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;

        // --- GESTIÓN DEL PERFIL DE USUARIO ---
        private User _currentUser;
        public User CurrentUser
        {
            get => _currentUser;
            set
            {
                _currentUser = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UserInitial));
            }
        }

        public string UserInitial => !string.IsNullOrEmpty(CurrentUser?.Username)
            ? CurrentUser.Username.Substring(0, 1).ToUpper()
            : (!string.IsNullOrEmpty(CurrentUser?.Email) ? CurrentUser.Email.Substring(0, 1).ToUpper() : "U");

        private bool _hasProfilePicture;
        public bool HasProfilePicture
        {
            get => _hasProfilePicture;
            set { _hasProfilePicture = value; OnPropertyChanged(); }
        }

        private ImageSource _profileImageSource;
        public ImageSource ProfileImageSource
        {
            get => _profileImageSource;
            set { _profileImageSource = value; OnPropertyChanged(); }
        }

        // --- CONTROL DE MENÚS (PERFIL Y MENÚ LATERAL) ---
        private bool _isProfileMenuVisible;
        public bool IsProfileMenuVisible
        {
            get => _isProfileMenuVisible;
            set { _isProfileMenuVisible = value; OnPropertyChanged(); }
        }

        private bool _isDrawerVisible;
        public bool IsDrawerVisible
        {
            get => _isDrawerVisible;
            set { _isDrawerVisible = value; OnPropertyChanged(); }
        }

        // --- PROPIEDADES DE LA INTERFAZ ---
        public ObservableCollection<Course> EnrolledCourses { get; set; }
        public string NextDeadlineMessage { get; set; }

        // --- COMANDOS ---
        public ICommand NavigateHomeCommand { get; }
        public ICommand NavigateCalendarCommand { get; }
        public ICommand NavigatePendingTasksCommand { get; }
        public ICommand NavigateArchivedCommand { get; }
        public ICommand NavigateSettingsCommand { get; }
        public ICommand OpenCourseCommand { get; }
        public ICommand OpenCourseFilesCommand { get; }
        public ICommand ViewPendingTaskCommand { get; }
        public ICommand ChangeProfilePictureCommand { get; }
        public ICommand ToggleProfileMenuCommand { get; }
        public ICommand ToggleDrawerCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand AddCourseCommand { get; }
        public ICommand DeleteCourseCommand { get; }

        public MainViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            EnrolledCourses = new ObservableCollection<Course>();

            NextDeadlineMessage = "No tienes ningún trabajo que entregar en los próximos 7 días";

            NavigateHomeCommand = new Command(OnNavigateHome);
            NavigateCalendarCommand = new Command(OnNavigateCalendar);
            NavigatePendingTasksCommand = new Command(OnNavigatePendingTasks);
            NavigateArchivedCommand = new Command(OnNavigateArchived);
            NavigateSettingsCommand = new Command(OnNavigateSettings);

            OpenCourseCommand = new Command<Course>(OnOpenCourse);
            OpenCourseFilesCommand = new Command<Course>(OnOpenCourseFiles);
            ViewPendingTaskCommand = new Command(OnNavigatePendingTasks);

            ChangeProfilePictureCommand = new Command(OnChangeProfilePicture);
            ToggleProfileMenuCommand = new Command(() => IsProfileMenuVisible = !IsProfileMenuVisible);
            ToggleDrawerCommand = new Command(() => IsDrawerVisible = !IsDrawerVisible);
            LogoutCommand = new Command(OnLogout);

            AddCourseCommand = new Command(OnAddCourse);
            DeleteCourseCommand = new Command<Course>(OnDeleteCourse);

            _ = LoadCoursesAsync();
        }

        public void SetCurrentUser(User user)
        {
            CurrentUser = user;
            if (user != null)
            {
                Preferences.Default.Set("LoggedInUserId", user.Id);
                _ = LoadCoursesAsync(); // Recarga las clases del nuevo usuario
            }
            LoadProfilePicture();
        }

        private void LoadProfilePicture()
        {
            if (CurrentUser != null && !string.IsNullOrEmpty(CurrentUser.ProfilePicturePath) && File.Exists(CurrentUser.ProfilePicturePath))
            {
                ProfileImageSource = ImageSource.FromFile(CurrentUser.ProfilePicturePath);
                HasProfilePicture = true;
            }
            else
            {
                ProfileImageSource = null;
                HasProfilePicture = false;
            }
        }

        private async void OnChangeProfilePicture()
        {
            IsProfileMenuVisible = false;
            try
            {
                var result = await MediaPicker.Default.PickPhotoAsync();

                if (result != null)
                {
                    if (CurrentUser == null)
                        CurrentUser = new User { Id = 1, Username = "Usuario" };

                    string localPath = Path.Combine(FileSystem.AppDataDirectory, $"profile_{CurrentUser.Id}.jpg");

                    using (var stream = await result.OpenReadAsync())
                    using (var newStream = File.OpenWrite(localPath))
                    {
                        await stream.CopyToAsync(newStream);
                    }

                    CurrentUser.ProfilePicturePath = localPath;
                    await _databaseService.UpdateUserAsync(CurrentUser);

                    ProfileImageSource = ImageSource.FromFile(localPath);
                    HasProfilePicture = true;
                }
            }
            catch (Exception ex)
            {
                if (Application.Current?.MainPage != null)
                    await Application.Current.MainPage.DisplayAlert("Error", $"No se pudo cargar la imagen: {ex.Message}", "OK");
            }
        }

        private void OnLogout()
        {
            Preferences.Default.Remove("LoggedInUserId");
            IsProfileMenuVisible = false;
            IsDrawerVisible = false;
            CurrentUser = null;
            ProfileImageSource = null;
            HasProfilePicture = false;

            var loginPage = Application.Current?.Handler?.MauiContext?.Services.GetService<LoginPage>();
            if (Application.Current?.Windows.Count > 0 && loginPage != null)
            {
                Application.Current.Windows[0].Page = new NavigationPage(loginPage);
            }
        }

        public async Task RefreshCoursesAsync()
        {
            await LoadCoursesAsync();
        }

        private async void OnAddCourse()
        {
            IsDrawerVisible = false;
            var addCoursePage = Application.Current?.Handler?.MauiContext?.Services.GetService<AddCoursePage>();
            if (Application.Current?.MainPage != null && addCoursePage != null)
            {
                await Application.Current.MainPage.Navigation.PushAsync(addCoursePage);
            }
        }

        private async void OnDeleteCourse(Course course)
        {
            if (course == null) return;

            bool confirm = await Application.Current.MainPage.DisplayAlert(
                "Eliminar clase",
                $"¿Estás seguro de que deseas eliminar '{course.CourseName}'?",
                "Eliminar", "Cancelar");

            if (confirm)
            {
                await _databaseService.DeleteCourseAsync(course.Id);
                await LoadCoursesAsync();
            }
        }

        private async Task LoadCoursesAsync()
        {
            if (CurrentUser == null)
            {
                EnrolledCourses.Clear();
                return;
            }

            var courses = await _databaseService.GetCoursesAsync(CurrentUser.Id);
            EnrolledCourses.Clear();
            foreach (var course in courses)
            {
                EnrolledCourses.Add(course);
            }
        }

        private async void OnNavigateHome()
        {
            IsDrawerVisible = false;
            await LoadCoursesAsync();
        }

        private async void OnNavigateCalendar()
        {
            IsDrawerVisible = false;
            if (Application.Current?.MainPage != null)
                await Application.Current.MainPage.Navigation.PushAsync(new CalendarPage());
        }

        private async void OnNavigatePendingTasks()
        {
            IsDrawerVisible = false;
            if (Application.Current?.MainPage != null)
                await Application.Current.MainPage.Navigation.PushAsync(new PendingTaskPage());
        }

        private async void OnNavigateArchived()
        {
            IsDrawerVisible = false;
            if (Application.Current?.MainPage != null)
                await Application.Current.MainPage.DisplayAlert("Clases archivadas", "No tienes clases archivadas en este ciclo escolar.", "OK");
        }

        private async void OnNavigateSettings()
        {
            IsDrawerVisible = false;
            if (Application.Current?.MainPage != null)
                await Application.Current.MainPage.Navigation.PushAsync(new SettingsPage());
        }

        private async void OnOpenCourse(Course selectedCourse)
        {
            IsDrawerVisible = false;
            if (selectedCourse == null) return;
            if (Application.Current?.MainPage != null)
                await Application.Current.MainPage.Navigation.PushAsync(new CourseDetailPage(selectedCourse));
        }

        private async void OnOpenCourseFiles(Course selectedCourse)
        {
            IsDrawerVisible = false;
            if (selectedCourse == null) return;
            if (Application.Current?.MainPage != null)
                await Application.Current.MainPage.DisplayAlert("Archivos de clase", $"Abriendo la carpeta de Drive/archivos locales para {selectedCourse.CourseName}", "OK");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Course
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string CourseName { get; set; }
        public string ThemeColor { get; set; }

        // ID y Nombre del creador/profesor principal
        public int PrimaryTeacherId { get; set; }
        public string PrimaryTeacherName { get; set; }
    }

    public class CourseTeacher
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int CourseId { get; set; }
        public int UserId { get; set; }
        public bool IsPrimary { get; set; }
    }

    public class User
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Unique]
        public string Username { get; set; }

        public string Email { get; set; }
        public string Password { get; set; }
        public string ProfilePicturePath { get; set; }
    }

    public class ClassContentItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int CourseId { get; set; }

        // Tipo: "Anuncio", "Material", o "Tarea"
        public string ItemType { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string AuthorName { get; set; }
    }
}
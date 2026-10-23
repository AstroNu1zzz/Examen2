using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Examen2.ViewModels
{
    public class MainViewModel
    {
        public ObservableCollection<Course> EnrolledCourses { get; set; }
        public string NextDeadlineMessage { get; set; }

        // Comandos de navegación principal
        public ICommand NavigateHomeCommand { get; }
        public ICommand NavigateCalendarCommand { get; }
        public ICommand NavigatePendingTasksCommand { get; }
        public ICommand NavigateSettingsCommand { get; }

        // Comandos de interacción con los elementos
        public ICommand OpenCourseCommand { get; }
        public ICommand OpenCourseFilesCommand { get; }

        public MainViewModel()
        {
            EnrolledCourses = new ObservableCollection<Course>();
            // Aquí haces tu consulta con Entity Framework Core a la base de datos
            // LoadCoursesFromDatabase();

            NextDeadlineMessage = "No tienes ningún trabajo que entregar en los próximos 7 días";

            OpenCourseCommand = new Command<Course>(OpenCourse);
            OpenCourseFilesCommand = new Command<Course>(OpenCourseFiles);
        }

        private void OpenCourse(Course selectedCourse)
        {
            if (selectedCourse == null) return;
            // Lógica para navegar a la página de la clase (Ej: Tablón) pasando el ID del curso
        }

        private void OpenCourseFiles(Course selectedCourse)
        {
            if (selectedCourse == null) return;
            // Lógica para abrir la vista del explorador de archivos del curso
        }
    }

    public class Course
    {
        public int Id { get; set; }
        public string CourseName { get; set; }
        public string TeacherName { get; set; }
        public string ThemeColor { get; set; } // Ej: "#1E8E3E"
    }
}
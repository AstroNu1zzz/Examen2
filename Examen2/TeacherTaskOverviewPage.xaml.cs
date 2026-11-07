using Examen2.Data;
using Examen2.ViewModels;

namespace Examen2;

public partial class TeacherTaskOverviewPage : ContentPage
{
    private readonly ClassContentItem _taskItem;
    private readonly int _courseId;
    private readonly DatabaseService _databaseService;
    private readonly MainViewModel _mainViewModel;

    public TeacherTaskOverviewPage(ClassContentItem taskItem, int courseId, DatabaseService databaseService, MainViewModel mainViewModel)
    {
        InitializeComponent();
        _taskItem = taskItem;
        _courseId = courseId;
        _databaseService = databaseService;
        _mainViewModel = mainViewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadOverviewDataAsync();
    }

    private async Task LoadOverviewDataAsync()
    {
        LblTitle.Text = _taskItem.Title;

        bool isExpired = _taskItem.DueDate.HasValue && DateTime.Now > _taskItem.DueDate.Value;
        LblDueDate.Text = _taskItem.DueDate.HasValue
            ? $"Fecha límite: {_taskItem.DueDate.Value:dd/MM/yyyy HH:mm} {(isExpired ? "(Vencida)" : "")}"
            : "Sin fecha límite";

        LblStatusBadge.Text = isExpired ? "⚠️ Tarea vencida" : "🟢 En curso";

        // 1. Cargar todos los alumnos de la clase
        var students = await _databaseService.GetCourseStudentsAsync(_courseId);

        // 2. Cargar todas las entregas realizadas para esta tarea
        var submissions = await _databaseService.GetTaskSubmissionsAsync(_taskItem.Id);

        // 3. Emparejar cada alumno con su entrega
        var list = new List<StudentSubmissionItem>();
        int submittedCount = 0;
        int missingCount = 0;

        foreach (var student in students)
        {
            var sub = submissions.FirstOrDefault(s => s.UserId == student.Id);
            bool hasSubmitted = sub != null;

            if (hasSubmitted) submittedCount++;
            else missingCount++;

            list.Add(new StudentSubmissionItem
            {
                Student = student,
                Submission = sub,
                IsExpired = isExpired
            });
        }

        LblCountSubmitted.Text = submittedCount.ToString();
        LblCountMissing.Text = missingCount.ToString();
        CvStudentSubmissions.ItemsSource = list;
    }

    private async void OnStudentItemTapped(object sender, EventArgs e)
    {
        var border = sender as Border;
        var item = border?.BindingContext as StudentSubmissionItem;

        if (item == null) return;

        if (!item.HasSubmitted)
        {
            await DisplayAlert("Sin entrega", $"{item.Student.Username} aún no ha subido ningún archivo.", "OK");
            return;
        }

        // Navegar a la pantalla de calificación pasando la entrega y el alumno
        await Navigation.PushAsync(new GradeSubmissionPage(_taskItem, item.Student, item.Submission, _databaseService));
    }
}

public class StudentSubmissionItem
{
    public User Student { get; set; }
    public TaskSubmission Submission { get; set; }
    public bool IsExpired { get; set; }

    public bool HasSubmitted => Submission != null;
    public string StatusText
    {
        get
        {
            if (!HasSubmitted)
                return IsExpired ? "Vencida (Sin entregar)" : "Pendiente de entrega";

            return $"Entregado el {Submission.SubmittedAt:dd/MM HH:mm}";
        }
    }

    public Color StatusColor => HasSubmitted ? Color.FromArgb("#5EEAD4") : Color.FromArgb("#EF4444");
    public string GradeText => Submission?.Grade.HasValue == true ? $"{Submission.Grade}/100" : (HasSubmitted ? "Calificar ➔" : "-");
}
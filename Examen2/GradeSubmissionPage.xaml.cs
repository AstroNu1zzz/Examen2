using Examen2.Data;

namespace Examen2;

public partial class GradeSubmissionPage : ContentPage
{
    private readonly ClassContentItem _taskItem;
    private readonly User _student;
    private readonly TaskSubmission _submission;
    private readonly DatabaseService _databaseService;

    public GradeSubmissionPage(ClassContentItem taskItem, User student, TaskSubmission submission, DatabaseService databaseService)
    {
        InitializeComponent();
        _taskItem = taskItem;
        _student = student;
        _submission = submission;
        _databaseService = databaseService;

        LoadSubmissionData();
    }

    private void LoadSubmissionData()
    {
        LblStudentName.Text = _student.Username;
        LblTaskTitle.Text = $"Tarea: {_taskItem.Title}";
        LblSubmittedAt.Text = $"Entregado el: {_submission.SubmittedAt:dd/MM/yyyy HH:mm}";

        // Nombre limpio del archivo
        LblFileName.Text = Path.GetFileName(_submission.FilePath);

        // Si ya tenía calificación o retroalimentación previa
        if (_submission.Grade.HasValue)
        {
            TxtGrade.Text = _submission.Grade.Value.ToString();
        }

        if (!string.IsNullOrWhiteSpace(_submission.Feedback))
        {
            TxtFeedback.Text = _submission.Feedback;
        }
    }

    private async void OnOpenFileClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_submission.FilePath))
        {
            await DisplayAlert("Error", "No se encontró el enlace del archivo.", "OK");
            return;
        }

        try
        {
            // Abre la URL pública de Supabase directamente en el navegador del dispositivo
            await Browser.Default.OpenAsync(new Uri(_submission.FilePath), BrowserLaunchMode.SystemPreferred);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo abrir el archivo: {ex.Message}", "OK");
        }
    }

    private async void OnSaveGradeClicked(object sender, EventArgs e)
    {
        string gradeInput = TxtGrade.Text?.Trim();

        if (string.IsNullOrEmpty(gradeInput) || !double.TryParse(gradeInput, out double grade) || grade < 0 || grade > 100)
        {
            await DisplayAlert("Calificación inválida", "Ingresa una calificación válida entre 0 y 100.", "OK");
            return;
        }

        BtnSaveGrade.IsEnabled = false;

        bool success = await _databaseService.UpdateTaskGradeAsync(_submission.Id, grade, TxtFeedback.Text?.Trim());

        BtnSaveGrade.IsEnabled = true;

        if (success)
        {
            await DisplayAlert("Éxito", "Calificación y retroalimentación guardadas correctamente.", "OK");
            await Navigation.PopAsync();
        }
        else
        {
            await DisplayAlert("Error", "No se pudo guardar la calificación.", "OK");
        }
    }
}
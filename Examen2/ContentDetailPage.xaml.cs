using Examen2.Data;
using Examen2.ViewModels;

namespace Examen2;

public partial class ContentDetailPage : ContentPage
{
    private readonly ClassContentItem _item;
    private readonly DatabaseService _databaseService;
    private readonly MainViewModel _mainViewModel;
    private string _submissionFilePath;

    public ContentDetailPage(ClassContentItem item, DatabaseService databaseService, MainViewModel mainViewModel)
    {
        InitializeComponent();
        _item = item;
        _databaseService = databaseService;
        _mainViewModel = mainViewModel;

        LoadDetails();
    }

    private async void LoadDetails()
    {
        LblTopType.Text = _item.ItemType;
        LblTitle.Text = _item.Title;
        LblDescription.Text = string.IsNullOrEmpty(_item.Description) ? "Sin instrucciones." : _item.Description;
        LblPoints.Text = $"{_item.Points} puntos";

        // Si es Tarea, mostrar panel de entrega y fechas
        if (_item.ItemType == "Tarea")
        {
            if (_item.DueDate.HasValue)
            {
                LblDueDate.Text = $"Fecha de entrega: {_item.DueDate.Value:dd/MM/yyyy HH:mm}";
                LblDueDate.IsVisible = true;
            }
            BorderStudentSubmission.IsVisible = true;
            LblSubmissionStatusTop.IsVisible = true;
            await LoadStudentSubmissionAsync();
        }
        else
        {
            // Si es Material o Anuncio, ocultar panel de entrega
            BorderStudentSubmission.IsVisible = false;
            LblSubmissionStatusTop.IsVisible = false;
        }

        if (!string.IsNullOrEmpty(_item.AttachedFilePath) && File.Exists(_item.AttachedFilePath))
        {
            LayoutTeacherAttachment.IsVisible = true;
            LblTeacherFileName.Text = Path.GetFileName(_item.AttachedFilePath);
        }
    }

    private async Task LoadStudentSubmissionAsync()
    {
        if (_mainViewModel.CurrentUser == null) return;

        var sub = await _databaseService.GetTaskSubmissionAsync(_item.Id, _mainViewModel.CurrentUser.Id);
        if (sub != null && !string.IsNullOrEmpty(sub.FilePath))
        {
            _submissionFilePath = sub.FilePath;
            LblSubmittedFileName.Text = $"📄 {Path.GetFileName(sub.FilePath)}";
            LblSubmittedFileName.IsVisible = true;

            LblSubmissionStatus.Text = "Entregado";
            LblSubmissionStatus.TextColor = Color.FromArgb("#1E8E3E");
            BtnSubmit.Text = "Anular entrega";
            BtnSubmit.BackgroundColor = Color.FromArgb("#D93025");
        }
    }

    private async void OnOpenTeacherFileClicked(object sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(_item.AttachedFilePath) && File.Exists(_item.AttachedFilePath))
        {
            await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(_item.AttachedFilePath)
            });
        }
    }

    private async void OnPickSubmissionFileClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync();
            if (result != null)
            {
                _submissionFilePath = result.FullPath;
                LblSubmittedFileName.Text = $"📄 {result.FileName}";
                LblSubmittedFileName.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo adjuntar el archivo: {ex.Message}", "OK");
        }
    }

    private async void OnSubmitTaskClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_submissionFilePath))
        {
            await DisplayAlert("Sin archivo", "Primero debes adjuntar un archivo para entregar la tarea.", "OK");
            return;
        }

        if (BtnSubmit.Text == "Anular entrega")
        {
            _submissionFilePath = null;
            LblSubmittedFileName.IsVisible = false;
            LblSubmissionStatus.Text = "Sin entregar";
            LblSubmissionStatus.TextColor = Color.FromArgb("#D93025");
            BtnSubmit.Text = "Entregar";
            BtnSubmit.BackgroundColor = Color.FromArgb("#1A73E8");
            return;
        }

        var sub = new TaskSubmission
        {
            ContentItemId = _item.Id,
            UserId = _mainViewModel.CurrentUser.Id,
            FilePath = _submissionFilePath
        };

        bool ok = await _databaseService.SaveTaskSubmissionAsync(sub);
        if (ok)
        {
            await DisplayAlert("Éxito", "Tarea entregada correctamente.", "OK");
            await LoadStudentSubmissionAsync();
        }
    }
}
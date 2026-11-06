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

        bool esTarea = _item.ItemType == "Tarea";

        StackPoints.IsVisible = esTarea;
        LblPoints.Text = $"{_item.Points} puntos";

        if (esTarea)
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
            BorderStudentSubmission.IsVisible = false;
            LblSubmissionStatusTop.IsVisible = false;
        }

        // Seccion del archivo adjunto del profesor (Materiales de referencia)
        bool hasTeacherFile = !string.IsNullOrEmpty(_item.AttachedFilePath) && File.Exists(_item.AttachedFilePath);
        LayoutTeacherAttachmentSection.IsVisible = hasTeacherFile;
        LayoutTeacherAttachment.IsVisible = hasTeacherFile;
        if (hasTeacherFile)
        {
            LblTeacherFileName.Text = Path.GetFileName(_item.AttachedFilePath);
        }
    }

    private async Task LoadStudentSubmissionAsync()
    {
        if (_mainViewModel?.CurrentUser == null) return;

        var sub = await _databaseService.GetTaskSubmissionAsync(_item.Id, _mainViewModel.CurrentUser.Id);
        if (sub != null && !string.IsNullOrEmpty(sub.FilePath))
        {
            _submissionFilePath = sub.FilePath;
            LblSubmittedFileName.Text = Path.GetFileName(sub.FilePath);
            LayoutStudentAttachment.IsVisible = true;

            LblSubmissionStatus.Text = "Entregado";
            LblSubmissionStatus.TextColor = Color.FromArgb("#5EEAD4");
            LblSubmissionStatusTop.Text = "Entregado";
            BtnSubmit.Text = "Anular entrega";
            BtnSubmit.BackgroundColor = Color.FromArgb("#D93025");
            BtnSubmit.TextColor = Colors.White;
        }
        else
        {
            _submissionFilePath = null;
            LayoutStudentAttachment.IsVisible = false;
            LblSubmissionStatus.Text = "Sin entregar";
            LblSubmissionStatus.TextColor = Color.FromArgb("#5EEAD4");
            LblSubmissionStatusTop.Text = "Asignada";
            BtnSubmit.Text = "Entregar tarea";
            BtnSubmit.BackgroundColor = Color.FromArgb("#5EEAD4");
            BtnSubmit.TextColor = Color.FromArgb("#0F172A");
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
                LblSubmittedFileName.Text = result.FileName;
                LayoutStudentAttachment.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo seleccionar el archivo: {ex.Message}", "OK");
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

    private async void OnOpenSubmittedFileClicked(object sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(_submissionFilePath) && File.Exists(_submissionFilePath))
        {
            await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(_submissionFilePath)
            });
        }
        else
        {
            await DisplayAlert("Atención", "El archivo no se encuentra disponible en este dispositivo para visualizar.", "OK");
        }
    }

    private async void OnSubmitTaskClicked(object sender, EventArgs e)
    {
        if (BtnSubmit.Text == "Anular entrega")
        {
            bool confirm = await DisplayAlert("Anular entrega", "¿Quieres anular la entrega de esta tarea?", "Sí, anular", "Cancelar");
            if (!confirm) return;

            _submissionFilePath = null;
            LayoutStudentAttachment.IsVisible = false;
            LblSubmissionStatus.Text = "Sin entregar";
            LblSubmissionStatusTop.Text = "Asignada";
            BtnSubmit.Text = "Entregar tarea";
            BtnSubmit.BackgroundColor = Color.FromArgb("#5EEAD4");
            BtnSubmit.TextColor = Color.FromArgb("#0F172A");
            return;
        }

        if (string.IsNullOrEmpty(_submissionFilePath))
        {
            await DisplayAlert("Sin archivo", "Primero debes adjuntar un archivo para entregar la tarea.", "OK");
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
        else
        {
            await DisplayAlert("Error", "No se pudo guardar la entrega en la base de datos.", "OK");
        }
    }
}
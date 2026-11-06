using Examen2.Data;
using Examen2.ViewModels;

namespace Examen2;

public partial class ContentDetailPage : ContentPage
{
    private readonly ClassContentItem _item;
    private readonly DatabaseService _databaseService;
    private readonly MainViewModel _mainViewModel;
    private string _submissionFilePath;
    private FileResult _pickedFileResult;

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

        // --- CORRECCIÓN: Evaluar si el adjunto del profesor es URL o ruta local existente ---
        bool isUrl = !string.IsNullOrEmpty(_item.AttachedFilePath) &&
                     (_item.AttachedFilePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                      _item.AttachedFilePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase));

        bool isLocalFile = !string.IsNullOrEmpty(_item.AttachedFilePath) && File.Exists(_item.AttachedFilePath);

        bool hasTeacherFile = isUrl || isLocalFile;

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
                _pickedFileResult = result;
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

    // --- MÉTODO DEL PROFESOR ACTUALIZADO PARA ABRIR URLS EN NAVEGADOR ---
    private async void OnOpenTeacherFileClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_item.AttachedFilePath)) return;

        try
        {
            if (_item.AttachedFilePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                _item.AttachedFilePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                await Browser.Default.OpenAsync(new Uri(_item.AttachedFilePath), BrowserLaunchMode.SystemPreferred);
            }
            else if (File.Exists(_item.AttachedFilePath))
            {
                await Launcher.Default.OpenAsync(new OpenFileRequest
                {
                    File = new ReadOnlyFile(_item.AttachedFilePath)
                });
            }
            else
            {
                await DisplayAlert("Atención", "El archivo no se encuentra disponible en este dispositivo.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo abrir el archivo: {ex.Message}", "OK");
        }
    }

    private async void OnOpenSubmittedFileClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_submissionFilePath))
        {
            await DisplayAlert("Atención", "No hay ningún archivo adjunto para visualizar.", "OK");
            return;
        }

        try
        {
            if (_submissionFilePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                _submissionFilePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                await Browser.Default.OpenAsync(new Uri(_submissionFilePath), BrowserLaunchMode.SystemPreferred);
            }
            else if (File.Exists(_submissionFilePath))
            {
                await Launcher.Default.OpenAsync(new OpenFileRequest
                {
                    File = new ReadOnlyFile(_submissionFilePath)
                });
            }
            else
            {
                await DisplayAlert("Atención", "El archivo local no se encuentra disponible en este dispositivo.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo abrir el archivo: {ex.Message}", "OK");
        }
    }

    private async void OnSubmitTaskClicked(object sender, EventArgs e)
    {
        if (BtnSubmit.Text == "Anular entrega")
        {
            bool confirm = await DisplayAlert("Anular entrega", "¿Quieres anular la entrega de esta tarea?", "Sí, anular", "Cancelar");
            if (!confirm) return;

            _submissionFilePath = null;
            _pickedFileResult = null;
            LayoutStudentAttachment.IsVisible = false;
            LblSubmissionStatus.Text = "Sin entregar";
            LblSubmissionStatusTop.Text = "Asignada";
            BtnSubmit.Text = "Entregar tarea";
            BtnSubmit.BackgroundColor = Color.FromArgb("#5EEAD4");
            BtnSubmit.TextColor = Color.FromArgb("#0F172A");
            return;
        }

        if (_pickedFileResult == null && string.IsNullOrEmpty(_submissionFilePath))
        {
            await DisplayAlert("Sin archivo", "Primero debes adjuntar un archivo para entregar la tarea.", "OK");
            return;
        }

        BtnSubmit.IsEnabled = false;
        BtnSubmit.Text = "Subiendo archivo...";

        try
        {
            Stream fileStream = null;
            string fileName = "archivo_adjunto";

            if (_pickedFileResult != null)
            {
                fileStream = await _pickedFileResult.OpenReadAsync();
                fileName = _pickedFileResult.FileName;
            }
            else if (!string.IsNullOrEmpty(_submissionFilePath) && File.Exists(_submissionFilePath))
            {
                fileStream = File.OpenRead(_submissionFilePath);
                fileName = Path.GetFileName(_submissionFilePath);
            }

            var (publicUrl, errorMessage) = await _databaseService.UploadFileStreamAsync(fileStream, fileName);
            fileStream?.Dispose();

            if (!string.IsNullOrEmpty(errorMessage) || string.IsNullOrEmpty(publicUrl))
            {
                await DisplayAlert("Error de Supabase", $"Detalle técnico: {errorMessage}", "OK");
                BtnSubmit.IsEnabled = true;
                BtnSubmit.Text = "Entregar tarea";
                return;
            }

            var sub = new TaskSubmission
            {
                ContentItemId = _item.Id,
                UserId = _mainViewModel.CurrentUser.Id,
                FilePath = publicUrl
            };

            bool ok = await _databaseService.SaveTaskSubmissionAsync(sub);
            BtnSubmit.IsEnabled = true;

            if (ok)
            {
                await DisplayAlert("Éxito", "Tarea entregada correctamente en la nube.", "OK");
                await LoadStudentSubmissionAsync();
            }
            else
            {
                await DisplayAlert("Error", "No se pudo guardar la entrega en la base de datos.", "OK");
            }
        }
        catch (Exception ex)
        {
            BtnSubmit.IsEnabled = true;
            BtnSubmit.Text = "Entregar tarea";
            await DisplayAlert("Error inesperado", ex.Message, "OK");
        }
    }
}
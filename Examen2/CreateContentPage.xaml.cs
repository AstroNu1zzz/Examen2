using Examen2.Data;
using Examen2.ViewModels;

namespace Examen2;

public partial class CreateContentPage : ContentPage
{
    private readonly int _courseId;
    private readonly string _itemType;
    private readonly DatabaseService _databaseService;
    private readonly MainViewModel _mainViewModel;
    private string _selectedFilePath;
    private FileResult _pickedFileResult; // Guardar la referencia directa para abrir el Stream en Android/Windows

    public CreateContentPage(int courseId, string itemType, DatabaseService databaseService, MainViewModel mainViewModel)
    {
        InitializeComponent();
        _courseId = courseId;
        _itemType = itemType;
        _databaseService = databaseService;
        _mainViewModel = mainViewModel;

        LblHeaderTitle.Text = $"Crear {_itemType}";
        LayoutDueDate.IsVisible = (_itemType == "Tarea");
        DpDueDate.MinimumDate = DateTime.Today;
    }

    private async void OnPickFileClicked(object sender, EventArgs e)
    {
        try
        {
            var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, new[] { ".pdf", ".jpg", ".jpeg", ".png" } },
                { DevicePlatform.Android, new[] { "application/pdf", "image/*" } }
            });

            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Selecciona una imagen o PDF",
                FileTypes = customFileType
            });

            if (result != null)
            {
                _pickedFileResult = result;
                _selectedFilePath = result.FullPath;
                LblAttachedFileName.Text = $"📎 {result.FileName}";
                LblAttachedFileName.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo seleccionar el archivo: {ex.Message}", "OK");
        }
    }

    private async void OnPublishClicked(object sender, EventArgs e)
    {
        string title = TxtTitle.Text?.Trim();
        string description = TxtDescription.Text?.Trim();

        if (string.IsNullOrEmpty(title))
        {
            await DisplayAlert("Campo requerido", "Ingresa un título para la publicación.", "OK");
            return;
        }

        DateTime? dueDate = null;
        if (_itemType == "Tarea")
        {
            DateTime fechaBase = DpDueDate.Date ?? DateTime.Today;
            TimeSpan horaSeleccionada = TpDueTime.Time ?? new TimeSpan(23, 59, 0);
            dueDate = fechaBase.Date.Add(horaSeleccionada);
        }

        string attachedFilePublicUrl = null;

        // Subir archivo a Supabase Storage si se seleccionó uno
        if (_pickedFileResult != null || !string.IsNullOrEmpty(_selectedFilePath))
        {
            try
            {
                Stream fileStream = null;
                string fileName = "adjunto_profesor";

                if (_pickedFileResult != null)
                {
                    fileStream = await _pickedFileResult.OpenReadAsync();
                    fileName = _pickedFileResult.FileName;
                }
                else if (File.Exists(_selectedFilePath))
                {
                    fileStream = File.OpenRead(_selectedFilePath);
                    fileName = Path.GetFileName(_selectedFilePath);
                }

                if (fileStream != null)
                {
                    var (publicUrl, errorMessage) = await _databaseService.UploadFileStreamAsync(fileStream, fileName);
                    fileStream.Dispose();

                    if (!string.IsNullOrEmpty(errorMessage) || string.IsNullOrEmpty(publicUrl))
                    {
                        await DisplayAlert("Error de carga", $"No se pudo subir el archivo adjunto: {errorMessage}", "OK");
                        return;
                    }

                    attachedFilePublicUrl = publicUrl;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Ocurrió un error al procesar el archivo: {ex.Message}", "OK");
                return;
            }
        }

        var newItem = new ClassContentItem
        {
            CourseId = _courseId,
            ItemType = _itemType,
            Title = title,
            Description = description,
            DueDate = dueDate,
            AuthorName = _mainViewModel.CurrentUser?.Username ?? "Profesor",
            AttachedFilePath = attachedFilePublicUrl // Guardo la URL pública https://... de Supabase
        };

        bool success = await _databaseService.AddClassContentItemAsync(newItem);

        if (success)
        {
            await _mainViewModel.RefreshCoursesAsync();
            await Navigation.PopAsync();
        }
        else
        {
            await DisplayAlert("Error", "No se pudo crear la publicación en la nube.", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
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
                FileTypes = customFileType // <--- Cambiado a FileTypes (en plural)
            });

            if (result != null)
            {
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

        var newItem = new ClassContentItem
        {
            CourseId = _courseId,
            ItemType = _itemType,
            Title = title,
            Description = description,
            DueDate = (_itemType == "Tarea") ? DpDueDate.Date : null,
            AuthorName = _mainViewModel.CurrentUser?.Username ?? "Profesor",
            AttachedFilePath = _selectedFilePath
        };

        bool success = await _databaseService.AddClassContentItemAsync(newItem);

        if (success)
        {
            await Navigation.PopAsync();
        }
        else
        {
            await DisplayAlert("Error", "No se pudo guardar la publicación.", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
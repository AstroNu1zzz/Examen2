using Examen2.Data;

namespace Examen2;

public partial class ManageUnitsPage : ContentPage
{
    private readonly int _courseId;
    private readonly DatabaseService _databaseService;

    public ManageUnitsPage(int courseId, DatabaseService databaseService)
    {
        InitializeComponent();
        _courseId = courseId;
        _databaseService = databaseService;

        DpStart.Date = DateTime.Now;
        DpEnd.Date = DateTime.Now.AddDays(30);

        _ = LoadUnitsAsync();
    }

    private async Task LoadUnitsAsync()
    {
        var units = await _databaseService.GetCourseUnitsAsync(_courseId);
        CvUnits.ItemsSource = units;
    }

    private async void OnAddUnitClicked(object sender, EventArgs e)
    {
        string title = TxtUnitTitle.Text?.Trim();

        if (string.IsNullOrWhiteSpace(title))
        {
            await DisplayAlert("Atención", "Ingresa un nombre para la unidad.", "OK");
            return;
        }

        DateTime rawStart = DpStart.Date ?? DateTime.Now;
        DateTime rawEnd = DpEnd.Date ?? DateTime.Now.AddDays(30);

        if (rawEnd < rawStart)
        {
            await DisplayAlert("Error", "La fecha fin debe ser posterior a la fecha de inicio.", "OK");
            return;
        }

        // CORRECCIÓN DE ZONA HORARIA: Fijar a las 12:00:00 evita que el desfase UTC reste un día
        DateTime startDate = rawStart.Date.AddHours(12);
        DateTime endDate = rawEnd.Date.AddHours(12);

        var newUnit = new CourseUnit
        {
            CourseId = _courseId,
            Title = title,
            StartDate = startDate,
            EndDate = endDate
        };

        bool ok = await _databaseService.AddCourseUnitAsync(newUnit);

        if (ok)
        {
            TxtUnitTitle.Text = string.Empty;
            await LoadUnitsAsync();
        }
        else
        {
            await DisplayAlert("Error", "No se pudo guardar la unidad.", "OK");
        }
    }
}   
namespace Examen2;

public partial class CourseDetailPage : ContentPage
{
    public CourseDetailPage(ViewModels.Course course)
    {
        InitializeComponent();
        BindingContext = course;
    }
}
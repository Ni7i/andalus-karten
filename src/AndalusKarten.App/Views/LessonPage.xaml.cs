using AndalusKarten.ViewModels;

namespace AndalusKarten.Views;

public partial class LessonPage : ContentPage
{
    private readonly LessonViewModel _viewModel;

    public LessonPage(LessonViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadCommand.Execute(null);
    }
}

using AndalusKarten.ViewModels;

namespace AndalusKarten.Views;

public partial class LessonPage : ContentPage
{
    private readonly LessonViewModel _vm;
    private bool _flipping;

    public LessonPage(LessonViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _vm = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.LoadCommand.Execute(null);
    }

    private async void OnPreviewTapped(object? sender, TappedEventArgs e)
    {
        if (_flipping)
        {
            return;
        }

        _flipping = true;
        try
        {
            await Preview.RotateYToAsync(90, 110, Easing.CubicIn);
            _vm.FlipPreviewCommand.Execute(null);
            Preview.RotationY = -90;
            await Preview.RotateYToAsync(0, 110, Easing.CubicOut);
        }
        finally
        {
            Preview.RotationY = 0;
            _flipping = false;
        }
    }
}

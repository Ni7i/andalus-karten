using AndalusKarten.ViewModels;

namespace AndalusKarten.Views;

public partial class LearnPage : ContentPage
{
    public LearnPage(LearnViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

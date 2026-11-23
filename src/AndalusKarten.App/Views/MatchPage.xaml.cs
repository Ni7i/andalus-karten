using AndalusKarten.ViewModels;

namespace AndalusKarten.Views;

public partial class MatchPage : ContentPage
{
    public MatchPage(MatchViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

using System.ComponentModel;
using AndalusKarten.ViewModels;

namespace AndalusKarten.Views;

public partial class StudyPage : ContentPage
{
    private readonly StudyViewModel _viewModel;

    public StudyPage(StudyViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnCardTapped(object? sender, TappedEventArgs e)
    {
        if (_viewModel.IsHidden)
        {
            _viewModel.RevealCommand.Execute(null);
        }
    }

    // Kleine Animationen: Karte „klappt“ beim Aufdecken, neue Karte gleitet herein.
    private async void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        try
        {
            if (e.PropertyName == nameof(StudyViewModel.IsRevealed))
            {
                Card.CancelAnimations();
                if (_viewModel.IsRevealed)
                {
                    Card.RotationX = 70;
                    await Card.RotateXToAsync(0, 160, Easing.CubicOut);
                }
                else
                {
                    Card.RotationX = 0;
                    Card.Opacity = 0;
                    Card.TranslationX = 40;
                    await Task.WhenAll(
                        Card.FadeToAsync(1, 160),
                        Card.TranslateToAsync(0, 0, 160, Easing.CubicOut));
                }
            }
        }
        catch (Exception)
        {
            // Animationen sind Kosmetik – Fehler dürfen das Lernen nicht stören.
            Card.RotationX = 0;
            Card.Opacity = 1;
            Card.TranslationX = 0;
        }
    }
}

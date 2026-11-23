using System.ComponentModel;
using AndalusKarten.Services;
using AndalusKarten.ViewModels;

namespace AndalusKarten.Views;

public partial class FlashcardsPage : ContentPage
{
    private const double SwipeThreshold = 110;
    private readonly FlashcardsViewModel _vm;
    private readonly DonutDrawable _donut = new();
    private bool _busy;
    private double _panX;

    public FlashcardsPage(FlashcardsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _vm = viewModel;
        Donut.Drawable = _donut;
        _vm.PropertyChanged += OnVmChanged;
    }

    private void OnVmChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FlashcardsViewModel.ResultRatio))
        {
            _donut.Ratio = _vm.ResultRatio;
            Donut.Invalidate();
        }
    }

    // Umdrehen: Karte dreht sich zur Hälfte, Seite wechselt, dreht zurück.
    private async void OnCardTapped(object? sender, TappedEventArgs e)
    {
        if (_busy || Math.Abs(Card.TranslationX) > 4)
        {
            return;
        }

        _busy = true;
        try
        {
            await Card.RotateYToAsync(90, 120, Easing.CubicIn);
            _vm.Flip();
            Card.RotationY = -90;
            await Card.RotateYToAsync(0, 120, Easing.CubicOut);
        }
        finally
        {
            Card.RotationY = 0;
            _busy = false;
        }
    }

    private async void OnCardPanned(object? sender, PanUpdatedEventArgs e)
    {
        if (_busy)
        {
            return;
        }

        switch (e.StatusType)
        {
            case GestureStatus.Running:
                _panX = e.TotalX;
                Card.TranslationX = _panX;
                Card.TranslationY = e.TotalY * 0.15;
                Card.Rotation = _panX / 22;
                UpdateOverlay(_panX);
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                if (Math.Abs(_panX) > SwipeThreshold)
                {
                    await FlyOutAsync(_panX > 0);
                }
                else
                {
                    await ResetCardAsync();
                }

                _panX = 0;
                break;
        }
    }

    private async void OnKnownClicked(object? sender, EventArgs e) => await FlyOutAsync(true);

    private async void OnLearningClicked(object? sender, EventArgs e) => await FlyOutAsync(false);

    private async Task FlyOutAsync(bool knew)
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        try
        {
            UpdateOverlay(knew ? SwipeThreshold * 1.5 : -SwipeThreshold * 1.5);
            var target = (knew ? 1 : -1) * (Width + 200);
            await Task.WhenAll(
                Card.TranslateToAsync(target, Card.TranslationY + 40, 220, Easing.CubicIn),
                Card.RotateToAsync(knew ? 18 : -18, 220),
                SwipeOverlay.FadeToAsync(0, 220));

            await _vm.SwipeAsync(knew);

            // Neue Karte kommt von hinten nach vorne.
            Card.TranslationX = 0;
            Card.TranslationY = 0;
            Card.Rotation = 0;
            Card.Scale = 0.92;
            Card.Opacity = 0;
            await Task.WhenAll(Card.ScaleToAsync(1, 160, Easing.CubicOut), Card.FadeToAsync(1, 160));
        }
        finally
        {
            Card.Opacity = 1;
            Card.Scale = 1;
            _busy = false;
        }
    }

    private async Task ResetCardAsync()
    {
        await Task.WhenAll(
            Card.TranslateToAsync(0, 0, 180, Easing.SpringOut),
            Card.RotateToAsync(0, 180, Easing.SpringOut),
            SwipeOverlay.FadeToAsync(0, 120));
    }

    private void UpdateOverlay(double x)
    {
        var knew = x > 0;
        var color = Palette.Get(knew ? "Known" : "Learning");
        SwipeOverlay.Stroke = color;
        SwipeOverlay.TranslationX = Card.TranslationX;
        SwipeOverlay.TranslationY = Card.TranslationY;
        SwipeOverlay.Rotation = Card.Rotation;
        SwipeLabel.Text = knew ? "Gewusst" : "Lerne ich noch";
        SwipeLabel.TextColor = color;
        SwipeOverlay.Opacity = Math.Clamp(Math.Abs(x) / SwipeThreshold, 0, 1);
    }

    private sealed class DonutDrawable : IDrawable
    {
        public double Ratio { get; set; }

        public void Draw(ICanvas canvas, RectF rect)
        {
            const float stroke = 12;
            var r = new RectF(rect.X + stroke / 2, rect.Y + stroke / 2, rect.Width - stroke, rect.Height - stroke);
            canvas.StrokeSize = stroke;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.StrokeColor = Palette.Get("LearningSoft");
            canvas.DrawEllipse(r);
            if (Ratio > 0)
            {
                canvas.StrokeColor = Palette.Get("Known");
                var sweep = (float)(360 * Math.Min(Ratio, 0.9999));
                canvas.DrawArc(r, 90, 90 - sweep, true, false);
            }
        }
    }
}

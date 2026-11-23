using AndalusKarten.Core.Models;
using AndalusKarten.ViewModels;

namespace AndalusKarten.Views;

public partial class LessonListPage : ContentPage
{
    private readonly LessonsViewModel _viewModel;

    protected LessonListPage(LessonsViewModel viewModel, LessonKind kind, string title)
    {
        InitializeComponent();
        viewModel.Kind = kind;
        Title = title;
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadCommand.Execute(null);
    }

    private void OnLessonTapped(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject { BindingContext: LessonItem item })
        {
            _viewModel.OpenLessonCommand.Execute(item);
        }
    }
}

public sealed class LessonsPage(LessonsViewModel viewModel)
    : LessonListPage(viewModel, LessonKind.Lesson, "Lektionen");

public sealed class EssentialPage(LessonsViewModel viewModel)
    : LessonListPage(viewModel, LessonKind.Essential, "Essential");

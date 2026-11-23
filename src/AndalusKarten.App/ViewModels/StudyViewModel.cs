using AndalusKarten.Core.Models;
using AndalusKarten.Core.Services;
using AndalusKarten.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AndalusKarten.ViewModels;

public sealed partial class StudyViewModel(ProgressService progress, StudySettings settings)
    : ObservableObject, IQueryAttributable
{
    private const double ArabicFontSize = 46;
    private const double LatinFontSize = 30;

    private StudyRequest? _request;
    private StudySession? _session;

    [ObservableProperty]
    public partial string Title { get; set; } = "";

    [ObservableProperty]
    public partial string Question { get; set; } = "";

    [ObservableProperty]
    public partial string Answer { get; set; } = "";

    [ObservableProperty]
    public partial string? Hint { get; set; }

    [ObservableProperty]
    public partial string? Note { get; set; }

    [ObservableProperty]
    public partial bool QuestionIsArabic { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHidden))]
    public partial bool IsRevealed { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStudying))]
    public partial bool IsFinished { get; set; }

    [ObservableProperty]
    public partial string ProgressText { get; set; } = "";

    [ObservableProperty]
    public partial double ProgressValue { get; set; }

    [ObservableProperty]
    public partial string ResultText { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMissed))]
    public partial int MissedCount { get; set; }

    public bool IsHidden => !IsRevealed;
    public bool IsStudying => !IsFinished;
    public bool HasMissed => MissedCount > 0;
    public bool HasHint => !string.IsNullOrEmpty(Hint);
    public bool HasNote => !string.IsNullOrEmpty(Note);

    public double QuestionFontSize => QuestionIsArabic ? ArabicFontSize : LatinFontSize;
    public double AnswerFontSize => QuestionIsArabic ? LatinFontSize : ArabicFontSize;
    public FlowDirection QuestionFlow => QuestionIsArabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    public FlowDirection AnswerFlow => QuestionIsArabic ? FlowDirection.LeftToRight : FlowDirection.RightToLeft;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(StudyRequest.QueryKey, out var value) && value is StudyRequest request)
        {
            _request = request;
            Title = request.Title;
            Start(request.Words);
        }
    }

    [RelayCommand]
    private void Reveal() => IsRevealed = true;

    [RelayCommand]
    private Task KnewAsync() => GradeAsync(true);

    [RelayCommand]
    private Task AgainAsync() => GradeAsync(false);

    [RelayCommand]
    private void RepeatMissed()
    {
        if (_request is null || _session is null)
        {
            return;
        }

        var missed = _session.MissedWordIds.ToHashSet();
        Start(_request.Words.Where(w => missed.Contains(w.Id)).ToList());
    }

    [RelayCommand]
    private void RepeatAll()
    {
        if (_request is not null)
        {
            Start(_request.Words);
        }
    }

    [RelayCommand]
    private Task CloseAsync() => Shell.Current.GoToAsync("..");

    private void Start(IReadOnlyList<Word> words)
    {
        _session = new StudySession(words);
        IsFinished = false;
        ShowCurrent();
    }

    private async Task GradeAsync(bool knewIt)
    {
        if (_session?.Current is not { } word || !IsRevealed)
        {
            return;
        }

        _session.Answer(knewIt);
        await progress.GradeAsync(word.Id, knewIt);
        ShowCurrent();
    }

    private void ShowCurrent()
    {
        var session = _session!;
        ProgressValue = session.Total == 0 ? 1 : (double)session.Done / session.Total;
        ProgressText = $"{session.Done} / {session.Total}";

        if (session.Current is not { } word)
        {
            IsFinished = true;
            MissedCount = session.MissedWordIds.Count;
            ResultText = session.Wrong == 0
                ? $"Alle {session.Total} Karten auf Anhieb gewusst. ممتاز!"
                : $"{session.Total - MissedCount} von {session.Total} auf Anhieb gewusst.";
            return;
        }

        QuestionIsArabic = settings.Direction switch
        {
            CardDirection.ArabicToGerman => true,
            CardDirection.GermanToArabic => false,
            _ => Random.Shared.Next(2) == 0,
        };

        Question = QuestionIsArabic ? word.Arabic : word.German;
        Answer = QuestionIsArabic ? word.German : word.Arabic;
        Hint = settings.ShowTransliteration ? word.Transliteration : null;
        Note = word.Note;
        IsRevealed = false;

        OnPropertyChanged(nameof(HasHint));
        OnPropertyChanged(nameof(HasNote));
        OnPropertyChanged(nameof(QuestionFontSize));
        OnPropertyChanged(nameof(AnswerFontSize));
        OnPropertyChanged(nameof(QuestionFlow));
        OnPropertyChanged(nameof(AnswerFlow));
    }
}

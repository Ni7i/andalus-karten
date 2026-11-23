using AndalusKarten.Core.Models;
using AndalusKarten.Core.Services;
using AndalusKarten.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AndalusKarten.ViewModels;

public sealed partial class FlashcardsViewModel(ProgressService progress, StudySettings settings, StarService stars)
    : ObservableObject, IQueryAttributable
{
    private readonly Stack<CardProgress> _undoProgress = new();
    private StudyRequest? _request;
    private FlashcardRound? _round;
    private IReadOnlyList<Word> _roundWords = [];
    private bool _frontIsArabic = true;

    [ObservableProperty] public partial string Title { get; set; } = "";
    [ObservableProperty] public partial bool IsFlipped { get; set; }
    [ObservableProperty] public partial bool IsFinished { get; set; }
    [ObservableProperty] public partial bool Shuffle { get; set; }
    [ObservableProperty] public partial string FaceText { get; set; } = "";
    [ObservableProperty] public partial string FaceLabel { get; set; } = "";
    [ObservableProperty] public partial string? FaceHint { get; set; }
    [ObservableProperty] public partial bool FaceIsArabic { get; set; }
    [ObservableProperty] public partial bool IsStarred { get; set; }
    [ObservableProperty] public partial int KnownCount { get; set; }
    [ObservableProperty] public partial int LearningCount { get; set; }
    [ObservableProperty] public partial string CounterText { get; set; } = "";
    [ObservableProperty] public partial double ProgressValue { get; set; }
    [ObservableProperty] public partial bool CanUndo { get; set; }
    [ObservableProperty] public partial string ResultTitle { get; set; } = "";
    [ObservableProperty] public partial string ResultPercent { get; set; } = "";
    [ObservableProperty] public partial double ResultRatio { get; set; }
    [ObservableProperty] public partial string ContinueText { get; set; } = "";
    [ObservableProperty] public partial bool HasStillLearning { get; set; }

    public bool IsStudying => !IsFinished;
    public bool HasHint => !string.IsNullOrEmpty(FaceHint);
    public double FaceFontSize => FaceIsArabic ? 48 : 28;
    public FlowDirection FaceFlow => FaceIsArabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    public string StarGlyph => IsStarred ? "★" : "☆";
    public string ShuffleText => Shuffle ? "Gemischt" : "Mischen";

    partial void OnIsFinishedChanged(bool value) => OnPropertyChanged(nameof(IsStudying));
    partial void OnIsStarredChanged(bool value) => OnPropertyChanged(nameof(StarGlyph));
    partial void OnShuffleChanged(bool value) => OnPropertyChanged(nameof(ShuffleText));

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(StudyRequest.QueryKey, out var v) && v is StudyRequest request)
        {
            _request = request;
            Title = request.Title;
            Start(request.Words);
        }
    }

    /// <summary>Karte umdrehen (die Animation macht die Seite).</summary>
    public void Flip()
    {
        IsFlipped = !IsFlipped;
        ShowFace();
    }

    /// <summary>Rechts = gewusst, links = lerne ich noch.</summary>
    public async Task SwipeAsync(bool knew)
    {
        if (_round?.Current is not { } word)
        {
            return;
        }

        _undoProgress.Push(progress.Get(word.Id));
        _round.Answer(knew);
        await progress.GradeAsync(word.Id, knew);
        ShowCurrent();
    }

    [RelayCommand]
    private async Task UndoAsync()
    {
        if (_round?.Undo() is { } last && _undoProgress.TryPop(out var previous))
        {
            await progress.RestoreAsync(last.Word.Id, previous);
            ShowCurrent();
        }
    }

    [RelayCommand]
    private void ToggleShuffle()
    {
        Shuffle = !Shuffle;
        Start(_roundWords);
    }

    [RelayCommand]
    private void ToggleStar()
    {
        if (_round?.Current is { } word)
        {
            IsStarred = stars.Toggle(word.Id);
        }
    }

    [RelayCommand]
    private void ContinueLearning()
    {
        if (_round is { StillLearning.Count: > 0 } round)
        {
            Start(round.StillLearning);
        }
    }

    [RelayCommand]
    private void Restart() => Start(_request?.Words ?? _roundWords);

    [RelayCommand]
    private Task CloseAsync() => Shell.Current.GoToAsync("..");

    private void Start(IReadOnlyList<Word> words)
    {
        _roundWords = words;
        _round = new FlashcardRound(words, Shuffle);
        _undoProgress.Clear();
        IsFinished = false;
        ShowCurrent();
    }

    private void ShowCurrent()
    {
        var round = _round!;
        KnownCount = round.KnownCount;
        LearningCount = round.LearningCount;
        CanUndo = round.CanUndo;
        ProgressValue = round.Total == 0 ? 0 : (double)round.Index / round.Total;
        CounterText = $"{Math.Min(round.Index + 1, round.Total)} / {round.Total}";

        if (round.Current is not { } word)
        {
            IsFinished = true;
            ResultRatio = round.Total == 0 ? 0 : (double)round.KnownCount / round.Total;
            ResultPercent = $"{ResultRatio:P0}";
            ResultTitle = round.LearningCount == 0
                ? "Stark! Du kannst alle Karten."
                : "Weiter so – du machst Fortschritte!";
            HasStillLearning = round.LearningCount > 0;
            ContinueText = $"Mit {round.LearningCount} Karten weiterlernen";
            return;
        }

        _frontIsArabic = settings.Direction switch
        {
            CardDirection.ArabicToGerman => true,
            CardDirection.GermanToArabic => false,
            _ => Random.Shared.Next(2) == 0,
        };
        IsFlipped = false;
        IsStarred = stars.IsStarred(word.Id);
        ShowFace();
    }

    private void ShowFace()
    {
        if (_round?.Current is not { } word)
        {
            return;
        }

        var arabicSide = IsFlipped != _frontIsArabic;
        FaceIsArabic = arabicSide;
        FaceText = arabicSide ? word.Arabic : word.German;
        FaceLabel = arabicSide ? "ARABISCH" : "BEDEUTUNG";
        FaceHint = IsFlipped
            ? string.Join("\n", new[] { settings.ShowTransliteration ? word.Transliteration : null, word.Note }
                .Where(s => !string.IsNullOrWhiteSpace(s)))
            : null;
        OnPropertyChanged(nameof(HasHint));
        OnPropertyChanged(nameof(FaceFontSize));
        OnPropertyChanged(nameof(FaceFlow));
    }
}

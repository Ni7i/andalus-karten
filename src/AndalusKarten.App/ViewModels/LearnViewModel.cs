using System.Collections.ObjectModel;
using AndalusKarten.Core.Models;
using AndalusKarten.Core.Services;
using AndalusKarten.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AndalusKarten.ViewModels;

public enum OptionState { Idle, Correct, Wrong, Dimmed }

public sealed partial class ChoiceOption(Word word, int index, bool showArabic) : ObservableObject
{
    public Word Word { get; } = word;
    public int Index { get; } = index;
    public string Number => (Index + 1).ToString();
    public string Text => showArabic ? Word.Arabic : Word.German;
    public FlowDirection Flow => showArabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    public double FontSize => showArabic ? 24 : 17;

    [ObservableProperty] public partial OptionState State { get; set; }
}

/// <summary>Multiple Choice: Falsch beantwortete Karten kommen später in der Runde wieder.</summary>
public sealed partial class LearnViewModel(ProgressService progress, StudySettings settings)
    : ObservableObject, IQueryAttributable
{
    private readonly Random _random = new();
    private StudyRequest? _request;
    private StudySession? _session;
    private ChoiceQuestion? _question;

    public ObservableCollection<ChoiceOption> Options { get; } = [];

    [ObservableProperty] public partial string Title { get; set; } = "";
    [ObservableProperty] public partial string Prompt { get; set; } = "";
    [ObservableProperty] public partial string PromptLabel { get; set; } = "";
    [ObservableProperty] public partial bool PromptIsArabic { get; set; }
    [ObservableProperty] public partial bool IsAnswered { get; set; }
    [ObservableProperty] public partial bool WasCorrect { get; set; }
    [ObservableProperty] public partial string Feedback { get; set; } = "";
    [ObservableProperty] public partial bool IsFinished { get; set; }
    [ObservableProperty] public partial double ProgressValue { get; set; }
    [ObservableProperty] public partial string ProgressText { get; set; } = "";
    [ObservableProperty] public partial string ResultText { get; set; } = "";

    public bool IsStudying => !IsFinished;
    public double PromptFontSize => PromptIsArabic ? 42 : 26;
    public FlowDirection PromptFlow => PromptIsArabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    partial void OnIsFinishedChanged(bool value) => OnPropertyChanged(nameof(IsStudying));

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(StudyRequest.QueryKey, out var v) && v is StudyRequest request)
        {
            _request = request;
            Title = request.Title;
            Start();
        }
    }

    [RelayCommand]
    private async Task ChooseAsync(ChoiceOption option)
    {
        if (IsAnswered || _question is null || _session?.Current is not { } word)
        {
            return;
        }

        IsAnswered = true;
        WasCorrect = option.Index == _question.CorrectIndex;
        foreach (var o in Options)
        {
            o.State = o.Index == _question.CorrectIndex ? OptionState.Correct
                : o == option ? OptionState.Wrong
                : OptionState.Dimmed;
        }

        Feedback = WasCorrect ? Praise() : "Noch nicht ganz – das merkst du dir!";
        _session.Answer(WasCorrect);
        await progress.GradeAsync(word.Id, WasCorrect);

        if (WasCorrect)
        {
            await Task.Delay(900);
            if (IsAnswered)
            {
                Next();
            }
        }
    }

    [RelayCommand]
    private void Next()
    {
        IsAnswered = false;
        ShowCurrent();
    }

    [RelayCommand]
    private void Start()
    {
        if (_request is null)
        {
            return;
        }

        _session = new StudySession(_request.Words, _random);
        IsFinished = false;
        ShowCurrent();
    }

    [RelayCommand]
    private Task CloseAsync() => Shell.Current.GoToAsync("..");

    private void ShowCurrent()
    {
        var session = _session!;
        ProgressValue = session.Total == 0 ? 1 : (double)session.Done / session.Total;
        ProgressText = $"{session.Done} / {session.Total}";

        if (session.Current is not { } word)
        {
            IsFinished = true;
            ResultText = session.Wrong == 0
                ? $"Alle {session.Total} Begriffe beim ersten Versuch richtig. ما شاء الله!"
                : $"{session.Total - session.MissedWordIds.Count} von {session.Total} beim ersten Versuch richtig.";
            return;
        }

        PromptIsArabic = settings.Direction switch
        {
            CardDirection.ArabicToGerman => true,
            CardDirection.GermanToArabic => false,
            _ => _random.Next(2) == 0,
        };
        Prompt = PromptIsArabic ? word.Arabic : word.German;
        PromptLabel = PromptIsArabic ? "BEGRIFF" : "BEDEUTUNG";
        OnPropertyChanged(nameof(PromptFontSize));
        OnPropertyChanged(nameof(PromptFlow));

        _question = QuizBuilder.Choice(word, _request!.Pool, _random);
        Options.Clear();
        for (var i = 0; i < _question.Options.Count; i++)
        {
            Options.Add(new ChoiceOption(_question.Options[i], i, showArabic: !PromptIsArabic));
        }
    }

    private string Praise() => _random.Next(4) switch
    {
        0 => "Super!",
        1 => "Richtig!",
        2 => "Genau so!",
        _ => "Weiter so!",
    };
}

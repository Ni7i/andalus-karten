using System.Collections.ObjectModel;
using System.Diagnostics;
using AndalusKarten.Core.Models;
using AndalusKarten.Core.Services;
using AndalusKarten.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AndalusKarten.ViewModels;

public enum TileState { Idle, Selected, Wrong, Matched }

public sealed partial class MatchTile(Word word, bool isArabic) : ObservableObject
{
    public Word Word { get; } = word;
    public bool IsArabic { get; } = isArabic;
    public string Text => IsArabic ? Word.Arabic : Word.German;
    public FlowDirection Flow => IsArabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    public double FontSize => IsArabic ? 22 : 15;

    [ObservableProperty] public partial TileState State { get; set; }
}

/// <summary>Zuordnen auf Zeit: Begriff und Bedeutung antippen, bis alle Paare verschwunden sind.</summary>
public sealed partial class MatchViewModel : ObservableObject, IQueryAttributable
{
    private readonly Random _random = new();
    private readonly Stopwatch _watch = new();
    private readonly IDispatcherTimer? _timer;
    private StudyRequest? _request;
    private MatchTile? _selected;
    private int _remaining;

    public MatchViewModel()
    {
        _timer = Application.Current?.Dispatcher.CreateTimer();
        if (_timer is not null)
        {
            _timer.Interval = TimeSpan.FromMilliseconds(100);
            _timer.Tick += (_, _) => TimeText = Format(_watch.Elapsed);
        }
    }

    public ObservableCollection<MatchTile> Tiles { get; } = [];

    [ObservableProperty] public partial string Title { get; set; } = "";
    [ObservableProperty] public partial string TimeText { get; set; } = "0,0 s";
    [ObservableProperty] public partial bool IsFinished { get; set; }
    [ObservableProperty] public partial string ResultText { get; set; } = "";
    [ObservableProperty] public partial string BestText { get; set; } = "";

    public bool IsPlaying => !IsFinished;

    partial void OnIsFinishedChanged(bool value) => OnPropertyChanged(nameof(IsPlaying));

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
    private void Start()
    {
        if (_request is null)
        {
            return;
        }

        var words = QuizBuilder.MatchSet(_request.Words.Count >= 3 ? _request.Words : _request.Pool, _random);
        var tiles = words.SelectMany(w => new[] { new MatchTile(w, true), new MatchTile(w, false) })
            .OrderBy(_ => _random.Next())
            .ToList();

        Tiles.Clear();
        foreach (var t in tiles)
        {
            Tiles.Add(t);
        }

        _remaining = words.Count;
        _selected = null;
        IsFinished = false;
        BestText = Best is { } best ? $"Bestzeit: {Format(best)}" : "";
        _watch.Restart();
        _timer?.Start();
    }

    [RelayCommand]
    private async Task TapAsync(MatchTile tile)
    {
        if (tile.State is TileState.Matched or TileState.Wrong || IsFinished)
        {
            return;
        }

        if (_selected is null || _selected == tile)
        {
            _selected = _selected == tile ? null : tile;
            tile.State = _selected == tile ? TileState.Selected : TileState.Idle;
            return;
        }

        var first = _selected;
        _selected = null;

        if (first.Word.Id == tile.Word.Id && first.IsArabic != tile.IsArabic)
        {
            first.State = tile.State = TileState.Matched;
            if (--_remaining == 0)
            {
                Finish();
            }

            return;
        }

        first.State = tile.State = TileState.Wrong;
        await Task.Delay(450);
        first.State = tile.State = TileState.Idle;
    }

    [RelayCommand]
    private Task CloseAsync() => Shell.Current.GoToAsync("..");

    private void Finish()
    {
        _watch.Stop();
        _timer?.Stop();
        var time = _watch.Elapsed;
        TimeText = Format(time);
        var best = Best;
        if (best is null || time < best)
        {
            Preferences.Default.Set(BestKey, time.TotalMilliseconds);
            ResultText = best is null ? $"Geschafft in {Format(time)}!" : $"Neue Bestzeit: {Format(time)}!";
        }
        else
        {
            ResultText = $"Geschafft in {Format(time)}. Bestzeit: {Format(best.Value)}.";
        }

        IsFinished = true;
    }

    private string BestKey => $"match-best-{_request?.Key}";

    private TimeSpan? Best => Preferences.Default.Get(BestKey, -1d) is var ms and >= 0 ? TimeSpan.FromMilliseconds(ms) : null;

    private static string Format(TimeSpan t) => $"{t.TotalSeconds:0.0} s";
}

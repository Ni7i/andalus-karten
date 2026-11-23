using AndalusKarten.Core.Models;
using AndalusKarten.Core.Services;
using AndalusKarten.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AndalusKarten.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private static readonly FilePickerFileType JsonFiles = new(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        [DevicePlatform.Android] = ["application/json", "application/octet-stream", "text/plain"],
        [DevicePlatform.MacCatalyst] = ["public.json"],
        [DevicePlatform.iOS] = ["public.json"],
        [DevicePlatform.WinUI] = [".json"],
    });

    private readonly VocabularyRepository _vocabulary;
    private readonly ProgressService _progress;
    private readonly StudySettings _settings;

    public SettingsViewModel(VocabularyRepository vocabulary, ProgressService progress, StudySettings settings)
    {
        _vocabulary = vocabulary;
        _progress = progress;
        _settings = settings;
        DirectionIndex = (int)settings.Direction;
        ShowTransliteration = settings.ShowTransliteration;
    }

    public IReadOnlyList<string> Directions { get; } = ["Arabisch → Deutsch", "Deutsch → Arabisch", "Gemischt"];

    [ObservableProperty]
    public partial int DirectionIndex { get; set; }

    [ObservableProperty]
    public partial bool ShowTransliteration { get; set; }

    [ObservableProperty]
    public partial string VocabularyInfo { get; set; } = "";

    [ObservableProperty]
    public partial string SourceInfo { get; set; } = "";

    [ObservableProperty]
    public partial bool IsImported { get; set; }

    public string AppVersion => $"Version {AppInfo.Current.VersionString}";

    partial void OnDirectionIndexChanged(int value)
    {
        if (value >= 0)
        {
            _settings.Direction = (CardDirection)value;
        }
    }

    partial void OnShowTransliterationChanged(bool value) => _settings.ShowTransliteration = value;

    [RelayCommand]
    private async Task LoadAsync()
    {
        var vocab = await _vocabulary.GetAsync();
        var lessons = vocab.OfKind(LessonKind.Lesson).Count();
        var essentials = vocab.OfKind(LessonKind.Essential).Count();
        VocabularyInfo = $"{lessons} Lektionen · {essentials} Essential · {vocab.AllWords.Count()} Wörter";
        IsImported = _vocabulary.IsImported;
        SourceInfo = IsImported
            ? $"Importierte Liste vom {_vocabulary.ImportedAt:dd.MM.yyyy HH:mm}"
            : "Mitgelieferte Liste";
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Vokabeldatei (vocabulary.json) wählen",
                FileTypes = JsonFiles,
            });
            if (file is null)
            {
                return;
            }

            await using var stream = await file.OpenReadAsync();
            var vocab = await _vocabulary.ImportAsync(stream);
            await LoadAsync();
            await Dialogs.AlertAsync("Import erfolgreich",
                $"{vocab.Lessons.Count} Lektionen mit {vocab.AllWords.Count()} Wörtern geladen. " +
                "Der Fortschritt bleibt für gleiche Wort-IDs erhalten.");
        }
        catch (Exception ex) when (ex is FormatException or System.Text.Json.JsonException or IOException)
        {
            await Dialogs.AlertAsync("Import fehlgeschlagen", ex.Message);
        }
    }

    [RelayCommand]
    private async Task ResetVocabularyAsync()
    {
        if (await Dialogs.ConfirmAsync("Mitgelieferte Liste verwenden?",
                "Die importierte Datei wird entfernt.", "Zurücksetzen"))
        {
            await _vocabulary.ResetToBundledAsync();
            await LoadAsync();
        }
    }

    [RelayCommand]
    private async Task ResetProgressAsync()
    {
        if (await Dialogs.ConfirmAsync("Gesamten Fortschritt löschen?",
                "Alle Karten gelten danach wieder als neu. Das lässt sich nicht rückgängig machen.", "Löschen"))
        {
            await _progress.EnsureLoadedAsync();
            await _progress.ResetAsync();
            await Dialogs.AlertAsync("Erledigt", "Der Lernfortschritt wurde zurückgesetzt.");
        }
    }
}

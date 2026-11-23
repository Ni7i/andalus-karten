using System.Text.Json;
using System.Text.Json.Serialization;
using AndalusKarten.Core.Models;

namespace AndalusKarten.Core.Services;

/// <summary>Source-generierte JSON-Metadaten: trimming-sicher und ohne Reflection.</summary>
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true)]
[JsonSerializable(typeof(Dictionary<string, CardProgress>))]
[JsonSerializable(typeof(VocabularyDto))]
internal sealed partial class CoreJsonContext : JsonSerializerContext;

internal sealed record VocabularyDto(List<LessonDto>? Lessons);

internal sealed record LessonDto(string? Id, int? Number, string? Title, bool Essential, List<WordDto>? Words);

internal sealed record WordDto(
    string? Id,
    [property: JsonPropertyName("ar")] string? Ar,
    [property: JsonPropertyName("de")] string? De,
    [property: JsonPropertyName("tr")] string? Tr,
    string? Note);

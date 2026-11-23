using AndalusKarten.Core.Models;

namespace AndalusKarten.Core.Services;

public interface IProgressStore
{
    Task<Dictionary<string, CardProgress>> LoadAsync(CancellationToken ct = default);
    Task SaveAsync(IReadOnlyDictionary<string, CardProgress> progress, CancellationToken ct = default);
}

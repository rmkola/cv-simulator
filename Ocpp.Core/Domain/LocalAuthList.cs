using Ocpp.Core.Types;

namespace Ocpp.Core.Domain;

/// <summary>
/// The Charge Point's Local Authorization List (spec 3.4, SendLocalList / GetLocalListVersion).
/// Supports full and differential updates.
/// </summary>
public sealed class LocalAuthList
{
    private readonly Dictionary<string, IdTagInfo> _entries = new(StringComparer.Ordinal);

    /// <summary>Current version number of the list. -1 means the list is not supported/initialized.</summary>
    public int Version { get; private set; }

    public IReadOnlyDictionary<string, IdTagInfo> Entries => _entries;

    public bool TryGet(string idTag, out IdTagInfo info) => _entries.TryGetValue(idTag, out info!);

    /// <summary>
    /// Applies a SendLocalList update. Returns the resulting status.
    /// Full: replaces the whole list. Differential: entries with idTagInfo are upserted, entries
    /// without idTagInfo are removed.
    /// </summary>
    public UpdateStatus Apply(int listVersion, UpdateType updateType, IReadOnlyList<AuthorizationData>? list)
    {
        if (updateType == UpdateType.Full)
        {
            _entries.Clear();
            if (list is not null)
                foreach (var e in list)
                    if (e.IdTagInfo is not null)
                        _entries[e.IdTag] = e.IdTagInfo;
            Version = listVersion;
            return UpdateStatus.Accepted;
        }

        // Differential
        if (listVersion <= Version)
            return UpdateStatus.VersionMismatch;

        if (list is not null)
        {
            foreach (var e in list)
            {
                if (e.IdTagInfo is null) _entries.Remove(e.IdTag);
                else _entries[e.IdTag] = e.IdTagInfo;
            }
        }
        Version = listVersion;
        return UpdateStatus.Accepted;
    }
}

using CPElite.Domain.Entities;

namespace CPElite.Application.Abstractions;

public interface IEaSyncRepository
{
    Task<EaClubSnapshot?> GetClubSnapshotAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<EaMemberStatsSnapshot?> GetMemberStatsSnapshotAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<EaMatchSnapshot?> GetMatchSnapshotAsync(Guid teamId, string matchType, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<EaPlayerProfileSnapshot>> GetPlayerProfilesAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<EaFriendlyMatch>> GetFriendlyMatchesAsync(Guid teamId, int take = 20, CancellationToken cancellationToken = default, Guid? tournamentMatchId = null);
    Task<IReadOnlyCollection<EaFriendlyMatch>> GetFriendlyMatchesForLookupAsync(Guid teamId, DateTimeOffset from, DateTimeOffset until, long homeEaClubId, long awayEaClubId, CancellationToken cancellationToken = default);
    Task<EaFriendlyMatch?> GetFriendlyMatchAsync(Guid teamId, string eaMatchId, CancellationToken cancellationToken = default);
    Task UpsertClubSnapshotAsync(EaClubSnapshot snapshot, CancellationToken cancellationToken = default);
    Task UpsertMemberStatsSnapshotAsync(EaMemberStatsSnapshot snapshot, CancellationToken cancellationToken = default);
    Task UpsertMatchSnapshotAsync(EaMatchSnapshot snapshot, CancellationToken cancellationToken = default);
    Task ReplacePlayerProfilesAsync(Guid teamId, IReadOnlyCollection<EaPlayerProfileSnapshot> profiles, CancellationToken cancellationToken = default);
    Task UpsertFriendlyMatchesAsync(Guid teamId, IReadOnlyCollection<EaFriendlyMatch> matches, IReadOnlyCollection<EaMatchPlayerStat> playerStats, IReadOnlyCollection<EaMatchClubStat> clubStats, CancellationToken cancellationToken = default);
    Task LinkFriendlyMatchToTournamentMatchAsync(string eaMatchId, Guid tournamentMatchId, CancellationToken cancellationToken = default);
}

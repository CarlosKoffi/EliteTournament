using CPElite.Application;
using CPElite.Application.Abstractions;
using CPElite.Application.Services;
using CPElite.Contracts.Auth;
using ContractPlatform = CPElite.Contracts.Common.Platform;
using CPElite.Domain.Entities;
using DomainPlatform = CPElite.Domain.Enums.Platform;

namespace CPElite.Tests.Unit.Application;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task Register_rejects_invalid_email()
    {
        var service = CreateService();
        var request = new RegisterPlayerRequest("bad-email", "Password123", "Carlos", "Carlos10", "EA123", ContractPlatform.CrossPlay, "en", "UTC");

        var result = await service.RegisterAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
    }

    [Fact]
    public async Task Register_rejects_duplicate_email()
    {
        var users = new FakeUserRepository();
        await users.AddAsync(new User(Guid.NewGuid(), "a@test.com", "A@TEST.COM", "hash", "A", null, null, null, DomainPlatform.Pc, "en", "UTC", DateTimeOffset.UtcNow));
        var service = CreateService(users);
        var request = new RegisterPlayerRequest("a@test.com", "Password123", "Carlos", "Carlos10", "EA123", ContractPlatform.CrossPlay, "en", "UTC");

        var result = await service.RegisterAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
    }

    [Fact]
    public async Task Register_attaches_player_to_selected_ea_club()
    {
        var users = new FakeUserRepository();
        var teams = new FakeTeamRepository();
        var existingTeam = new Team(Guid.NewGuid(), "TheSurvivors", "THESURVIVORS", null, DomainPlatform.CrossPlay, null, "INVITE1234", Guid.NewGuid(), DateTimeOffset.UtcNow);
        existingTeam.UpdateProfile(existingTeam.Name, existingTeam.NormalizedName, existingTeam.ShortName, existingTeam.Platform, existingTeam.Region, null, 2148207, null, null);
        await teams.AddAsync(existingTeam);
        var service = CreateService(users, teams);

        var result = await service.RegisterAsync(new RegisterPlayerRequest(
            "koffi@test.com",
            "Password123",
            "KoffiMarvelous",
            "KoffiMarvelous",
            "KoffiMarvelous",
            ContractPlatform.CrossPlay,
            "fr",
            "Europe/Zurich",
            EaClubId: 2148207,
            EaClubName: "TheSurvivors"));

        Assert.True(result.IsSuccess);
        var membership = Assert.Single(teams.AddedMembers);
        Assert.Equal(existingTeam.Id, membership.TeamId);
        Assert.Equal(result.Value!.User.Id, membership.UserId);
    }

    private static AuthService CreateService(FakeUserRepository? users = null, FakeTeamRepository? teams = null)
    {
        return new AuthService(
            users ?? new FakeUserRepository(),
            teams ?? new FakeTeamRepository(),
            new FakePasswordHasher(),
            new FakeTokenService(),
            new FakeClock(),
            new FakeUnitOfWork());
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly List<User> _users = [];

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_users.FirstOrDefault(user => user.Id == id));
        }

        public Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_users.FirstOrDefault(user => user.NormalizedEmail == normalizedEmail));
        }

        public Task<User?> GetByEaIdentityAsync(string eaPlayerId, string playerName, string? proName = null, CancellationToken cancellationToken = default)
        {
            var candidates = new[] { eaPlayerId, playerName, proName }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!.Trim())
                .ToArray();

            return Task.FromResult(_users.FirstOrDefault(user =>
                candidates.Contains(user.EaSportsId, StringComparer.OrdinalIgnoreCase) ||
                candidates.Contains(user.Gamertag, StringComparer.OrdinalIgnoreCase) ||
                candidates.Contains(user.DisplayName, StringComparer.OrdinalIgnoreCase)));
        }

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            _users.Add(user);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTeamRepository : ITeamRepository
    {
        private readonly List<Team> _teams = [];
        public List<TeamMember> AddedMembers { get; } = [];

        public Task<Team?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Team?>(null);
        public Task<Team?> GetByInviteCodeAsync(string inviteCode, CancellationToken cancellationToken = default) => Task.FromResult<Team?>(null);
        public Task<Team?> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default) => Task.FromResult(_teams.FirstOrDefault(team => team.NormalizedName == normalizedName));
        public Task<Team?> GetByEaClubIdAsync(long eaClubId, CancellationToken cancellationToken = default) => Task.FromResult(_teams.FirstOrDefault(team => team.EaClubId == eaClubId));
        public Task<IReadOnlyCollection<Team>> SearchByNameAsync(string normalizedSearch, int take = 10, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Team>>(Array.Empty<Team>());
        public Task<IReadOnlyCollection<Team>> GetTeamsLinkedToEaAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Team>>(Array.Empty<Team>());
        public Task<TeamMember?> GetMembershipAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<TeamMember?>(null);
        public Task<TeamMember?> GetActiveMembershipForUserAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<TeamMember?>(null);
        public Task<IReadOnlyCollection<TeamMember>> GetMembershipsForUserAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<TeamMember>>(Array.Empty<TeamMember>());
        public Task<IReadOnlyCollection<TeamMember>> GetTeamMembersAsync(Guid teamId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<TeamMember>>(Array.Empty<TeamMember>());
        public Task<TeamJoinRequest?> GetPendingJoinRequestAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<TeamJoinRequest?>(null);
        public Task<TeamJoinRequest?> GetJoinRequestAsync(Guid requestId, CancellationToken cancellationToken = default) => Task.FromResult<TeamJoinRequest?>(null);
        public Task<IReadOnlyCollection<TeamJoinRequest>> GetPendingJoinRequestsAsync(Guid teamId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<TeamJoinRequest>>(Array.Empty<TeamJoinRequest>());
        public Task<IReadOnlyCollection<TeamPosition>> GetPositionsAsync(Guid teamId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<TeamPosition>>(Array.Empty<TeamPosition>());
        public Task<TeamPosition?> GetPositionAsync(Guid teamId, Guid positionId, CancellationToken cancellationToken = default) => Task.FromResult<TeamPosition?>(null);
        public Task<IReadOnlyCollection<TeamScheduleSlot>> GetScheduleSlotsAsync(Guid teamId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<TeamScheduleSlot>>(Array.Empty<TeamScheduleSlot>());
        public Task<TeamScheduleSlot?> GetScheduleSlotAsync(Guid teamId, Guid slotId, CancellationToken cancellationToken = default) => Task.FromResult<TeamScheduleSlot?>(null);
        public Task<TeamPlayerDemand?> GetPlayerDemandAsync(Guid teamId, Guid demandId, CancellationToken cancellationToken = default) => Task.FromResult<TeamPlayerDemand?>(null);
        public Task<IReadOnlyCollection<TeamPlayerDemand>> GetActivePlayerDemandsAsync(DateTimeOffset startInclusive, DateTimeOffset endExclusive, DateTimeOffset now, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<TeamPlayerDemand>>(Array.Empty<TeamPlayerDemand>());
        public Task AddAsync(Team team, CancellationToken cancellationToken = default)
        {
            _teams.Add(team);
            return Task.CompletedTask;
        }

        public Task AddMemberAsync(TeamMember membership, CancellationToken cancellationToken = default)
        {
            AddedMembers.Add(membership);
            return Task.CompletedTask;
        }
        public Task AddJoinRequestAsync(TeamJoinRequest joinRequest, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddPositionAsync(TeamPosition position, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddScheduleSlotAsync(TeamScheduleSlot slot, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddPlayerDemandAsync(TeamPlayerDemand demand, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void RemovePosition(TeamPosition position) { }
        public void RemoveScheduleSlot(TeamScheduleSlot slot) { }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hash:{password}";
        public bool Verify(string password, string passwordHash) => passwordHash == Hash(password);
    }

    private sealed class FakeTokenService : ITokenService
    {
        public AuthToken Create(User user) => new("token", DateTimeOffset.UtcNow.AddHours(1));
    }

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 6, 26, 9, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}

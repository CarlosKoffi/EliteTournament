using CPElite.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CPElite.Infrastructure.Data;

public sealed class CPEliteDbContext : DbContext
{
    public CPEliteDbContext(DbContextOptions<CPEliteDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<TeamJoinRequest> TeamJoinRequests => Set<TeamJoinRequest>();
    public DbSet<TeamPosition> TeamPositions => Set<TeamPosition>();
    public DbSet<TeamScheduleSlot> TeamScheduleSlots => Set<TeamScheduleSlot>();
    public DbSet<TeamPlayerDemand> TeamPlayerDemands => Set<TeamPlayerDemand>();
    public DbSet<EaApiCacheEntry> EaApiCacheEntries => Set<EaApiCacheEntry>();
    public DbSet<EaClubSnapshot> EaClubSnapshots => Set<EaClubSnapshot>();
    public DbSet<EaMemberStatsSnapshot> EaMemberStatsSnapshots => Set<EaMemberStatsSnapshot>();
    public DbSet<EaMatchSnapshot> EaMatchSnapshots => Set<EaMatchSnapshot>();
    public DbSet<EaPlayerProfileSnapshot> EaPlayerProfileSnapshots => Set<EaPlayerProfileSnapshot>();
    public DbSet<EaFriendlyMatch> EaFriendlyMatches => Set<EaFriendlyMatch>();
    public DbSet<EaMatchPlayerStat> EaMatchPlayerStats => Set<EaMatchPlayerStat>();
    public DbSet<EaMatchClubStat> EaMatchClubStats => Set<EaMatchClubStat>();
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<TournamentRegistration> TournamentRegistrations => Set<TournamentRegistration>();
    public DbSet<TournamentMatch> TournamentMatches => Set<TournamentMatch>();
    public DbSet<MatchScoreSubmission> MatchScoreSubmissions => Set<MatchScoreSubmission>();
    public DbSet<ChampionTitle> ChampionTitles => Set<ChampionTitle>();
    public DbSet<TournamentMoment> TournamentMoments => Set<TournamentMoment>();
    public DbSet<EaDiagnosticProbe> EaDiagnosticProbes => Set<EaDiagnosticProbe>();
    public DbSet<UserTournamentAccess> UserTournamentAccesses => Set<UserTournamentAccess>();
    public DbSet<TeamSlotPackage> TeamSlotPackages => Set<TeamSlotPackage>();
    public DbSet<TeamSlotAssignment> TeamSlotAssignments => Set<TeamSlotAssignment>();
    public DbSet<TournamentPlayerConfirmation> TournamentPlayerConfirmations => Set<TournamentPlayerConfirmation>();
    public DbSet<LocalizedContent> LocalizedContents => Set<LocalizedContent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.HasIndex(user => user.NormalizedEmail).IsUnique();
            entity.Property(user => user.Email).HasMaxLength(320).IsRequired();
            entity.Property(user => user.NormalizedEmail).HasMaxLength(320).IsRequired();
            entity.Property(user => user.PasswordHash).IsRequired();
            entity.Property(user => user.DisplayName).HasMaxLength(80).IsRequired();
            entity.Property(user => user.Gamertag).HasMaxLength(80);
            entity.Property(user => user.EaSportsId).HasMaxLength(80);
            entity.Property(user => user.DiscordUserId).HasMaxLength(80);
            entity.Property(user => user.PreferredLanguage).HasMaxLength(10).IsRequired();
            entity.Property(user => user.TimeZone).HasMaxLength(80).IsRequired();
            entity.Property(user => user.ProfileImageUrl).HasMaxLength(1000);
            entity.Property(user => user.EaClubName).HasMaxLength(120);
            entity.Property(user => user.IsAdmin).HasDefaultValue(false);
            entity.HasMany(user => user.Memberships)
                .WithOne(member => member.User)
                .HasForeignKey(member => member.UserId);
            entity.Navigation(user => user.Memberships).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(team => team.Id);
            entity.HasIndex(team => team.NormalizedName).IsUnique();
            entity.HasIndex(team => team.InviteCode).IsUnique();
            entity.Property(team => team.Name).HasMaxLength(120).IsRequired();
            entity.Property(team => team.NormalizedName).HasMaxLength(120).IsRequired();
            entity.Property(team => team.ShortName).HasMaxLength(16);
            entity.Property(team => team.Region).HasMaxLength(64);
            entity.Property(team => team.Description).HasMaxLength(500);
            entity.Property(team => team.LogoUrl).HasMaxLength(500);
            entity.Property(team => team.BannerUrl).HasMaxLength(500);
            entity.Property(team => team.DiscordUrl).HasMaxLength(300);
            entity.Property(team => team.TwitchUrl).HasMaxLength(300);
            entity.Property(team => team.TikTokUrl).HasMaxLength(300);
            entity.Property(team => team.TwitterUrl).HasMaxLength(300);
            entity.Property(team => team.InviteCode).HasMaxLength(24).IsRequired();
            entity.HasMany(team => team.Members)
                .WithOne(member => member.Team)
                .HasForeignKey(member => member.TeamId);
            entity.HasMany(team => team.JoinRequests)
                .WithOne(joinRequest => joinRequest.Team)
                .HasForeignKey(joinRequest => joinRequest.TeamId);
            entity.HasMany(team => team.Positions)
                .WithOne(position => position.Team)
                .HasForeignKey(position => position.TeamId);
            entity.HasMany(team => team.ScheduleSlots)
                .WithOne(slot => slot.Team)
                .HasForeignKey(slot => slot.TeamId);
            entity.HasMany(team => team.PlayerDemands)
                .WithOne(demand => demand.Team)
                .HasForeignKey(demand => demand.TeamId);
            entity.Navigation(team => team.Members).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Navigation(team => team.JoinRequests).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Navigation(team => team.Positions).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Navigation(team => team.ScheduleSlots).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Navigation(team => team.PlayerDemands).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<TeamMember>(entity =>
        {
            entity.HasKey(member => member.Id);
            entity.HasIndex(member => new { member.TeamId, member.UserId }).IsUnique();
        });

        modelBuilder.Entity<UserTournamentAccess>(entity =>
        {
            entity.HasKey(access => access.Id);
            entity.HasIndex(access => access.UserId).IsUnique();
            entity.Property(access => access.ProviderTransactionId).HasMaxLength(160).IsRequired();
            entity.HasOne(access => access.User)
                .WithMany()
                .HasForeignKey(access => access.UserId);
        });

        modelBuilder.Entity<TeamSlotPackage>(entity =>
        {
            entity.HasKey(package => package.Id);
            entity.HasIndex(package => package.TeamId);
            entity.Property(package => package.Currency).HasMaxLength(3).IsRequired();
            entity.Property(package => package.ProviderTransactionId).HasMaxLength(160).IsRequired();
            entity.HasOne(package => package.Team)
                .WithMany()
                .HasForeignKey(package => package.TeamId);
            entity.HasOne(package => package.PurchasedByUser)
                .WithMany()
                .HasForeignKey(package => package.PurchasedByUserId);
        });

        modelBuilder.Entity<TeamSlotAssignment>(entity =>
        {
            entity.HasKey(assignment => assignment.Id);
            entity.HasIndex(assignment => new { assignment.TeamId, assignment.UserId, assignment.ReleasedAt });
            entity.HasOne(assignment => assignment.Package)
                .WithMany()
                .HasForeignKey(assignment => assignment.TeamSlotPackageId);
            entity.HasOne(assignment => assignment.Team)
                .WithMany()
                .HasForeignKey(assignment => assignment.TeamId);
            entity.HasOne(assignment => assignment.User)
                .WithMany()
                .HasForeignKey(assignment => assignment.UserId);
        });

        modelBuilder.Entity<TeamJoinRequest>(entity =>
        {
            entity.HasKey(joinRequest => joinRequest.Id);
            entity.HasIndex(joinRequest => new { joinRequest.TeamId, joinRequest.UserId, joinRequest.Status });
            entity.Property(joinRequest => joinRequest.Message).HasMaxLength(500);
            entity.HasOne(joinRequest => joinRequest.User)
                .WithMany()
                .HasForeignKey(joinRequest => joinRequest.UserId);
        });

        modelBuilder.Entity<TeamPosition>(entity =>
        {
            entity.HasKey(position => position.Id);
            entity.HasIndex(position => new { position.TeamId, position.Name }).IsUnique();
            entity.Property(position => position.Name).HasMaxLength(80).IsRequired();
            entity.Property(position => position.Description).HasMaxLength(300);
        });

        modelBuilder.Entity<TeamScheduleSlot>(entity =>
        {
            entity.HasKey(slot => slot.Id);
            entity.HasIndex(slot => new { slot.TeamId, slot.DayOfWeek, slot.StartTime });
            entity.Property(slot => slot.Label).HasMaxLength(120);
        });

        modelBuilder.Entity<TeamPlayerDemand>(entity =>
        {
            entity.HasKey(demand => demand.Id);
            entity.HasIndex(demand => new { demand.Status, demand.NeededAt, demand.ExpiresAt });
            entity.HasIndex(demand => new { demand.TeamId, demand.Status });
            entity.Property(demand => demand.Position).HasMaxLength(80).IsRequired();
            entity.Property(demand => demand.Note).HasMaxLength(500);
            entity.HasOne(demand => demand.CreatedByUser)
                .WithMany()
                .HasForeignKey(demand => demand.CreatedByUserId);
        });

        modelBuilder.Entity<EaApiCacheEntry>(entity =>
        {
            entity.HasKey(entry => entry.Id);
            entity.HasIndex(entry => entry.CacheKey).IsUnique();
            entity.HasIndex(entry => entry.ExpiresAt);
            entity.Property(entry => entry.CacheKey).HasMaxLength(200).IsRequired();
            entity.Property(entry => entry.Endpoint).HasMaxLength(500).IsRequired();
            entity.Property(entry => entry.RawJson).HasColumnType("jsonb");
        });

        modelBuilder.Entity<EaClubSnapshot>(entity =>
        {
            entity.HasKey(snapshot => snapshot.Id);
            entity.HasIndex(snapshot => snapshot.TeamId).IsUnique();
            entity.Property(snapshot => snapshot.Platform).HasMaxLength(40).IsRequired();
            entity.Property(snapshot => snapshot.Name).HasMaxLength(120);
            entity.Property(snapshot => snapshot.Abbreviation).HasMaxLength(20);
            entity.Property(snapshot => snapshot.RawJson).HasColumnType("jsonb");
            entity.HasOne(snapshot => snapshot.Team)
                .WithMany()
                .HasForeignKey(snapshot => snapshot.TeamId);
        });

        modelBuilder.Entity<EaMemberStatsSnapshot>(entity =>
        {
            entity.HasKey(snapshot => snapshot.Id);
            entity.HasIndex(snapshot => snapshot.TeamId).IsUnique();
            entity.Property(snapshot => snapshot.Platform).HasMaxLength(40).IsRequired();
            entity.Property(snapshot => snapshot.RawJson).HasColumnType("jsonb");
            entity.HasOne(snapshot => snapshot.Team)
                .WithMany()
                .HasForeignKey(snapshot => snapshot.TeamId);
        });

        modelBuilder.Entity<EaMatchSnapshot>(entity =>
        {
            entity.HasKey(snapshot => snapshot.Id);
            entity.HasIndex(snapshot => new { snapshot.TeamId, snapshot.MatchType }).IsUnique();
            entity.Property(snapshot => snapshot.Platform).HasMaxLength(40).IsRequired();
            entity.Property(snapshot => snapshot.MatchType).HasMaxLength(80).IsRequired();
            entity.Property(snapshot => snapshot.RawJson).HasColumnType("jsonb");
            entity.HasOne(snapshot => snapshot.Team)
                .WithMany()
                .HasForeignKey(snapshot => snapshot.TeamId);
        });

        modelBuilder.Entity<EaPlayerProfileSnapshot>(entity =>
        {
            entity.HasKey(snapshot => snapshot.Id);
            entity.HasIndex(snapshot => new { snapshot.TeamId, snapshot.EaPlayerId }).IsUnique();
            entity.HasIndex(snapshot => snapshot.PlayerName);
            entity.Property(snapshot => snapshot.Platform).HasMaxLength(40).IsRequired();
            entity.Property(snapshot => snapshot.EaPlayerId).HasMaxLength(80).IsRequired();
            entity.Property(snapshot => snapshot.PlayerName).HasMaxLength(120).IsRequired();
            entity.Property(snapshot => snapshot.ProName).HasMaxLength(120);
            entity.Property(snapshot => snapshot.Position).HasMaxLength(40);
            entity.Property(snapshot => snapshot.RawJson).HasColumnType("jsonb");
            entity.HasOne(snapshot => snapshot.Team)
                .WithMany()
                .HasForeignKey(snapshot => snapshot.TeamId);
        });

        modelBuilder.Entity<EaFriendlyMatch>(entity =>
        {
            entity.HasKey(match => match.Id);
            entity.HasIndex(match => new { match.TeamId, match.EaMatchId }).IsUnique();
            entity.HasIndex(match => new { match.TeamId, match.PlayedAt });
            entity.HasIndex(match => match.TournamentMatchId);
            entity.Property(match => match.Platform).HasMaxLength(40).IsRequired();
            entity.Property(match => match.EaMatchId).HasMaxLength(80).IsRequired();
            entity.Property(match => match.MatchType).HasMaxLength(40).IsRequired();
            entity.Property(match => match.HomeClubName).HasMaxLength(120);
            entity.Property(match => match.AwayClubName).HasMaxLength(120);
            entity.Property(match => match.RawJson).HasColumnType("jsonb");
            entity.HasOne(match => match.Team)
                .WithMany()
                .HasForeignKey(match => match.TeamId);
            entity.Navigation(match => match.PlayerStats).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Navigation(match => match.ClubStats).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<EaMatchClubStat>(entity =>
        {
            entity.HasKey(stat => stat.Id);
            entity.HasIndex(stat => new { stat.EaFriendlyMatchId, stat.EaClubId }).IsUnique();
            entity.Property(stat => stat.RawJson).HasColumnType("jsonb");
            entity.HasOne(stat => stat.Match)
                .WithMany(match => match.ClubStats)
                .HasForeignKey(stat => stat.EaFriendlyMatchId);
        });

        modelBuilder.Entity<EaMatchPlayerStat>(entity =>
        {
            entity.HasKey(stat => stat.Id);
            entity.HasIndex(stat => new { stat.EaFriendlyMatchId, stat.EaPlayerId, stat.EaClubId }).IsUnique();
            entity.HasIndex(stat => new { stat.TeamId, stat.PlayerName });
            entity.Property(stat => stat.EaPlayerId).HasMaxLength(80).IsRequired();
            entity.Property(stat => stat.PlayerName).HasMaxLength(120).IsRequired();
            entity.Property(stat => stat.Position).HasMaxLength(40);
            entity.Property(stat => stat.VproAttributes).HasMaxLength(300);
            entity.Property(stat => stat.MatchEventAggregate0).HasMaxLength(1000);
            entity.Property(stat => stat.MatchEventAggregate1).HasMaxLength(1000);
            entity.Property(stat => stat.MatchEventAggregate2).HasMaxLength(1000);
            entity.Property(stat => stat.MatchEventAggregate3).HasMaxLength(1000);
            entity.Property(stat => stat.RawJson).HasColumnType("jsonb");
            entity.HasOne(stat => stat.Match)
                .WithMany(match => match.PlayerStats)
                .HasForeignKey(stat => stat.EaFriendlyMatchId);
        });

        modelBuilder.Entity<Tournament>(entity =>
        {
            entity.HasKey(tournament => tournament.Id);
            entity.Property(tournament => tournament.Name).HasMaxLength(160).IsRequired();
            entity.Property(tournament => tournament.TimeZone).HasMaxLength(80).IsRequired();
            entity.Property(tournament => tournament.Currency).HasMaxLength(3).IsRequired();
            entity.Property(tournament => tournament.GoodiesDescription).HasMaxLength(1000);
            entity.Property(tournament => tournament.BannerUrl).HasMaxLength(1000);
            entity.Property(tournament => tournament.PlayerRestrictionsJson).HasColumnType("jsonb");
        });

        modelBuilder.Entity<TournamentRegistration>(entity =>
        {
            entity.HasKey(registration => registration.Id);
            entity.HasIndex(registration => new { registration.TournamentId, registration.TeamId }).IsUnique();
            entity.Property(registration => registration.Source).HasMaxLength(40).IsRequired();
            entity.Property(registration => registration.DiscordGuildId).HasMaxLength(80);
            entity.Property(registration => registration.DiscordChannelId).HasMaxLength(80);
            entity.Property(registration => registration.DiscordMessageId).HasMaxLength(80);
            entity.Property(registration => registration.DiscordRequestedByUserId).HasMaxLength(80);
            entity.HasOne(registration => registration.Tournament)
                .WithMany()
                .HasForeignKey(registration => registration.TournamentId);
            entity.HasOne(registration => registration.Team)
                .WithMany()
                .HasForeignKey(registration => registration.TeamId);
        });

        modelBuilder.Entity<TournamentMatch>(entity =>
        {
            entity.HasKey(match => match.Id);
            entity.HasIndex(match => new { match.TournamentId, match.RoundNumber });
            entity.HasIndex(match => new { match.TournamentId, match.Stage, match.GroupName });
            entity.Property(match => match.GroupName).HasMaxLength(8);
            entity.Property(match => match.EaRawMatchJson).HasColumnType("jsonb");
            entity.HasOne(match => match.Tournament)
                .WithMany()
                .HasForeignKey(match => match.TournamentId);
        });

        modelBuilder.Entity<MatchScoreSubmission>(entity =>
        {
            entity.HasKey(submission => submission.Id);
            entity.Property(submission => submission.ProofUrl).HasMaxLength(500);
            entity.HasOne(submission => submission.Match)
                .WithMany()
                .HasForeignKey(submission => submission.MatchId);
        });

        modelBuilder.Entity<ChampionTitle>(entity =>
        {
            entity.HasKey(champion => champion.Id);
            entity.HasIndex(champion => new { champion.TeamId, champion.IsActive });
            entity.Property(champion => champion.Currency).HasMaxLength(3).IsRequired();
            entity.HasOne(champion => champion.Team)
                .WithMany()
                .HasForeignKey(champion => champion.TeamId);
        });

        modelBuilder.Entity<TournamentMoment>(entity =>
        {
            entity.HasKey(moment => moment.Id);
            entity.HasIndex(moment => new { moment.IsPublishedToDiscord, moment.CreatedAt });
            entity.HasIndex(moment => new { moment.TournamentId, moment.MatchId });
            entity.Property(moment => moment.DiscordUserId).HasMaxLength(80);
            entity.Property(moment => moment.Title).HasMaxLength(160).IsRequired();
            entity.Property(moment => moment.Message).HasMaxLength(1000).IsRequired();
            entity.Property(moment => moment.PayloadJson).HasColumnType("jsonb");
        });

        modelBuilder.Entity<TournamentPlayerConfirmation>(entity =>
        {
            entity.HasKey(confirmation => confirmation.Id);
            entity.HasIndex(confirmation => new { confirmation.TournamentId, confirmation.TeamId, confirmation.UserId }).IsUnique();
            entity.HasIndex(confirmation => new { confirmation.TournamentId, confirmation.TeamId, confirmation.Status });
            entity.HasIndex(confirmation => new { confirmation.IsLoan, confirmation.LoanFromTeamId });
            entity.Property(confirmation => confirmation.Position).HasMaxLength(80).IsRequired();
            entity.Property(confirmation => confirmation.Note).HasMaxLength(500);
            entity.HasOne(confirmation => confirmation.Tournament)
                .WithMany()
                .HasForeignKey(confirmation => confirmation.TournamentId);
            entity.HasOne(confirmation => confirmation.Team)
                .WithMany()
                .HasForeignKey(confirmation => confirmation.TeamId);
            entity.HasOne(confirmation => confirmation.User)
                .WithMany()
                .HasForeignKey(confirmation => confirmation.UserId);
        });

        modelBuilder.Entity<EaDiagnosticProbe>(entity =>
        {
            entity.HasKey(probe => probe.Id);
            entity.HasIndex(probe => probe.CreatedAt);
            entity.Property(probe => probe.StepName).HasMaxLength(80).IsRequired();
            entity.Property(probe => probe.Endpoint).HasMaxLength(500).IsRequired();
            entity.Property(probe => probe.Platform).HasMaxLength(40).IsRequired();
            entity.Property(probe => probe.ClubName).HasMaxLength(160);
            entity.Property(probe => probe.Error).HasMaxLength(1000);
            entity.Property(probe => probe.RawPreview).HasMaxLength(4000);
        });

        modelBuilder.Entity<LocalizedContent>(entity =>
        {
            entity.HasKey(content => content.Id);
            entity.HasIndex(content => new { content.Key, content.Language }).IsUnique();
            entity.HasIndex(content => new { content.Language, content.Section });
            entity.Property(content => content.Key).HasMaxLength(160).IsRequired();
            entity.Property(content => content.Language).HasMaxLength(10).IsRequired();
            entity.Property(content => content.Value).HasMaxLength(4000).IsRequired();
            entity.Property(content => content.Section).HasMaxLength(120);
            entity.Property(content => content.Description).HasMaxLength(500);
        });
    }
}

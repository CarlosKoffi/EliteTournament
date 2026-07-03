using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CPElite.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EaApiCacheEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CacheKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Endpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RawJson = table.Column<string>(type: "jsonb", nullable: false),
                    FetchedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EaApiCacheEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EaDiagnosticProbes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StepName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Endpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Platform = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ClubName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    ClubId = table.Column<long>(type: "bigint", nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    StatusCode = table.Column<int>(type: "integer", nullable: true),
                    Error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RawPreview = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    RawLength = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EaDiagnosticProbes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ShortName = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    Platform = table.Column<int>(type: "integer", nullable: false),
                    Region = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    InviteCode = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    InviteCodeExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EaClubId = table.Column<long>(type: "bigint", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BannerUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DiscordUrl = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    TwitchUrl = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    TikTokUrl = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    TwitterUrl = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    RequireJoinApproval = table.Column<bool>(type: "boolean", nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TournamentMoments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DiscordUserId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: true),
                    IsPublishedToDiscord = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublishedToDiscordAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentMoments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tournaments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TimeZone = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    MinTeams = table.Column<int>(type: "integer", nullable: false),
                    MaxTeams = table.Column<int>(type: "integer", nullable: false),
                    EntryFee = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    GoodiesDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RegistrationLockAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RegistrationStartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RegistrationEndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EstimatedPrizeBudget = table.Column<decimal>(type: "numeric", nullable: true),
                    IsCashPrize = table.Column<bool>(type: "boolean", nullable: false),
                    EaMonitoringStartsMinutesBefore = table.Column<int>(type: "integer", nullable: false),
                    EaMonitoringEndsMinutesAfter = table.Column<int>(type: "integer", nullable: false),
                    PlayerRestrictionsJson = table.Column<string>(type: "jsonb", nullable: true),
                    BannerUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tournaments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Gamertag = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    EaSportsId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    DiscordUserId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Platform = table.Column<int>(type: "integer", nullable: false),
                    PreferredLanguage = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TimeZone = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ProfileImageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EaClubId = table.Column<long>(type: "bigint", nullable: true),
                    EaClubName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChampionTitles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CrownedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DethronedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    NextEntryDiscountAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChampionTitles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChampionTitles_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EaClubSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    EaClubId = table.Column<long>(type: "bigint", nullable: false),
                    Platform = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Abbreviation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Division = table.Column<int>(type: "integer", nullable: true),
                    MembersCount = table.Column<int>(type: "integer", nullable: true),
                    RawJson = table.Column<string>(type: "jsonb", nullable: false),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EaClubSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EaClubSnapshots_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EaFriendlyMatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    EaClubId = table.Column<long>(type: "bigint", nullable: false),
                    Platform = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    EaMatchId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PlayedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MatchType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    HomeEaClubId = table.Column<long>(type: "bigint", nullable: false),
                    HomeClubName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    HomeScore = table.Column<int>(type: "integer", nullable: false),
                    AwayEaClubId = table.Column<long>(type: "bigint", nullable: false),
                    AwayClubName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    AwayScore = table.Column<int>(type: "integer", nullable: false),
                    RawJson = table.Column<string>(type: "jsonb", nullable: false),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TournamentMatchId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EaFriendlyMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EaFriendlyMatches_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EaMatchSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    EaClubId = table.Column<long>(type: "bigint", nullable: false),
                    Platform = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    MatchType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    RawJson = table.Column<string>(type: "jsonb", nullable: false),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EaMatchSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EaMatchSnapshots_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EaMemberStatsSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    EaClubId = table.Column<long>(type: "bigint", nullable: false),
                    Platform = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RawJson = table.Column<string>(type: "jsonb", nullable: false),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EaMemberStatsSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EaMemberStatsSnapshots_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EaPlayerProfileSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    EaClubId = table.Column<long>(type: "bigint", nullable: false),
                    Platform = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    EaPlayerId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PlayerName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ProName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Position = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Matches = table.Column<int>(type: "integer", nullable: true),
                    WinRate = table.Column<int>(type: "integer", nullable: true),
                    Goals = table.Column<int>(type: "integer", nullable: true),
                    Assists = table.Column<int>(type: "integer", nullable: true),
                    AverageRating = table.Column<double>(type: "double precision", nullable: true),
                    Height = table.Column<int>(type: "integer", nullable: true),
                    Weight = table.Column<int>(type: "integer", nullable: true),
                    Overall = table.Column<int>(type: "integer", nullable: true),
                    Shots = table.Column<int>(type: "integer", nullable: true),
                    ShotSuccessRate = table.Column<int>(type: "integer", nullable: true),
                    PassesMade = table.Column<int>(type: "integer", nullable: true),
                    PassAttempts = table.Column<int>(type: "integer", nullable: true),
                    StoredPassSuccessRate = table.Column<double>(type: "double precision", nullable: true),
                    TacklesMade = table.Column<int>(type: "integer", nullable: true),
                    TackleAttempts = table.Column<int>(type: "integer", nullable: true),
                    StoredTackleSuccessRate = table.Column<double>(type: "double precision", nullable: true),
                    Saves = table.Column<int>(type: "integer", nullable: true),
                    CleanSheets = table.Column<int>(type: "integer", nullable: true),
                    CleanSheetsGk = table.Column<int>(type: "integer", nullable: true),
                    PlayerOfTheMatch = table.Column<int>(type: "integer", nullable: true),
                    RedCards = table.Column<int>(type: "integer", nullable: true),
                    PrevGoals = table.Column<int>(type: "integer", nullable: true),
                    RawJson = table.Column<string>(type: "jsonb", nullable: false),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EaPlayerProfileSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EaPlayerProfileSnapshots_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamPositions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamPositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamPositions_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamScheduleSlots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamScheduleSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamScheduleSlots_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TournamentMatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uuid", nullable: false),
                    HomeTeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    AwayTeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoundNumber = table.Column<int>(type: "integer", nullable: false),
                    Stage = table.Column<int>(type: "integer", nullable: false),
                    GroupName = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    MatchNumber = table.Column<int>(type: "integer", nullable: false),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EaLookupFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EaLookupUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    HomeScore = table.Column<int>(type: "integer", nullable: true),
                    AwayScore = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    WinnerTeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    EaRawMatchJson = table.Column<string>(type: "jsonb", nullable: true),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentMatches_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TournamentRegistrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegisteredByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentMode = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IsPaymentComplete = table.Column<bool>(type: "boolean", nullable: false),
                    Source = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DiscordGuildId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    DiscordChannelId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    DiscordMessageId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    DiscordRequestedByUserId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentRegistrations_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TournamentRegistrations_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamJoinRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DecidedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamJoinRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamJoinRequests_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamJoinRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamMembers_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamPlayerDemands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    NeededAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamPlayerDemands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamPlayerDemands_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamPlayerDemands_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamSlotPackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchasedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SlotCount = table.Column<int>(type: "integer", nullable: false),
                    PricePaid = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    ProviderTransactionId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamSlotPackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamSlotPackages_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamSlotPackages_Users_PurchasedByUserId",
                        column: x => x.PurchasedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TournamentPlayerConfirmations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DelayMinutes = table.Column<int>(type: "integer", nullable: true),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsLoan = table.Column<bool>(type: "boolean", nullable: false),
                    LoanFromTeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    LoanApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LoanApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastReminderSentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RequiresOwnerNotification = table.Column<bool>(type: "boolean", nullable: false),
                    ReplacementSuggested = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentPlayerConfirmations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentPlayerConfirmations_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TournamentPlayerConfirmations_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TournamentPlayerConfirmations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTournamentAccesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    ProviderTransactionId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTournamentAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTournamentAccesses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EaMatchClubStats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EaFriendlyMatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    EaClubId = table.Column<long>(type: "bigint", nullable: false),
                    Goals = table.Column<int>(type: "integer", nullable: true),
                    Assists = table.Column<int>(type: "integer", nullable: true),
                    Rating = table.Column<double>(type: "double precision", nullable: true),
                    Shots = table.Column<int>(type: "integer", nullable: true),
                    PassesMade = table.Column<int>(type: "integer", nullable: true),
                    PassAttempts = table.Column<int>(type: "integer", nullable: true),
                    TacklesMade = table.Column<int>(type: "integer", nullable: true),
                    TackleAttempts = table.Column<int>(type: "integer", nullable: true),
                    Saves = table.Column<int>(type: "integer", nullable: true),
                    GoalsConceded = table.Column<int>(type: "integer", nullable: true),
                    RedCards = table.Column<int>(type: "integer", nullable: true),
                    PlayerOfTheMatch = table.Column<int>(type: "integer", nullable: true),
                    RawJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EaMatchClubStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EaMatchClubStats_EaFriendlyMatches_EaFriendlyMatchId",
                        column: x => x.EaFriendlyMatchId,
                        principalTable: "EaFriendlyMatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EaMatchPlayerStats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EaFriendlyMatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    EaClubId = table.Column<long>(type: "bigint", nullable: false),
                    EaPlayerId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PlayerName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Position = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Goals = table.Column<int>(type: "integer", nullable: true),
                    Assists = table.Column<int>(type: "integer", nullable: true),
                    Rating = table.Column<double>(type: "double precision", nullable: true),
                    PlayerOfTheMatch = table.Column<bool>(type: "boolean", nullable: false),
                    Shots = table.Column<int>(type: "integer", nullable: true),
                    PassesMade = table.Column<int>(type: "integer", nullable: true),
                    PassAttempts = table.Column<int>(type: "integer", nullable: true),
                    TacklesMade = table.Column<int>(type: "integer", nullable: true),
                    TackleAttempts = table.Column<int>(type: "integer", nullable: true),
                    Saves = table.Column<int>(type: "integer", nullable: true),
                    GoalsConceded = table.Column<int>(type: "integer", nullable: true),
                    SecondsPlayed = table.Column<int>(type: "integer", nullable: true),
                    VproAttributes = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CleanSheetsAny = table.Column<int>(type: "integer", nullable: true),
                    CleanSheetsDef = table.Column<int>(type: "integer", nullable: true),
                    CleanSheetsGk = table.Column<int>(type: "integer", nullable: true),
                    BallDiveSaves = table.Column<int>(type: "integer", nullable: true),
                    CrossSaves = table.Column<int>(type: "integer", nullable: true),
                    GoodDirectionSaves = table.Column<int>(type: "integer", nullable: true),
                    ParrySaves = table.Column<int>(type: "integer", nullable: true),
                    PunchSaves = table.Column<int>(type: "integer", nullable: true),
                    ReflexSaves = table.Column<int>(type: "integer", nullable: true),
                    RedCards = table.Column<int>(type: "integer", nullable: true),
                    GameTime = table.Column<int>(type: "integer", nullable: true),
                    RealtimeGame = table.Column<int>(type: "integer", nullable: true),
                    RealtimeIdle = table.Column<int>(type: "integer", nullable: true),
                    ArchetypeId = table.Column<int>(type: "integer", nullable: true),
                    MatchEventAggregate0 = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MatchEventAggregate1 = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MatchEventAggregate2 = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MatchEventAggregate3 = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RawJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EaMatchPlayerStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EaMatchPlayerStats_EaFriendlyMatches_EaFriendlyMatchId",
                        column: x => x.EaFriendlyMatchId,
                        principalTable: "EaFriendlyMatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MatchScoreSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    HomeScore = table.Column<int>(type: "integer", nullable: false),
                    AwayScore = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    ProofUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchScoreSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchScoreSubmissions_TournamentMatches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "TournamentMatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamSlotAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamSlotPackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReleasedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamSlotAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamSlotAssignments_TeamSlotPackages_TeamSlotPackageId",
                        column: x => x.TeamSlotPackageId,
                        principalTable: "TeamSlotPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamSlotAssignments_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamSlotAssignments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChampionTitles_TeamId_IsActive",
                table: "ChampionTitles",
                columns: new[] { "TeamId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_EaApiCacheEntries_CacheKey",
                table: "EaApiCacheEntries",
                column: "CacheKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EaApiCacheEntries_ExpiresAt",
                table: "EaApiCacheEntries",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_EaClubSnapshots_TeamId",
                table: "EaClubSnapshots",
                column: "TeamId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EaDiagnosticProbes_CreatedAt",
                table: "EaDiagnosticProbes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EaFriendlyMatches_TeamId_EaMatchId",
                table: "EaFriendlyMatches",
                columns: new[] { "TeamId", "EaMatchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EaFriendlyMatches_TeamId_PlayedAt",
                table: "EaFriendlyMatches",
                columns: new[] { "TeamId", "PlayedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EaFriendlyMatches_TournamentMatchId",
                table: "EaFriendlyMatches",
                column: "TournamentMatchId");

            migrationBuilder.CreateIndex(
                name: "IX_EaMatchClubStats_EaFriendlyMatchId_EaClubId",
                table: "EaMatchClubStats",
                columns: new[] { "EaFriendlyMatchId", "EaClubId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EaMatchPlayerStats_EaFriendlyMatchId_EaPlayerId_EaClubId",
                table: "EaMatchPlayerStats",
                columns: new[] { "EaFriendlyMatchId", "EaPlayerId", "EaClubId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EaMatchPlayerStats_TeamId_PlayerName",
                table: "EaMatchPlayerStats",
                columns: new[] { "TeamId", "PlayerName" });

            migrationBuilder.CreateIndex(
                name: "IX_EaMatchSnapshots_TeamId_MatchType",
                table: "EaMatchSnapshots",
                columns: new[] { "TeamId", "MatchType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EaMemberStatsSnapshots_TeamId",
                table: "EaMemberStatsSnapshots",
                column: "TeamId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EaPlayerProfileSnapshots_PlayerName",
                table: "EaPlayerProfileSnapshots",
                column: "PlayerName");

            migrationBuilder.CreateIndex(
                name: "IX_EaPlayerProfileSnapshots_TeamId_EaPlayerId",
                table: "EaPlayerProfileSnapshots",
                columns: new[] { "TeamId", "EaPlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchScoreSubmissions_MatchId",
                table: "MatchScoreSubmissions",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamJoinRequests_TeamId_UserId_Status",
                table: "TeamJoinRequests",
                columns: new[] { "TeamId", "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TeamJoinRequests_UserId",
                table: "TeamJoinRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_TeamId_UserId",
                table: "TeamMembers",
                columns: new[] { "TeamId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_UserId",
                table: "TeamMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamPlayerDemands_CreatedByUserId",
                table: "TeamPlayerDemands",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamPlayerDemands_Status_NeededAt_ExpiresAt",
                table: "TeamPlayerDemands",
                columns: new[] { "Status", "NeededAt", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TeamPlayerDemands_TeamId_Status",
                table: "TeamPlayerDemands",
                columns: new[] { "TeamId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TeamPositions_TeamId_Name",
                table: "TeamPositions",
                columns: new[] { "TeamId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teams_InviteCode",
                table: "Teams",
                column: "InviteCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teams_NormalizedName",
                table: "Teams",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamScheduleSlots_TeamId_DayOfWeek_StartTime",
                table: "TeamScheduleSlots",
                columns: new[] { "TeamId", "DayOfWeek", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_TeamSlotAssignments_TeamId_UserId_ReleasedAt",
                table: "TeamSlotAssignments",
                columns: new[] { "TeamId", "UserId", "ReleasedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TeamSlotAssignments_TeamSlotPackageId",
                table: "TeamSlotAssignments",
                column: "TeamSlotPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamSlotAssignments_UserId",
                table: "TeamSlotAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamSlotPackages_PurchasedByUserId",
                table: "TeamSlotPackages",
                column: "PurchasedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamSlotPackages_TeamId",
                table: "TeamSlotPackages",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_TournamentId_RoundNumber",
                table: "TournamentMatches",
                columns: new[] { "TournamentId", "RoundNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_TournamentId_Stage_GroupName",
                table: "TournamentMatches",
                columns: new[] { "TournamentId", "Stage", "GroupName" });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMoments_IsPublishedToDiscord_CreatedAt",
                table: "TournamentMoments",
                columns: new[] { "IsPublishedToDiscord", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMoments_TournamentId_MatchId",
                table: "TournamentMoments",
                columns: new[] { "TournamentId", "MatchId" });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentPlayerConfirmations_IsLoan_LoanFromTeamId",
                table: "TournamentPlayerConfirmations",
                columns: new[] { "IsLoan", "LoanFromTeamId" });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentPlayerConfirmations_TeamId",
                table: "TournamentPlayerConfirmations",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentPlayerConfirmations_TournamentId_TeamId_Status",
                table: "TournamentPlayerConfirmations",
                columns: new[] { "TournamentId", "TeamId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentPlayerConfirmations_TournamentId_TeamId_UserId",
                table: "TournamentPlayerConfirmations",
                columns: new[] { "TournamentId", "TeamId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentPlayerConfirmations_UserId",
                table: "TournamentPlayerConfirmations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRegistrations_TeamId",
                table: "TournamentRegistrations",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRegistrations_TournamentId_TeamId",
                table: "TournamentRegistrations",
                columns: new[] { "TournamentId", "TeamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_NormalizedEmail",
                table: "Users",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserTournamentAccesses_UserId",
                table: "UserTournamentAccesses",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChampionTitles");

            migrationBuilder.DropTable(
                name: "EaApiCacheEntries");

            migrationBuilder.DropTable(
                name: "EaClubSnapshots");

            migrationBuilder.DropTable(
                name: "EaDiagnosticProbes");

            migrationBuilder.DropTable(
                name: "EaMatchClubStats");

            migrationBuilder.DropTable(
                name: "EaMatchPlayerStats");

            migrationBuilder.DropTable(
                name: "EaMatchSnapshots");

            migrationBuilder.DropTable(
                name: "EaMemberStatsSnapshots");

            migrationBuilder.DropTable(
                name: "EaPlayerProfileSnapshots");

            migrationBuilder.DropTable(
                name: "MatchScoreSubmissions");

            migrationBuilder.DropTable(
                name: "TeamJoinRequests");

            migrationBuilder.DropTable(
                name: "TeamMembers");

            migrationBuilder.DropTable(
                name: "TeamPlayerDemands");

            migrationBuilder.DropTable(
                name: "TeamPositions");

            migrationBuilder.DropTable(
                name: "TeamScheduleSlots");

            migrationBuilder.DropTable(
                name: "TeamSlotAssignments");

            migrationBuilder.DropTable(
                name: "TournamentMoments");

            migrationBuilder.DropTable(
                name: "TournamentPlayerConfirmations");

            migrationBuilder.DropTable(
                name: "TournamentRegistrations");

            migrationBuilder.DropTable(
                name: "UserTournamentAccesses");

            migrationBuilder.DropTable(
                name: "EaFriendlyMatches");

            migrationBuilder.DropTable(
                name: "TournamentMatches");

            migrationBuilder.DropTable(
                name: "TeamSlotPackages");

            migrationBuilder.DropTable(
                name: "Tournaments");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}

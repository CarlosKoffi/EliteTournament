CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE TABLE "EaApiCacheEntries" (
        "Id" uuid NOT NULL,
        "CacheKey" character varying(200) NOT NULL,
        "Endpoint" character varying(500) NOT NULL,
        "RawJson" jsonb NOT NULL,
        "FetchedAt" timestamp with time zone NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_EaApiCacheEntries" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE TABLE "EaDiagnosticProbes" (
        "Id" uuid NOT NULL,
        "StepName" character varying(80) NOT NULL,
        "Endpoint" character varying(500) NOT NULL,
        "Platform" character varying(40) NOT NULL,
        "ClubName" character varying(160),
        "ClubId" bigint,
        "Success" boolean NOT NULL,
        "StatusCode" integer,
        "Error" character varying(1000),
        "RawPreview" character varying(4000),
        "RawLength" integer,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_EaDiagnosticProbes" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE TABLE "Teams" (
        "Id" uuid NOT NULL,
        "Name" character varying(120) NOT NULL,
        "NormalizedName" character varying(120) NOT NULL,
        "ShortName" character varying(16),
        "Platform" integer NOT NULL,
        "Region" character varying(64),
        "InviteCode" character varying(24) NOT NULL,
        "InviteCodeExpiresAt" timestamp with time zone,
        "EaClubId" bigint,
        "Description" character varying(500),
        "LogoUrl" character varying(500),
        "BannerUrl" character varying(500),
        "DiscordUrl" character varying(300),
        "TwitchUrl" character varying(300),
        "TikTokUrl" character varying(300),
        "TwitterUrl" character varying(300),
        "RequireJoinApproval" boolean NOT NULL,
        "IsArchived" boolean NOT NULL,
        "ArchivedAt" timestamp with time zone,
        "CreatedByUserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Teams" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE TABLE "TournamentMoments" (
        "Id" uuid NOT NULL,
        "TournamentId" uuid NOT NULL,
        "MatchId" uuid,
        "TeamId" uuid,
        "UserId" uuid,
        "DiscordUserId" character varying(80),
        "Type" integer NOT NULL,
        "Title" character varying(160) NOT NULL,
        "Message" character varying(1000) NOT NULL,
        "PayloadJson" jsonb,
        "IsPublishedToDiscord" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "PublishedToDiscordAt" timestamp with time zone,
        CONSTRAINT "PK_TournamentMoments" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE TABLE "Tournaments" (
        "Id" uuid NOT NULL,
        "Name" character varying(160) NOT NULL,
        "Type" integer NOT NULL,
        "Status" integer NOT NULL,
        "StartsAt" timestamp with time zone NOT NULL,
        "TimeZone" character varying(80) NOT NULL,
        "MinTeams" integer NOT NULL,
        "MaxTeams" integer NOT NULL,
        "EntryFee" numeric NOT NULL,
        "Currency" character varying(3) NOT NULL,
        "GoodiesDescription" character varying(1000),
        "RegistrationLockAt" timestamp with time zone,
        "RegistrationStartsAt" timestamp with time zone,
        "RegistrationEndsAt" timestamp with time zone,
        "EstimatedPrizeBudget" numeric,
        "IsCashPrize" boolean NOT NULL,
        "EaMonitoringStartsMinutesBefore" integer NOT NULL,
        "EaMonitoringEndsMinutesAfter" integer NOT NULL,
        "PlayerRestrictionsJson" jsonb,
        "BannerUrl" character varying(1000),
        "CreatedByUserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Tournaments" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE TABLE "Users" (
        "Id" uuid NOT NULL,
        "Email" character varying(320) NOT NULL,
        "NormalizedEmail" character varying(320) NOT NULL,
        "PasswordHash" text NOT NULL,
        "DisplayName" character varying(80) NOT NULL,
        "Gamertag" character varying(80),
        "EaSportsId" character varying(80),
        "DiscordUserId" character varying(80),
        "Platform" integer NOT NULL,
        "PreferredLanguage" character varying(10) NOT NULL,
        "TimeZone" character varying(80) NOT NULL,
        "ProfileImageUrl" character varying(1000),
        "EaClubId" bigint,
        "EaClubName" character varying(120),
        "CreatedAt" timestamp with time zone NOT NULL,
        "LastLoginAt" timestamp with time zone,
        CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE TABLE "ChampionTitles" (
        "Id" uuid NOT NULL,
        "TeamId" uuid NOT NULL,
        "TournamentId" uuid NOT NULL,
        "CrownedAt" timestamp with time zone NOT NULL,
        "DethronedAt" timestamp with time zone,
        "IsActive" boolean NOT NULL,
        "NextEntryDiscountAmount" numeric NOT NULL,
        "Currency" character varying(3) NOT NULL,
        CONSTRAINT "PK_ChampionTitles" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ChampionTitles_Teams_TeamId" FOREIGN KEY ("TeamId") REFERENCES "Teams" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE TABLE "EaClubSnapshots" (
        "Id" uuid NOT NULL,
        "TeamId" uuid NOT NULL,
        "EaClubId" bigint NOT NULL,
        "Platform" character varying(40) NOT NULL,
        "Name" character varying(120),
        "Abbreviation" character varying(20),
        "Division" integer,
        "MembersCount" integer,
        "RawJson" jsonb NOT NULL,
        "SyncedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_EaClubSnapshots" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_EaClubSnapshots_Teams_TeamId" FOREIGN KEY ("TeamId") REFERENCES "Teams" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE TABLE "EaFriendlyMatches" (
        "Id" uuid NOT NULL,
        "TeamId" uuid NOT NULL,
        "EaClubId" bigint NOT NULL,
        "Platform" character varying(40) NOT NULL,
        "EaMatchId" character varying(80) NOT NULL,
        "PlayedAt" timestamp with time zone NOT NULL,
        "MatchType" character varying(40) NOT NULL,
        "HomeEaClubId" bigint NOT NULL,
        "HomeClubName" character varying(120),
        "HomeScore" integer NOT NULL,
        "AwayEaClubId" bigint NOT NULL,
        "AwayClubName" character varying(120),
        "AwayScore" integer NOT NULL,
        "RawJson" jsonb NOT NULL,
        "SyncedAt" timestamp with time zone NOT NULL,
        "TournamentMatchId" uuid,
        CONSTRAINT "PK_EaFriendlyMatches" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_EaFriendlyMatches_Teams_TeamId" FOREIGN KEY ("TeamId") REFERENCES "Teams" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE TABLE "EaMatchSnapshots" (
        "Id" uuid NOT NULL,
        "TeamId" uuid NOT NULL,
        "EaClubId" bigint NOT NULL,
        "Platform" character varying(40) NOT NULL,
        "MatchType" character varying(80) NOT NULL,
        "RawJson" jsonb NOT NULL,
        "SyncedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_EaMatchSnapshots" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_EaMatchSnapshots_Teams_TeamId" FOREIGN KEY ("TeamId") REFERENCES "Teams" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE TABLE "EaMemberStatsSnapshots" (
        "Id" uuid NOT NULL,
        "TeamId" uuid NOT NULL,
        "EaClubId" bigint NOT NULL,
        "Platform" character varying(40) NOT NULL,
        "RawJson" jsonb NOT NULL,
        "SyncedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_EaMemberStatsSnapshots" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_EaMemberStatsSnapshots_Teams_TeamId" FOREIGN KEY ("TeamId") REFERENCES "Teams" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE TABLE "EaPlayerProfileSnapshots" (
        "Id" uuid NOT NULL,
        "TeamId" uuid NOT NULL,
        "EaClubId" bigint NOT NULL,
        "Platform" character varying(40) NOT NULL,
        "EaPlayerId" character varying(80) NOT NULL,
        "PlayerName" character varying(120) NOT NULL,
        "ProName" character varying(120),
        "Position" character varying(40),
        "Matches" integer,
        "WinRate" integer,
        "Goals" integer,
        "Assists" integer,
        "AverageRating" double precision,
        "Height" integer,
        "Weight" integer,
        "Overall" integer,
        "Shots" integer,
        "ShotSuccessRate" integer,
        "PassesMade" integer,
        "PassAttempts" integer,
        "StoredPassSuccessRate" double precision,
        "TacklesMade" integer,
        "TackleAttempts" integer,
        "StoredTackleSuccessRate" double precision,
        "Saves" integer,
        "CleanSheets" integer,
        "CleanSheetsGk" integer,
        "PlayerOfTheMatch" integer,
        "RedCards" integer,
        "PrevGoals" integer,
        "RawJson" jsonb NOT NULL,
        "SyncedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_EaPlayerProfileSnapshots" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_EaPlayerProfileSnapshots_Teams_TeamId" FOREIGN KEY ("TeamId") REFERENCES "Teams" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE TABLE "TeamPositions" (
        "Id" uuid NOT NULL,
        "TeamId" uuid NOT NULL,
        "Name" character varying(80) NOT NULL,
        "Description" character varying(300),
        "SortOrder" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_TeamPositions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_TeamPositions_Teams_TeamId" FOREIGN KEY ("TeamId") REFERENCES "Teams" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE TABLE "TeamScheduleSlots" (
        "Id" uuid NOT NULL,
        "TeamId" uuid NOT NULL,
        "DayOfWeek" integer NOT NULL,
        "StartTime" time without time zone NOT NULL,
        "EndTime" time without time zone NOT NULL,
        "Label" character varying(120),
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_TeamScheduleSlots" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_TeamScheduleSlots_Teams_TeamId" FOREIGN KEY ("TeamId") REFERENCES "Teams" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE TABLE "TournamentMatches" (
        "Id" uuid NOT NULL,
        "TournamentId" uuid NOT NULL,
        "HomeTeamId" uuid NOT NULL,
        "AwayTeamId" uuid NOT NULL,
        "RoundNumber" integer NOT NULL,
        "Stage" integer NOT NULL,
        "GroupName" character varying(8),
        "MatchNumber" integer NOT NULL,
        "ScheduledAt" timestamp with time zone NOT NULL,
        "EaLookupFrom" timestamp with time zone NOT NULL,
        "EaLookupUntil" timestamp with time zone NOT NULL,
        "HomeScore" integer,
        "AwayScore" integer,
        "Status" integer NOT NULL,
        "WinnerTeamId" uuid,
        "EaRawMatchJson" jsonb,
        "VerifiedAt" timestamp with time zone,
        CONSTRAINT "PK_TournamentMatches" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_TournamentMatches_Tournaments_TournamentId" FOREIGN KEY ("TournamentId") REFERENCES "Tournaments" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE TABLE "TournamentRegistrations" (
        "Id" uuid NOT NULL,
        "TournamentId" uuid NOT NULL,
        "TeamId" uuid NOT NULL,
        "RegisteredByUserId" uuid NOT NULL,
        "PaymentMode" integer NOT NULL,
        "Status" integer NOT NULL,
        "IsPaymentComplete" boolean NOT NULL,
        "Source" character varying(40) NOT NULL,
        "DiscordGuildId" character varying(80),
        "DiscordChannelId" character varying(80),
        "DiscordMessageId" character varying(80),
        "DiscordRequestedByUserId" character varying(80),
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_TournamentRegistrations" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_TournamentRegistrations_Teams_TeamId" FOREIGN KEY ("TeamId") REFERENCES "Teams" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_TournamentRegistrations_Tournaments_TournamentId" FOREIGN KEY ("TournamentId") REFERENCES "Tournaments" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE TABLE "TeamJoinRequests" (
        "Id" uuid NOT NULL,
        "TeamId" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Message" character varying(500),
        "Status" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "DecidedAt" timestamp with time zone,
        "DecidedByUserId" uuid,
        CONSTRAINT "PK_TeamJoinRequests" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_TeamJoinRequests_Teams_TeamId" FOREIGN KEY ("TeamId") REFERENCES "Teams" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_TeamJoinRequests_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE TABLE "TeamMembers" (
        "Id" uuid NOT NULL,
        "TeamId" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Role" integer NOT NULL,
        "Status" integer NOT NULL,
        "JoinedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_TeamMembers" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_TeamMembers_Teams_TeamId" FOREIGN KEY ("TeamId") REFERENCES "Teams" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_TeamMembers_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE TABLE "TeamPlayerDemands" (
        "Id" uuid NOT NULL,
        "TeamId" uuid NOT NULL,
        "CreatedByUserId" uuid NOT NULL,
        "Position" character varying(80) NOT NULL,
        "NeededAt" timestamp with time zone NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        "Note" character varying(500),
        "Status" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_TeamPlayerDemands" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_TeamPlayerDemands_Teams_TeamId" FOREIGN KEY ("TeamId") REFERENCES "Teams" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_TeamPlayerDemands_Users_CreatedByUserId" FOREIGN KEY ("CreatedByUserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE TABLE "TeamSlotPackages" (
        "Id" uuid NOT NULL,
        "TeamId" uuid NOT NULL,
        "PurchasedByUserId" uuid NOT NULL,
        "SlotCount" integer NOT NULL,
        "PricePaid" numeric NOT NULL,
        "Currency" character varying(3) NOT NULL,
        "Provider" integer NOT NULL,
        "ProviderTransactionId" character varying(160) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_TeamSlotPackages" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_TeamSlotPackages_Teams_TeamId" FOREIGN KEY ("TeamId") REFERENCES "Teams" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_TeamSlotPackages_Users_PurchasedByUserId" FOREIGN KEY ("PurchasedByUserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE TABLE "TournamentPlayerConfirmations" (
        "Id" uuid NOT NULL,
        "TournamentId" uuid NOT NULL,
        "TeamId" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Position" character varying(80) NOT NULL,
        "Status" integer NOT NULL,
        "DelayMinutes" integer,
        "Note" character varying(500),
        "IsLoan" boolean NOT NULL,
        "LoanFromTeamId" uuid,
        "LoanApprovedAt" timestamp with time zone,
        "LoanApprovedByUserId" uuid,
        "LastReminderSentAt" timestamp with time zone,
        "RequiresOwnerNotification" boolean NOT NULL,
        "ReplacementSuggested" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_TournamentPlayerConfirmations" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_TournamentPlayerConfirmations_Teams_TeamId" FOREIGN KEY ("TeamId") REFERENCES "Teams" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_TournamentPlayerConfirmations_Tournaments_TournamentId" FOREIGN KEY ("TournamentId") REFERENCES "Tournaments" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_TournamentPlayerConfirmations_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE TABLE "UserTournamentAccesses" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Provider" integer NOT NULL,
        "ProviderTransactionId" character varying(160) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_UserTournamentAccesses" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_UserTournamentAccesses_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE TABLE "EaMatchClubStats" (
        "Id" uuid NOT NULL,
        "EaFriendlyMatchId" uuid NOT NULL,
        "TeamId" uuid NOT NULL,
        "EaClubId" bigint NOT NULL,
        "Goals" integer,
        "Assists" integer,
        "Rating" double precision,
        "Shots" integer,
        "PassesMade" integer,
        "PassAttempts" integer,
        "TacklesMade" integer,
        "TackleAttempts" integer,
        "Saves" integer,
        "GoalsConceded" integer,
        "RedCards" integer,
        "PlayerOfTheMatch" integer,
        "RawJson" jsonb NOT NULL,
        CONSTRAINT "PK_EaMatchClubStats" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_EaMatchClubStats_EaFriendlyMatches_EaFriendlyMatchId" FOREIGN KEY ("EaFriendlyMatchId") REFERENCES "EaFriendlyMatches" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE TABLE "EaMatchPlayerStats" (
        "Id" uuid NOT NULL,
        "EaFriendlyMatchId" uuid NOT NULL,
        "TeamId" uuid NOT NULL,
        "EaClubId" bigint NOT NULL,
        "EaPlayerId" character varying(80) NOT NULL,
        "PlayerName" character varying(120) NOT NULL,
        "Position" character varying(40),
        "Goals" integer,
        "Assists" integer,
        "Rating" double precision,
        "PlayerOfTheMatch" boolean NOT NULL,
        "Shots" integer,
        "PassesMade" integer,
        "PassAttempts" integer,
        "TacklesMade" integer,
        "TackleAttempts" integer,
        "Saves" integer,
        "GoalsConceded" integer,
        "SecondsPlayed" integer,
        "VproAttributes" character varying(300),
        "CleanSheetsAny" integer,
        "CleanSheetsDef" integer,
        "CleanSheetsGk" integer,
        "BallDiveSaves" integer,
        "CrossSaves" integer,
        "GoodDirectionSaves" integer,
        "ParrySaves" integer,
        "PunchSaves" integer,
        "ReflexSaves" integer,
        "RedCards" integer,
        "GameTime" integer,
        "RealtimeGame" integer,
        "RealtimeIdle" integer,
        "ArchetypeId" integer,
        "MatchEventAggregate0" character varying(1000),
        "MatchEventAggregate1" character varying(1000),
        "MatchEventAggregate2" character varying(1000),
        "MatchEventAggregate3" character varying(1000),
        "RawJson" jsonb NOT NULL,
        CONSTRAINT "PK_EaMatchPlayerStats" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_EaMatchPlayerStats_EaFriendlyMatches_EaFriendlyMatchId" FOREIGN KEY ("EaFriendlyMatchId") REFERENCES "EaFriendlyMatches" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE TABLE "MatchScoreSubmissions" (
        "Id" uuid NOT NULL,
        "MatchId" uuid NOT NULL,
        "SubmittedByUserId" uuid NOT NULL,
        "HomeScore" integer NOT NULL,
        "AwayScore" integer NOT NULL,
        "Source" integer NOT NULL,
        "ProofUrl" character varying(500),
        "SubmittedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_MatchScoreSubmissions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_MatchScoreSubmissions_TournamentMatches_MatchId" FOREIGN KEY ("MatchId") REFERENCES "TournamentMatches" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE TABLE "TeamSlotAssignments" (
        "Id" uuid NOT NULL,
        "TeamSlotPackageId" uuid NOT NULL,
        "TeamId" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "AssignedByUserId" uuid NOT NULL,
        "AssignedAt" timestamp with time zone NOT NULL,
        "ReleasedAt" timestamp with time zone,
        CONSTRAINT "PK_TeamSlotAssignments" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_TeamSlotAssignments_TeamSlotPackages_TeamSlotPackageId" FOREIGN KEY ("TeamSlotPackageId") REFERENCES "TeamSlotPackages" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_TeamSlotAssignments_Teams_TeamId" FOREIGN KEY ("TeamId") REFERENCES "Teams" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_TeamSlotAssignments_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE INDEX "IX_ChampionTitles_TeamId_IsActive" ON "ChampionTitles" ("TeamId", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_EaApiCacheEntries_CacheKey" ON "EaApiCacheEntries" ("CacheKey");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE INDEX "IX_EaApiCacheEntries_ExpiresAt" ON "EaApiCacheEntries" ("ExpiresAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_EaClubSnapshots_TeamId" ON "EaClubSnapshots" ("TeamId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE INDEX "IX_EaDiagnosticProbes_CreatedAt" ON "EaDiagnosticProbes" ("CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_EaFriendlyMatches_TeamId_EaMatchId" ON "EaFriendlyMatches" ("TeamId", "EaMatchId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE INDEX "IX_EaFriendlyMatches_TeamId_PlayedAt" ON "EaFriendlyMatches" ("TeamId", "PlayedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE INDEX "IX_EaFriendlyMatches_TournamentMatchId" ON "EaFriendlyMatches" ("TournamentMatchId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_EaMatchClubStats_EaFriendlyMatchId_EaClubId" ON "EaMatchClubStats" ("EaFriendlyMatchId", "EaClubId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_EaMatchPlayerStats_EaFriendlyMatchId_EaPlayerId_EaClubId" ON "EaMatchPlayerStats" ("EaFriendlyMatchId", "EaPlayerId", "EaClubId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE INDEX "IX_EaMatchPlayerStats_TeamId_PlayerName" ON "EaMatchPlayerStats" ("TeamId", "PlayerName");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_EaMatchSnapshots_TeamId_MatchType" ON "EaMatchSnapshots" ("TeamId", "MatchType");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_EaMemberStatsSnapshots_TeamId" ON "EaMemberStatsSnapshots" ("TeamId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE INDEX "IX_EaPlayerProfileSnapshots_PlayerName" ON "EaPlayerProfileSnapshots" ("PlayerName");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_EaPlayerProfileSnapshots_TeamId_EaPlayerId" ON "EaPlayerProfileSnapshots" ("TeamId", "EaPlayerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE INDEX "IX_MatchScoreSubmissions_MatchId" ON "MatchScoreSubmissions" ("MatchId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE INDEX "IX_TeamJoinRequests_TeamId_UserId_Status" ON "TeamJoinRequests" ("TeamId", "UserId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE INDEX "IX_TeamJoinRequests_UserId" ON "TeamJoinRequests" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_TeamMembers_TeamId_UserId" ON "TeamMembers" ("TeamId", "UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE INDEX "IX_TeamMembers_UserId" ON "TeamMembers" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE INDEX "IX_TeamPlayerDemands_CreatedByUserId" ON "TeamPlayerDemands" ("CreatedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE INDEX "IX_TeamPlayerDemands_Status_NeededAt_ExpiresAt" ON "TeamPlayerDemands" ("Status", "NeededAt", "ExpiresAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE INDEX "IX_TeamPlayerDemands_TeamId_Status" ON "TeamPlayerDemands" ("TeamId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_TeamPositions_TeamId_Name" ON "TeamPositions" ("TeamId", "Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Teams_InviteCode" ON "Teams" ("InviteCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Teams_NormalizedName" ON "Teams" ("NormalizedName");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE INDEX "IX_TeamScheduleSlots_TeamId_DayOfWeek_StartTime" ON "TeamScheduleSlots" ("TeamId", "DayOfWeek", "StartTime");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE INDEX "IX_TeamSlotAssignments_TeamId_UserId_ReleasedAt" ON "TeamSlotAssignments" ("TeamId", "UserId", "ReleasedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE INDEX "IX_TeamSlotAssignments_TeamSlotPackageId" ON "TeamSlotAssignments" ("TeamSlotPackageId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE INDEX "IX_TeamSlotAssignments_UserId" ON "TeamSlotAssignments" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE INDEX "IX_TeamSlotPackages_PurchasedByUserId" ON "TeamSlotPackages" ("PurchasedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE INDEX "IX_TeamSlotPackages_TeamId" ON "TeamSlotPackages" ("TeamId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE INDEX "IX_TournamentMatches_TournamentId_RoundNumber" ON "TournamentMatches" ("TournamentId", "RoundNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE INDEX "IX_TournamentMatches_TournamentId_Stage_GroupName" ON "TournamentMatches" ("TournamentId", "Stage", "GroupName");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE INDEX "IX_TournamentMoments_IsPublishedToDiscord_CreatedAt" ON "TournamentMoments" ("IsPublishedToDiscord", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE INDEX "IX_TournamentMoments_TournamentId_MatchId" ON "TournamentMoments" ("TournamentId", "MatchId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE INDEX "IX_TournamentPlayerConfirmations_IsLoan_LoanFromTeamId" ON "TournamentPlayerConfirmations" ("IsLoan", "LoanFromTeamId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE INDEX "IX_TournamentPlayerConfirmations_TeamId" ON "TournamentPlayerConfirmations" ("TeamId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE INDEX "IX_TournamentPlayerConfirmations_TournamentId_TeamId_Status" ON "TournamentPlayerConfirmations" ("TournamentId", "TeamId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_TournamentPlayerConfirmations_TournamentId_TeamId_UserId" ON "TournamentPlayerConfirmations" ("TournamentId", "TeamId", "UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE INDEX "IX_TournamentPlayerConfirmations_UserId" ON "TournamentPlayerConfirmations" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE INDEX "IX_TournamentRegistrations_TeamId" ON "TournamentRegistrations" ("TeamId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_TournamentRegistrations_TournamentId_TeamId" ON "TournamentRegistrations" ("TournamentId", "TeamId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Users_NormalizedEmail" ON "Users" ("NormalizedEmail");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_UserTournamentAccesses_UserId" ON "UserTournamentAccesses" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628122250_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260628122250_InitialCreate', '9.0.3');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628231049_AddLocalizedContent') THEN
    CREATE TABLE "LocalizedContents" (
        "Id" uuid NOT NULL,
        "Key" character varying(160) NOT NULL,
        "Language" character varying(10) NOT NULL,
        "Value" character varying(4000) NOT NULL,
        "Section" character varying(120),
        "Description" character varying(500),
        "UpdatedAt" timestamp with time zone NOT NULL,
        "UpdatedByUserId" uuid,
        CONSTRAINT "PK_LocalizedContents" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628231049_AddLocalizedContent') THEN
    CREATE UNIQUE INDEX "IX_LocalizedContents_Key_Language" ON "LocalizedContents" ("Key", "Language");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628231049_AddLocalizedContent') THEN
    CREATE INDEX "IX_LocalizedContents_Language_Section" ON "LocalizedContents" ("Language", "Section");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260628231049_AddLocalizedContent') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260628231049_AddLocalizedContent', '9.0.3');
    END IF;
END $EF$;
COMMIT;


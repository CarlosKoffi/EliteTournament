-- Seed demo application data from real synced EA friendly matches.
-- Goal: reuse the 8 latest TheSurvivors friendly matches as a completed tournament
-- so UI screens can display application/tournament player statistics.
--
-- Safe to run multiple times: it deletes/recreates only the seeded tournament
-- named below and relinks the same 8 latest friendlies.

CREATE EXTENSION IF NOT EXISTS pgcrypto;

DO $$
DECLARE
    v_team_id uuid;
    v_team_name text;
    v_team_ea_id bigint;
    v_user_id uuid;
    v_tournament_id uuid := '10000000-0000-0000-0000-214820700001'::uuid;
    v_now timestamptz := now();
    v_starts_at timestamptz;
    v_match record;
    v_match_id uuid;
    v_opponent_team_id uuid;
    v_opponent_name text;
    v_opponent_ea_id bigint;
    v_home_team_id uuid;
    v_away_team_id uuid;
    v_home_score int;
    v_away_score int;
    v_winner_team_id uuid;
    v_index int := 0;
    v_stage int;
    v_group_name text;
    v_seeded_count int;
    v_final_winner_team_id uuid;
BEGIN
    SELECT "Id", "Name", "EaClubId"
    INTO v_team_id, v_team_name, v_team_ea_id
    FROM "Teams"
    WHERE "EaClubId" = 2148207 OR lower("Name") = lower('TheSurvivors')
    ORDER BY CASE WHEN "EaClubId" = 2148207 THEN 0 ELSE 1 END
    LIMIT 1;

    IF v_team_id IS NULL THEN
        RAISE NOTICE 'Seed skipped: TheSurvivors team was not found.';
        RETURN;
    END IF;
    v_team_ea_id := COALESCE(v_team_ea_id, 2148207);

    SELECT "Id"
    INTO v_user_id
    FROM "Users"
    ORDER BY "IsAdmin" DESC, "CreatedAt"
    LIMIT 1;

    IF v_user_id IS NULL THEN
        RAISE NOTICE 'Seed skipped: no user exists to own the seeded tournament.';
        RETURN;
    END IF;

    SELECT count(*)
    INTO v_seeded_count
    FROM (
        SELECT 1
        FROM "EaFriendlyMatches"
        WHERE "TeamId" = v_team_id
        ORDER BY "PlayedAt" DESC
        LIMIT 8
    ) latest;

    IF v_seeded_count < 8 THEN
        RAISE NOTICE 'Seed skipped: only % friendly match(es) found for TheSurvivors, 8 required.', v_seeded_count;
        RETURN;
    END IF;

    SELECT min("PlayedAt")
    INTO v_starts_at
    FROM (
        SELECT "PlayedAt"
        FROM "EaFriendlyMatches"
        WHERE "TeamId" = v_team_id
        ORDER BY "PlayedAt" DESC
        LIMIT 8
    ) latest;

    UPDATE "EaFriendlyMatches"
    SET "TournamentMatchId" = NULL
    WHERE "TournamentMatchId" IN (
        SELECT "Id" FROM "TournamentMatches" WHERE "TournamentId" = v_tournament_id
    );

    DELETE FROM "TournamentScoreAudits" WHERE "TournamentId" = v_tournament_id;
    DELETE FROM "MatchScoreSubmissions" WHERE "MatchId" IN (SELECT "Id" FROM "TournamentMatches" WHERE "TournamentId" = v_tournament_id);
    DELETE FROM "TournamentPlayerConfirmations" WHERE "TournamentId" = v_tournament_id;
    DELETE FROM "TournamentMatches" WHERE "TournamentId" = v_tournament_id;
    DELETE FROM "TournamentRegistrations" WHERE "TournamentId" = v_tournament_id;
    DELETE FROM "TournamentMoments" WHERE "TournamentId" = v_tournament_id;
    DELETE FROM "ChampionTitles" WHERE "TournamentId" = v_tournament_id;
    DELETE FROM "Tournaments" WHERE "Id" = v_tournament_id;

    INSERT INTO "Tournaments" (
        "Id", "Name", "Type", "Status", "StartsAt", "TimeZone", "MinTeams", "MaxTeams",
        "EntryFee", "Currency", "GoodiesDescription", "RegistrationLockAt", "RegistrationStartsAt",
        "RegistrationEndsAt", "EstimatedPrizeBudget", "IsCashPrize", "EaMonitoringStartsMinutesBefore",
        "EaMonitoringEndsMinutesAfter", "PlayerRestrictionsJson", "BannerUrl", "CreatedByUserId",
        "CreatedAt", "ScoreRecoveryMode", "ScoreRecoveryIntervalMinutes", "AutoPublishPerfectScore", "Tier")
    VALUES (
        v_tournament_id, 'TheSurvivors EA Seed Cup', 2, 5, v_starts_at, 'Europe/Zurich', 24, 32,
        50.00, 'EUR', 'Seed technique: tournoi passe genere depuis les 8 derniers matchs amicaux TheSurvivors.',
        v_starts_at - interval '4 hours', v_starts_at - interval '7 days', v_starts_at - interval '4 hours',
        250.00, true, 15, 90,
        '{"maxHeightCm":182,"centerBackMaxHeightCm":187,"restrictedPlayStyles":[]}'::jsonb,
        null, v_user_id, v_now, 2, 2, true, 3);

    INSERT INTO "TournamentRegistrations" (
        "Id", "TournamentId", "TeamId", "RegisteredByUserId", "PaymentMode", "Status",
        "IsPaymentComplete", "Source", "DiscordGuildId", "DiscordChannelId", "DiscordMessageId",
        "DiscordRequestedByUserId", "CreatedAt")
    VALUES (gen_random_uuid(), v_tournament_id, v_team_id, v_user_id, 2, 2, true, 'Seed', null, null, null, null, v_now);

    FOR v_match IN
        SELECT *
        FROM (
            SELECT *
            FROM "EaFriendlyMatches"
            WHERE "TeamId" = v_team_id
            ORDER BY "PlayedAt" DESC
            LIMIT 8
        ) latest
        ORDER BY "PlayedAt" ASC
    LOOP
        v_index := v_index + 1;
        v_match_id := gen_random_uuid();
        v_opponent_ea_id := CASE WHEN v_match."HomeEaClubId" = v_team_ea_id THEN v_match."AwayEaClubId" ELSE v_match."HomeEaClubId" END;
        v_opponent_name := COALESCE(
            CASE WHEN v_match."HomeEaClubId" = v_team_ea_id THEN v_match."AwayClubName" ELSE v_match."HomeClubName" END,
            'Seed Opponent ' || v_index::text);

        INSERT INTO "Teams" (
            "Id", "Name", "NormalizedName", "ShortName", "Platform", "Region", "InviteCode",
            "InviteCodeExpiresAt", "EaClubId", "Description", "LogoUrl", "BannerUrl", "DiscordUrl",
            "TwitchUrl", "TikTokUrl", "TwitterUrl", "RequireJoinApproval", "IsArchived", "ArchivedAt",
            "CreatedByUserId", "CreatedAt")
        VALUES (
            gen_random_uuid(), v_opponent_name, upper(v_opponent_name), left(regexp_replace(upper(v_opponent_name), '[^A-Z0-9]', '', 'g'), 8),
            4, 'Seed', 'SEED' || left(replace(gen_random_uuid()::text, '-', ''), 12), null, v_opponent_ea_id,
            'Adversaire cree automatiquement pour le seed tournoi TheSurvivors.', null, null, null, null, null, null,
            false, false, null, v_user_id, v_now)
        ON CONFLICT ("NormalizedName") DO UPDATE
            SET "EaClubId" = COALESCE("Teams"."EaClubId", EXCLUDED."EaClubId")
        RETURNING "Id" INTO v_opponent_team_id;

        IF v_opponent_team_id IS NULL THEN
            SELECT "Id" INTO v_opponent_team_id FROM "Teams" WHERE "NormalizedName" = upper(v_opponent_name) LIMIT 1;
        END IF;

        INSERT INTO "TournamentRegistrations" (
            "Id", "TournamentId", "TeamId", "RegisteredByUserId", "PaymentMode", "Status",
            "IsPaymentComplete", "Source", "DiscordGuildId", "DiscordChannelId", "DiscordMessageId",
            "DiscordRequestedByUserId", "CreatedAt")
        VALUES (gen_random_uuid(), v_tournament_id, v_opponent_team_id, v_user_id, 2, 2, true, 'Seed', null, null, null, null, v_now)
        ON CONFLICT ("TournamentId", "TeamId") DO NOTHING;

        v_stage := CASE
            WHEN v_index <= 3 THEN 1
            WHEN v_index = 4 THEN 2
            WHEN v_index = 5 THEN 2
            WHEN v_index = 6 THEN 3
            WHEN v_index = 7 THEN 4
            ELSE 5
        END;
        v_group_name := CASE WHEN v_index <= 3 THEN 'A' ELSE NULL END;

        IF v_match."HomeEaClubId" = v_team_ea_id THEN
            v_home_team_id := v_team_id;
            v_away_team_id := v_opponent_team_id;
        ELSE
            v_home_team_id := v_opponent_team_id;
            v_away_team_id := v_team_id;
        END IF;

        -- The tournament score must always match the linked EA friendly score.
        v_home_score := v_match."HomeScore";
        v_away_score := v_match."AwayScore";
        v_winner_team_id := CASE
            WHEN v_home_score = v_away_score THEN NULL
            WHEN v_home_score > v_away_score THEN v_home_team_id
            ELSE v_away_team_id
        END;

        IF v_stage = 5 THEN
            v_final_winner_team_id := v_winner_team_id;
        END IF;

        INSERT INTO "TournamentMatches" (
            "Id", "TournamentId", "HomeTeamId", "AwayTeamId", "RoundNumber", "Stage", "GroupName",
            "MatchNumber", "ScheduledAt", "EaLookupFrom", "EaLookupUntil", "HomeScore", "AwayScore",
            "Status", "WinnerTeamId", "EaRawMatchJson", "VerifiedAt")
        VALUES (
            v_match_id, v_tournament_id, v_home_team_id, v_away_team_id, v_index, v_stage, v_group_name,
            v_index, v_match."PlayedAt", v_match."PlayedAt" - interval '20 minutes', v_match."PlayedAt" + interval '45 minutes',
            v_home_score, v_away_score, 5, v_winner_team_id, v_match."RawJson", v_now);

        UPDATE "EaFriendlyMatches"
        SET "TournamentMatchId" = v_match_id
        WHERE "Id" = v_match."Id";

        INSERT INTO "TournamentScoreAudits" (
            "Id", "TournamentId", "TournamentMatchId", "AttemptedAt", "Trigger", "Status", "Summary",
            "EaMatchId", "CandidateCount", "Confidence", "HomeScore", "AwayScore", "ManualHomeScore",
            "ManualAwayScore", "TeamsMatched", "TimestampMatched", "ScoreMatched", "PlayersMatched",
            "DurationLooksComplete", "DuplicateCandidateDetected", "IssuesJson", "EvidenceJson", "RawCandidateJson")
        VALUES (
            gen_random_uuid(), v_tournament_id, v_match_id, v_now, 'Seed', 1,
            'Match lie automatiquement au tournoi seed TheSurvivors.',
            v_match."EaMatchId", 1, 100, v_home_score, v_away_score, null, null,
            true, true, true, true, true, false, '[]'::jsonb, jsonb_build_object('seed', true, 'eaMatchId', v_match."EaMatchId"), v_match."RawJson");
    END LOOP;

    IF v_final_winner_team_id IS NOT NULL THEN
        INSERT INTO "ChampionTitles" ("Id", "TeamId", "TournamentId", "CrownedAt", "DethronedAt", "IsActive", "NextEntryDiscountAmount", "Currency")
        VALUES (gen_random_uuid(), v_final_winner_team_id, v_tournament_id, v_now, null, true, 15.00, 'EUR');
    END IF;

    RAISE NOTICE 'Seed completed: TheSurvivors tournament % linked to % friendly matches.', v_tournament_id, v_index;
END $$;

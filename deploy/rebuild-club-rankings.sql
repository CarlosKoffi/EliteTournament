DELETE FROM "TeamRankings";

WITH team_matches AS (
    SELECT
        tm."TournamentId",
        t."StartsAt" AS "TournamentStartsAt",
        tm."Stage",
        tm."HomeTeamId" AS "TeamId",
        tm."HomeScore" AS "GoalsFor",
        tm."AwayScore" AS "GoalsAgainst",
        tm."WinnerTeamId"
    FROM "TournamentMatches" tm
    INNER JOIN "Tournaments" t ON t."Id" = tm."TournamentId"
    WHERE tm."HomeScore" IS NOT NULL AND tm."AwayScore" IS NOT NULL

    UNION ALL

    SELECT
        tm."TournamentId",
        t."StartsAt" AS "TournamentStartsAt",
        tm."Stage",
        tm."AwayTeamId" AS "TeamId",
        tm."AwayScore" AS "GoalsFor",
        tm."HomeScore" AS "GoalsAgainst",
        tm."WinnerTeamId"
    FROM "TournamentMatches" tm
    INNER JOIN "Tournaments" t ON t."Id" = tm."TournamentId"
    WHERE tm."HomeScore" IS NOT NULL AND tm."AwayScore" IS NOT NULL
),
match_points AS (
    SELECT
        *,
        CASE
            WHEN "Stage" = 2 THEN 1.10
            WHEN "Stage" = 3 THEN 1.20
            WHEN "Stage" = 4 THEN 1.35
            WHEN "Stage" = 5 THEN 1.55
            WHEN "Stage" = 6 THEN 1.25
            ELSE 1.00
        END AS "Importance",
        CASE
            WHEN "GoalsFor" = "GoalsAgainst" THEN 10
            WHEN "GoalsFor" > "GoalsAgainst" OR "WinnerTeamId" = "TeamId" THEN 30
            ELSE -10
        END AS "ResultPoints",
        LEAST(GREATEST("GoalsFor" - "GoalsAgainst", -4), 4) * 3 AS "GoalDifferenceBonus",
        LEAST("GoalsFor", 5) AS "AttackBonus",
        CASE WHEN "GoalsAgainst" = 0 THEN 5 ELSE 0 END AS "CleanSheetBonus"
    FROM team_matches
),
team_totals AS (
    SELECT
        "TeamId",
        COUNT(DISTINCT "TournamentId") AS "TournamentsPlayed",
        COUNT(*) AS "MatchesPlayed",
        SUM(CASE WHEN "GoalsFor" > "GoalsAgainst" OR ("WinnerTeamId" = "TeamId" AND "GoalsFor" <> "GoalsAgainst") THEN 1 ELSE 0 END) AS "Wins",
        SUM(CASE WHEN "GoalsFor" = "GoalsAgainst" THEN 1 ELSE 0 END) AS "Draws",
        SUM(CASE WHEN "GoalsFor" < "GoalsAgainst" AND ("WinnerTeamId" IS NULL OR "WinnerTeamId" <> "TeamId") THEN 1 ELSE 0 END) AS "Losses",
        SUM("GoalsFor") AS "GoalsFor",
        SUM("GoalsAgainst") AS "GoalsAgainst",
        SUM(CASE WHEN "GoalsAgainst" = 0 THEN 1 ELSE 0 END) AS "CleanSheets",
        COUNT(DISTINCT CASE WHEN "Stage" = 5 THEN "TournamentId" END) AS "Finals",
        COUNT(DISTINCT CASE WHEN "Stage" IN (4, 5, 6) THEN "TournamentId" END) AS "Podiums",
        MAX("TournamentStartsAt") AS "LastTournamentAt",
        SUM(("ResultPoints" + "GoalDifferenceBonus" + "AttackBonus" + "CleanSheetBonus") * "Importance") AS "MatchPoints"
    FROM match_points
    GROUP BY "TeamId"
),
title_totals AS (
    SELECT "TeamId", COUNT(DISTINCT "TournamentId") AS "Titles"
    FROM "ChampionTitles"
    GROUP BY "TeamId"
),
ranked AS (
    SELECT
        totals.*,
        COALESCE(titles."Titles", 0) AS "Titles",
        GREATEST(
            0,
            ROUND(
                1000
                + totals."MatchPoints"
                + COALESCE(titles."Titles", 0) * 120
                + GREATEST(totals."Finals" - COALESCE(titles."Titles", 0), 0) * 60
                + GREATEST(totals."Podiums" - totals."Finals" - COALESCE(titles."Titles", 0), 0) * 35
            )::integer
        ) AS "Points"
    FROM team_totals totals
    LEFT JOIN title_totals titles ON titles."TeamId" = totals."TeamId"
)
INSERT INTO "TeamRankings" (
    "Id", "TeamId", "Rank", "Points", "TournamentsPlayed", "MatchesPlayed",
    "Wins", "Draws", "Losses", "GoalsFor", "GoalsAgainst", "CleanSheets",
    "Titles", "Finals", "Podiums", "LastTournamentAt", "UpdatedAt")
SELECT
    gen_random_uuid(),
    "TeamId",
    ROW_NUMBER() OVER (ORDER BY "Points" DESC, ("GoalsFor" - "GoalsAgainst") DESC, "GoalsFor" DESC, "TeamId")::integer,
    "Points",
    "TournamentsPlayed"::integer,
    "MatchesPlayed"::integer,
    "Wins"::integer,
    "Draws"::integer,
    "Losses"::integer,
    "GoalsFor"::integer,
    "GoalsAgainst"::integer,
    "CleanSheets"::integer,
    "Titles"::integer,
    "Finals"::integer,
    "Podiums"::integer,
    "LastTournamentAt",
    now()
FROM ranked;

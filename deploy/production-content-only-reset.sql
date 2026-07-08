-- TS Tournament production reset: keep website labels/texts only.
--
-- This script keeps:
-- - "__EFMigrationsHistory" so EF Core knows which migrations are already applied.
-- - "LocalizedContents" so website labels, menu texts and translatable content stay available.
--
-- This script removes:
-- - users/admin accounts
-- - teams and memberships
-- - tournaments and tournament history
-- - EA snapshots/cache/diagnostics
-- - billing/access/slot data
--
-- IMPORTANT:
-- If Seed__AdminEmail and Seed__AdminPassword remain configured in Production,
-- the API will recreate an admin user on startup when no admin exists.

begin;

-- Optional pre-check.
select 'before' as phase, 'Users' as table_name, count(*) as rows from "Users"
union all select 'before', 'Teams', count(*) from "Teams"
union all select 'before', 'Tournaments', count(*) from "Tournaments"
union all select 'before', 'LocalizedContents', count(*) from "LocalizedContents";

truncate table
    "TournamentMoments",
    "MatchScoreSubmissions",
    "TournamentPlayerConfirmations",
    "TournamentMatches",
    "TournamentRegistrations",
    "ChampionTitles",
    "Tournaments",
    "UserTournamentAccesses",
    "TeamSlotAssignments",
    "TeamSlotPackages",
    "TeamPlayerDemands",
    "TeamScheduleSlots",
    "TeamPositions",
    "TeamJoinRequests",
    "TeamMembers",
    "Teams",
    "EaMatchPlayerStats",
    "EaMatchClubStats",
    "EaFriendlyMatches",
    "EaPlayerProfileSnapshots",
    "EaMatchSnapshots",
    "EaMemberStatsSnapshots",
    "EaClubSnapshots",
    "EaApiCacheEntries",
    "EaDiagnosticProbes",
    "Users"
restart identity cascade;

-- Keep only clean semantic localized content keys.
-- This removes old broad auto-extracted keys like home.001..., account.001..., etc.
delete from "LocalizedContents"
where "Key" ~ '^(home|account|teams|mercato|tournaments|admin\.tournaments|admin\.tournaments\.new|admin\.tournaments\.tracking|admin\.content|login|ea\.lab)\.[0-9]{3}\.';

-- Optional post-check.
select 'after' as phase, 'Users' as table_name, count(*) as rows from "Users"
union all select 'after', 'Teams', count(*) from "Teams"
union all select 'after', 'Tournaments', count(*) from "Tournaments"
union all select 'after', 'LocalizedContents', count(*) from "LocalizedContents";

commit;

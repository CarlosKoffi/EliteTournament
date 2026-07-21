-- TS Tournament production verification.
-- Run this on the same PostgreSQL database used by the deployed API.

select current_database() as database_name, current_user as database_user, now() as checked_at;

select 'Users' as table_name, count(*) as rows from "Users"
union all select 'Teams', count(*) from "Teams"
union all select 'TeamMembers', count(*) from "TeamMembers"
union all select 'TeamJoinRequests', count(*) from "TeamJoinRequests"
union all select 'Tournaments', count(*) from "Tournaments"
union all select 'TournamentRegistrations', count(*) from "TournamentRegistrations"
union all select 'EaClubSnapshots', count(*) from "EaClubSnapshots"
union all select 'EaMemberStatsSnapshots', count(*) from "EaMemberStatsSnapshots"
union all select 'EaPlayerProfileSnapshots', count(*) from "EaPlayerProfileSnapshots"
union all select 'EaFriendlyMatches', count(*) from "EaFriendlyMatches"
union all select 'EaApiCacheEntries', count(*) from "EaApiCacheEntries"
union all select 'LocalizedContents', count(*) from "LocalizedContents"
order by table_name;

select
    "Id",
    "Email",
    "DisplayName",
    "Gamertag",
    "EaSportsId",
    "EaClubId",
    "EaClubName",
    "IsAdmin",
    "CreatedAt"
from "Users"
where "DisplayName" ilike '%ginga%'
   or "Gamertag" ilike '%ginga%'
   or "EaSportsId" ilike '%ginga%'
   or "EaClubName" ilike '%survivors%'
order by "CreatedAt" desc;

select
    team."Id" as team_id,
    team."Name" as team_name,
    team."EaClubId",
    member."UserId",
    member."Role",
    member."Status"
from "TeamMembers" member
join "Teams" team on team."Id" = member."TeamId"
order by team."Name";

select
    'EaPlayerProfileSnapshots' as source,
    "PlayerName",
    "EaPlayerId",
    "ProName",
    "Position",
    "SyncedAt"
from "EaPlayerProfileSnapshots"
where "PlayerName" ilike '%ginga%'
   or "EaPlayerId" ilike '%ginga%'
   or "ProName" ilike '%ginga%'
order by "SyncedAt" desc;

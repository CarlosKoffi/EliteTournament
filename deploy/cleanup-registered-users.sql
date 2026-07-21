-- TS Tournament cleanup: remove registered users and user-owned data.
--
-- Use this when tests created real accounts but you want to keep:
-- - EF Core migrations
-- - LocalizedContents labels/texts
-- - existing tournament/team structure if it does not depend on users
--
-- IMPORTANT:
-- If Seed__AdminEmail and Seed__AdminPassword are configured, the API can recreate
-- an admin user on startup when no admin exists.

begin;

select 'before' as phase, 'Users' as table_name, count(*) as rows from "Users"
union all select 'before', 'TeamMembers', count(*) from "TeamMembers"
union all select 'before', 'TeamJoinRequests', count(*) from "TeamJoinRequests"
union all select 'before', 'TournamentPlayerConfirmations', count(*) from "TournamentPlayerConfirmations"
union all select 'before', 'UserTournamentAccesses', count(*) from "UserTournamentAccesses";

truncate table
    "TournamentPlayerConfirmations",
    "UserTournamentAccesses",
    "TeamSlotAssignments",
    "TeamSlotPackages",
    "TeamPlayerDemands",
    "TeamJoinRequests",
    "TeamMembers",
    "Users"
restart identity cascade;

select 'after' as phase, 'Users' as table_name, count(*) as rows from "Users"
union all select 'after', 'TeamMembers', count(*) from "TeamMembers"
union all select 'after', 'TeamJoinRequests', count(*) from "TeamJoinRequests"
union all select 'after', 'TournamentPlayerConfirmations', count(*) from "TournamentPlayerConfirmations"
union all select 'after', 'UserTournamentAccesses', count(*) from "UserTournamentAccesses";

do $$
begin
    if exists (select 1 from "Users" limit 1) then
        raise exception 'Registered users cleanup failed: Users table is not empty.';
    end if;
end $$;

commit;

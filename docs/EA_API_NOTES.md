# EA FC Pro Clubs API Notes

EA's Pro Clubs endpoints appear to be unofficial/undocumented. We should treat them as unstable and keep all access behind infrastructure interfaces.

Endpoint patterns carried over from the previous ProClubHub implementation:

- Base URL: `https://proclubs.ea.com/api/fc`
- Club search: `/allTimeLeaderboard/search?platform={platform}&clubName={clubName}`
- Legacy/alternate club search: `/clubs/search?platform={platform}&clubName={clubName}`
- Club info: `/clubs/info?platform={platform}&clubIds={clubId}`
- Club matches: `/clubs/matches?matchType={matchType}&platform={platform}&clubIds={clubId}&maxResultCount={count}`
- Member stats: `/members/stats?platform={platform}&clubId={clubId}`
- Member career stats: `/members/career/stats?platform={platform}&clubId={clubId}`

Known platform value from ProClubHub:

- `common-gen5`

Caching rule:

- Store raw EA responses in `EaApiCacheEntries`.
- Use endpoint-specific cache keys.
- Prefer reading from cache while fresh.
- Refresh in the background or on explicit sync.
- Parse only the fields the app needs, while preserving raw JSON for future parser changes.

Player identity:

- CPElite users now have an optional `EaSportsId` field.
- This is separate from `Gamertag` because EA/FC identifiers may not match visible platform names.

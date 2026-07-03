-- Cleanup for the previous broad auto-extraction seed.
-- Use this only if deploy/seed-localized-content.sql from before 2026-06-30 was already executed.
-- It removes generated keys like home.001..., account.001..., teams.001...
-- It keeps clean semantic keys like nav.home or home.hero.title.

delete from "LocalizedContents"
where "Key" ~ '^(home|account|teams|mercato|tournaments|admin\.tournaments|admin\.tournaments\.new|admin\.tournaments\.tracking|admin\.content|login|ea\.lab)\.[0-9]{3}\.';

# EliteTournament - Deploiement Coolify

Ce document decrit le deploiement web de EliteTournament sur Coolify.

Objectif court terme :

- deployer le site Blazor WebAssembly ;
- deployer l'API ASP.NET Core ;
- utiliser Supabase/PostgreSQL ;
- garder une architecture simple pour la V1 web.

Objectif long terme :

- garder l'API reutilisable pour un futur bot Discord ;
- garder l'API reutilisable pour une future application mobile ;
- ne pas ajouter MAUI maintenant.

## Strategie retenue

La strategie recommandee pour le premier deploiement est un seul container Coolify.

Le container :

1. publie `src/CPElite.Web` en Blazor WebAssembly ;
2. publie `src/CPElite.Api` en ASP.NET Core ;
3. copie le `wwwroot` Blazor publie dans le `wwwroot` de l'API ;
4. sert le site et l'API depuis le meme host ;
5. ecoute sur le port `8080`.

Avantages :

- un seul service Coolify ;
- un seul domaine possible ;
- CORS beaucoup plus simple ;
- API toujours reutilisable plus tard par mobile ou Discord ;
- pas de gros refactor metier.

## Fichiers de deploiement

Fichiers importants :

```text
Dockerfile
.dockerignore
README_DEPLOY.md
deploy/supabase-initial.sql
deploy/supabase-localized-content.sql
deploy/seed-localized-content.sql
deploy/cleanup-auto-localized-content.sql
```

## Prerequis

Dans Coolify :

- un projet Coolify ;
- une application connectee au repo GitHub ;
- un domaine ou sous-domaine ;
- une base Supabase/PostgreSQL deja creee ;
- la connection string PostgreSQL Supabase ;
- une cle JWT longue et secrete.

Repository GitHub :

```text
https://github.com/CarlosKoffi/EliteTournament
```

## Build Coolify

Type de build :

```text
Dockerfile
```

Dockerfile :

```text
Dockerfile
```

Port expose :

```text
8080
```

Health check recommande :

```text
/health
```

## Variables d'environnement

Variables minimales :

```text
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
Database__Provider=Postgres
ConnectionStrings__DefaultConnection=<SUPABASE_POSTGRES_CONNECTION_STRING>
Jwt__Issuer=EliteTournament
Jwt__Audience=EliteTournament
Jwt__Secret=<LONG_RANDOM_SECRET>
Jwt__ExpiryMinutes=60
Seed__AdminEmail=<INITIAL_ADMIN_EMAIL>
Seed__AdminPassword=<INITIAL_ADMIN_PASSWORD>
EaSync__Enabled=true
EaSync__IntervalMinutes=120
EaMatchVerification__Enabled=true
EaMatchVerification__IntervalMinutes=2
```

Si le Web et l'API sont servis par le meme domaine dans ce container, CORS peut rester vide.

Si un autre domaine doit appeler l'API, ajouter :

```text
Cors__AllowedOrigins__0=https://ton-site.com
```

Ou, si Coolify gere plus facilement une valeur texte :

```text
Cors__AllowedOrigins=https://ton-site.com,https://www.ton-site.com
```

Compatibilite :

- `ConnectionStrings__DefaultConnection` est recommande ;
- `ConnectionStrings__Supabase` reste supporte ;
- `Jwt__Secret` est recommande ;
- `Jwt__SigningKey` reste supporte.

Ne jamais mettre les secrets dans Git.

Format attendu pour PostgreSQL Coolify :

```text
Host=<HOST>;Port=5432;Database=<DATABASE>;Username=<USERNAME>;Password=<PASSWORD>
```

Exemple :

```text
Host=postgres;Port=5432;Database=elite_tournament;Username=postgres;Password=...
```

## Configuration Blazor WebAssembly

En production, le fichier :

```text
src/CPElite.Web/wwwroot/appsettings.Production.json
```

peut garder :

```json
{
  "ApiBaseUrl": ""
}
```

Avec une valeur vide, le frontend appelle l'API sur le meme domaine que le site.

Exemple :

```text
https://elite-tournament.com/api/auth/login
```

## Initialisation base de donnees

En production, l'application initialise la base au demarrage avec une methode dediee :

```text
InitializeDatabaseAsync()
```

Cette initialisation fait :

1. application des migrations EF Core avec `MigrateAsync()` ;
2. creation du compte admin initial si aucun admin n'existe ;
3. seed minimal des contenus traduisibles de base ;
4. aucune duplication si l'application redemarre.

Important :

- `EnsureCreated()` n'est jamais utilise en production ;
- les migrations EF Core sont la source de verite ;
- si PostgreSQL est indisponible, l'application log une erreur critique claire ;
- si aucun admin n'existe et que les variables admin sont absentes, le demarrage echoue avec un message explicite.

Variables obligatoires pour le premier demarrage si aucun admin n'existe :

```text
Seed__AdminEmail=<INITIAL_ADMIN_EMAIL>
Seed__AdminPassword=<INITIAL_ADMIN_PASSWORD>
```

Contraintes :

- `Seed__AdminEmail` doit etre un email valide ;
- `Seed__AdminPassword` doit contenir au moins 12 caracteres ;
- ces valeurs ne doivent jamais etre commitees dans Git ;
- si un admin existe deja, aucun admin n'est recree.

Seed automatique minimal :

- compte admin initial ;
- textes fixes minimaux FR/EN pour navigation, accueil, compte et admin contenu.

Verification apres deploiement :

```text
https://ton-domaine.com/health
https://ton-domaine.com/health/db
```

Pour verifier les migrations depuis la base PostgreSQL :

```sql
select * from "__EFMigrationsHistory" order by "MigrationId";
```

La migration la plus recente doit apparaitre dans cette table.

## Base de donnees Supabase/PostgreSQL

Le projet utilise EF Core avec PostgreSQL.

Migrations presentes :

```text
20260628122250_InitialCreate
20260628231049_AddLocalizedContent
20260703143345_AddUserAdminFlag
```

Pour Coolify avec PostgreSQL cree dans Coolify, les migrations sont appliquees automatiquement au demarrage en production.

La migration manuelle reste possible en cas de maintenance ou de debug.

Commande recommandee :

```powershell
dotnet ef database update --project src/CPElite.Infrastructure --startup-project src/CPElite.Api
```

Ou avec le script existant :

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\update-supabase-db.ps1
```

Important :

- ne pas utiliser `EnsureCreated` en production PostgreSQL ;
- preferer les migrations EF Core ;
- garder une sauvegarde avant toute migration importante.

## Contenu traduisible

Seed propre des textes fixes :

```text
deploy/seed-localized-content.sql
```

Ce fichier ne doit contenir que :

- menus ;
- titres ;
- boutons ;
- placeholders ;
- messages generiques ;
- labels.

Il ne doit pas contenir :

- noms de joueurs ;
- noms d'equipes ;
- scores ;
- stats EA ;
- dates de tournoi ;
- prix ;
- places restantes.

Si un ancien seed automatique a ete execute par erreur :

```text
deploy/cleanup-auto-localized-content.sql
```

## Securite production

Les routes demo Blazor :

```text
/demo-login
/demo-logout
```

sont neutralisees en production et redirigent vers l'accueil.

Les diagnostics EA cote API sont proteges par authentification.

En production :

- utiliser une vraie cle JWT longue ;
- restreindre CORS ;
- ne pas exposer les secrets ;
- verifier les droits admin / owner / co-owner avant ouverture publique ;
- proteger les endpoints Discord avec une cle bot dediee avant usage reel.

## Procedure de deploiement Coolify

1. Pousser le code sur GitHub.
2. Creer une application Coolify depuis le repository.
3. Choisir Dockerfile.
4. Configurer le port `8080`.
5. Ajouter les variables d'environnement.
6. Lancer le build.
7. Verifier `/health`.
8. Verifier `/health/db`.
9. Ouvrir le site.
10. Tester login, compte, equipe, tournois, admin contenu.

Endpoints a verifier :

```text
https://ton-domaine.com/health
https://ton-domaine.com/health/db
https://ton-domaine.com/
```

## Procedure de rollback

Dans Coolify :

1. ouvrir l'application ;
2. aller dans Deployments ;
3. choisir le dernier deploiement stable ;
4. cliquer sur Redeploy / Rollback selon l'interface ;
5. verifier `/health` ;
6. verifier les logs.

En cas de probleme DB :

- ne pas supprimer la base ;
- verifier la derniere migration appliquee ;
- restaurer depuis Supabase si necessaire ;
- ne pas lancer de script SQL correctif sans sauvegarde.

## Commandes locales utiles

Build :

```powershell
dotnet build CPElite.sln
```

Tests :

```powershell
dotnet test CPElite.sln
```

Build Docker local :

```powershell
docker build -t elite-tournament .
```

Run Docker local :

```powershell
docker run --rm -p 8080:8080 `
  -e ASPNETCORE_ENVIRONMENT=Production `
  -e ASPNETCORE_URLS=http://+:8080 `
  -e Database__Provider=Postgres `
  -e ConnectionStrings__DefaultConnection="<SUPABASE_POSTGRES_CONNECTION_STRING>" `
  -e Jwt__Issuer=EliteTournament `
  -e Jwt__Audience=EliteTournament `
  -e Jwt__Secret="<LONG_RANDOM_SECRET>" `
  elite-tournament
```

Tester localement :

```text
http://localhost:8080
http://localhost:8080/health
http://localhost:8080/health/db
```

## Notes futures mobile

Ne pas ajouter MAUI maintenant.

La bonne trajectoire :

```text
Web stable
API stable
DB stable
Discord bot
Mobile plus tard
```

L'architecture actuelle permet deja une future application mobile car :

- l'API est separee ;
- les contrats sont dans `CPElite.Contracts` ;
- la logique metier n'est pas dans Blazor ;
- l'auth JWT est reutilisable.

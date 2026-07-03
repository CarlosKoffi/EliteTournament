# Audit du projet EliteTournament

Audit realise sur le dossier local :

```text
C:\Users\bruce\source\repos\CarlosKoffi\CPElite
```

Objectif du document :

- comprendre ce que fait le projet aujourd'hui ;
- identifier la structure technique ;
- identifier les forces, limites et risques ;
- preparer les prochaines etapes produit et techniques.

Ce document concerne le projet lui-meme.  
La partie GitHub / Coolify / deploiement est separee dans `AUDIT_DEPLOY.md`.

## 1. Resume fonctionnel

EliteTournament est une application web pour organiser des tournois EA FC Pro Clubs.

L'objectif produit est de proposer une experience plus complete qu'un simple outil de tournoi :

- creation de compte joueur ;
- rattachement joueur a un club ;
- creation et gestion de clubs par owner / co-owner ;
- recuperation de donnees EA autour des clubs, joueurs et matchs amicaux ;
- inscription des clubs aux tournois ;
- validation de presence des joueurs ;
- tirage des poules puis phase finale ;
- suivi des matchs ;
- gestion des resultats ;
- interaction future avec Discord ;
- contenu administrable et traduisible.

Le projet vise d'abord un site web Blazor propre.  
Une application mobile pourra venir plus tard en reutilisant l'API.

## 2. Stack actuelle

Le projet est en .NET 9.

Solution :

```text
CPElite.sln
```

Technologies principales :

```text
.NET 9
ASP.NET Core Web API
Blazor WebAssembly
Entity Framework Core
PostgreSQL / Supabase
JWT
BCrypt
xUnit
Bootstrap
```

## 3. Structure des projets

La solution contient :

```text
src/CPElite.Api
src/CPElite.Web
src/CPElite.Application
src/CPElite.Contracts
src/CPElite.Domain
src/CPElite.Infrastructure
tests/CPElite.Tests.Unit
tests/CPElite.Tests.Integration
```

Role des projets :

```text
CPElite.Domain
```

Contient les entites metier et enums : users, teams, tournaments, EA snapshots, confirmations, registrations, etc.

```text
CPElite.Application
```

Contient les services applicatifs et interfaces de repositories.

```text
CPElite.Infrastructure
```

Contient EF Core, repositories, clients EA, services background, securite JWT/BCrypt.

```text
CPElite.Contracts
```

Contient les DTO partages entre API et frontend.

```text
CPElite.Api
```

Expose l'API REST ASP.NET Core.

```text
CPElite.Web
```

Frontend Blazor WebAssembly.

```text
tests
```

Tests unitaires et tests d'integration.

## 4. Etat du build

Commande lancee :

```powershell
dotnet build CPElite.sln
```

Resultat :

```text
Build succeeded.
0 Warning(s)
0 Error(s)
```

Conclusion : le projet compile correctement.

## 5. Pages web existantes

Routes Blazor detectees :

```text
/
/login
/account
/teams
/tournaments
/mercato
/actualites
/today
/ea-lab
/admin/content
/admin/tournaments
/admin/tournaments/new
/admin/tournaments/tracking
/demo-login
/demo-logout
```

Pages importantes :

- Accueil ;
- Login ;
- Compte joueur ;
- Mon equipe / clubs ;
- Tournois ;
- Mercato ;
- Admin contenu ;
- Admin tournois ;
- EA Lab ;
- Demo login local.

Les pages demo sont utiles pour tester vite en local, mais ne doivent pas etre accessibles en production.

## 6. Backend actuel

Controllers principaux :

- `AuthController`
- `MeController`
- `TeamsController`
- `TeamAssetsController`
- `TournamentsController`
- `TournamentParticipantsController`
- `DiscordTournamentsController`
- `EaClubsController`
- `EaSyncController`
- `EaDiagnosticsController`
- `ContentController`
- `AccessController`
- `TodayController`

Le backend couvre deja :

- inscription ;
- connexion ;
- JWT ;
- profil joueur ;
- creation equipe ;
- gestion equipe ;
- membres d'equipe ;
- roles ;
- invitations ;
- join requests ;
- upload logo / banniere ;
- demandes de joueur ;
- tournois ;
- inscriptions ;
- liste d'attente ;
- remplacement automatique potentiel ;
- tirage world-cup style ;
- confirmations joueur ;
- verification EA ;
- endpoints Discord ;
- contenu traduisible.

## 7. Frontend actuel

Le frontend est en Blazor WebAssembly.

Il consomme l'API via :

```text
src/CPElite.Web/Services/ApiClient.cs
```

L'auth locale cote web est geree par :

```text
src/CPElite.Web/Services/AuthState.cs
```

Les tokens et infos user sont stockes dans le `localStorage`.

Point positif :

- le frontend est deja separe de l'API ;
- il est compatible avec une future app mobile ;
- les DTO sont partages via `CPElite.Contracts`.

Point a ameliorer :

- plusieurs pages contiennent encore beaucoup de donnees mockees ou de demo ;
- le wording doit etre branche progressivement sur le systeme de contenu traduisible ;
- certains parcours UX doivent etre stabilises avant production.

## 8. Base de donnees

La base est PostgreSQL via Supabase.

EF Core est configure dans :

```text
src/CPElite.Infrastructure/Data/CPEliteDbContext.cs
```

Migrations presentes :

```text
20260628122250_InitialCreate
20260628231049_AddLocalizedContent
```

Entites principales :

- `User`
- `Team`
- `TeamMember`
- `TeamJoinRequest`
- `TeamPosition`
- `TeamScheduleSlot`
- `TeamPlayerDemand`
- `Tournament`
- `TournamentRegistration`
- `TournamentMatch`
- `TournamentPlayerConfirmation`
- `MatchScoreSubmission`
- `ChampionTitle`
- `TournamentMoment`
- `EaClubSnapshot`
- `EaMemberStatsSnapshot`
- `EaPlayerProfileSnapshot`
- `EaFriendlyMatch`
- `EaMatchPlayerStat`
- `EaMatchClubStat`
- `EaApiCacheEntry`
- `LocalizedContent`

Conclusion : le modele DB est deja assez avance pour une V1 web.

## 9. Logique EA

Le projet contient une integration EA / Pro Clubs.

Clients identifies :

```text
DirectEaProClubsClient
EaProClubsClient
ProClubsTrackerClient
EaDiagnosticsClient
```

Services :

```text
EaClubDiscoveryService
EaSyncService
EaDiagnosticsService
```

Donnees stockees :

- snapshot club ;
- stats membres ;
- profils joueurs ;
- matchs amicaux ;
- stats joueurs de match ;
- stats club de match ;
- JSON brut EA en `jsonb`.

Point fort :

- les donnees brutes sont conservees, ce qui permettra d'ameliorer les parsers plus tard sans perdre l'information.

Risque :

- les endpoints EA utilises sont non officiels ou instables ;
- il faudra prevoir une couche d'abstraction forte si EA donne un acces officiel plus tard.

## 10. Logique tournoi

Le projet couvre deja beaucoup de logique tournoi :

- creation de tournoi ;
- type normal / goodies ;
- dates d'inscription ;
- date et heure de tournoi ;
- restrictions joueurs ;
- inscriptions equipe ;
- liste d'attente ;
- validation app ;
- retrait equipe ;
- promotion depuis waitlist ;
- creation de matchs ;
- tirage format poules + phase finale ;
- score submission ;
- verification EA ;
- moments de tournoi.

Le projet va dans la bonne direction pour devenir un vrai outil central de gestion de tournoi.

## 11. Logique equipe / joueur

Le projet gere :

- creation de joueur ;
- EA Sports ID ;
- club EA ;
- compte joueur ;
- photo de profil ;
- creation equipe ;
- owner / co-owner ;
- membres ;
- roles ;
- demande d'adhesion ;
- effectif ;
- logo / banniere ;
- reseaux sociaux ;
- couleur equipe ;
- donnees EA enrichies.

Point a clarifier :

- un joueur doit avoir une seule equipe active ;
- il peut etre prete pour un tournoi ;
- un owner peut posseder plusieurs clubs ;
- les droits owner / co-owner doivent etre verifies partout cote backend, pas seulement cote UI.

## 12. Contenu traduisible

Le projet contient deja une base pour gerer les textes traduisibles :

- entite `LocalizedContent` ;
- controller `ContentController` ;
- page admin `/admin/content` ;
- seed SQL de textes fixes ;
- cleanup SQL pour anciennes cles generees automatiquement.

Regle importante :

- les textes fixes sont traduisibles ;
- les donnees dynamiques ne doivent pas etre traduites ici.

Exemples de textes fixes :

- menus ;
- titres ;
- boutons ;
- placeholders ;
- messages vides ;
- labels.

Exemples de donnees dynamiques :

- noms d'equipes ;
- noms de joueurs ;
- scores ;
- stats EA ;
- dates de tournoi ;
- prix ;
- places restantes.

## 13. Tests

Tests detectes :

```text
tests/CPElite.Tests.Unit
tests/CPElite.Tests.Integration
```

Exemples :

- auth flow ;
- team flow ;
- team management ;
- player demand ;
- tournament flow ;
- diagnostics EA ;
- team member domain.

Point positif :

- les tests existent deja, ce qui est tres bon pour une base SOLID.

Point a faire :

- lancer `dotnet test` regulierement ;
- ajouter des tests sur les nouveaux parcours critiques ;
- tester les autorisations owner / co-owner ;
- tester les restrictions tournoi ;
- tester la logique waitlist.

## 14. Points forts

- Architecture deja propre.
- Separation Domain/Application/Infrastructure/API/Web.
- Build propre.
- Base metier avancee.
- Supabase deja prevu.
- Tests deja presents.
- DTO partages.
- Integration EA encapsulee.
- Bonne direction pour future app mobile.
- Admin contenu deja initie.
- Logique tournoi deja bien avancee.

## 15. Risques techniques

Risques principaux :

- certaines pages web contiennent encore des donnees mockees ;
- routes demo visibles ;
- droits admin/owner a renforcer ;
- endpoints Discord anonymes ;
- stockage uploads local non ideal pour production ;
- dependance EA instable ;
- CORS a securiser ;
- JWT secret a changer ;
- contenu traduisible pas encore branche partout ;
- repo Git local a stabiliser.

## 16. Risques produit

Risques produit :

- l'application essaie de couvrir beaucoup de choses tres vite ;
- le parcours joueur doit rester simple ;
- le parcours owner doit etre ultra clair ;
- l'admin tournoi doit etre separe du parcours public ;
- Discord doit etre un pont temporaire, pas le coeur du produit ;
- la valeur principale doit rester : organiser le tournoi plus facilement que les solutions actuelles.

## 17. Dette technique actuelle

Dette probable :

- pages Blazor longues et riches en logique UI ;
- donnees mockees a remplacer progressivement par API ;
- textes fixes pas encore tous centralises ;
- uploads pas externalises ;
- securite Discord a renforcer ;
- demo-login a isoler en dev only ;
- Docker/Coolify pas encore prepare ;
- naming CPElite / EliteTournament / TS Tournament a harmoniser plus tard.

## 18. Architecture recommandee court terme

Pour maintenant :

```text
Blazor WebAssembly
ASP.NET Core API
Supabase PostgreSQL
```

Ne pas faire MAUI tout de suite.

Objectif prioritaire :

```text
site web stable + API stable + DB propre
```

## 19. Architecture recommandee moyen terme

Ensuite :

```text
Discord Bot -> API
Mobile App -> API
Admin Web -> API
Public Web -> API
```

L'API doit devenir le centre stable.

## 20. Prochaines etapes recommandees

1. Stabiliser Git avec le repo GitHub `EliteTournament`.
2. Nettoyer ou bloquer les routes demo en production.
3. Remplacer les donnees mockees prioritaires par appels API reels.
4. Brancher progressivement les textes fixes sur `LocalizedContent`.
5. Renforcer les droits owner / co-owner / admin.
6. Finaliser le workflow joueur :
   - creer compte ;
   - chercher club ;
   - choisir son joueur EA ;
   - rejoindre le club.
7. Finaliser le workflow owner :
   - creer club ;
   - sync EA ;
   - gerer effectif ;
   - inscrire equipe au tournoi ;
   - selectionner les 11 ;
   - suivre confirmations.
8. Finaliser le workflow tournoi :
   - creation admin ;
   - inscriptions ;
   - liste d'attente ;
   - tirage ;
   - suivi matchs ;
   - resultats.
9. Preparer Coolify dans un document separe.
10. Preparer ensuite le bot Discord.

## 21. Conclusion

Le projet est deja une vraie base applicative, pas seulement une maquette.

Il y a deja :

- une architecture solide ;
- un backend serieux ;
- une base de donnees avancee ;
- un frontend utilisable ;
- des tests ;
- une direction produit claire.

La priorite n'est pas d'ajouter beaucoup de nouvelles features, mais de stabiliser :

- Git ;
- deploiement ;
- auth ;
- donnees reelles ;
- droits ;
- workflow utilisateur ;
- workflow owner ;
- workflow tournoi.

Une fois ces points stabilises, le projet sera bien place pour une premiere V1 web deployable.

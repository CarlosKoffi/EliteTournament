# Audit de preparation GitHub + Coolify

Projet cible : EliteTournament  
Repository GitHub : https://github.com/CarlosKoffi/EliteTournament  
Audit realise sur le dossier local : `C:\Users\bruce\source\repos\CarlosKoffi\CPElite`

## 1. Resume du projet

Le projet est une application .NET 9 deja structuree autour d'une architecture propre :

- un backend ASP.NET Core Web API ;
- un frontend Blazor WebAssembly ;
- une couche Domain ;
- une couche Application ;
- une couche Infrastructure ;
- une couche Contracts partagee ;
- des tests unitaires ;
- des tests d'integration ;
- une base PostgreSQL/Supabase via Entity Framework Core.

Le projet compile correctement avec :

```powershell
dotnet build CPElite.sln
```

Resultat observe :

```text
Build succeeded.
0 Warning(s)
0 Error(s)
```

## 2. Point important Git local

Le dossier contenant le code :

```text
C:\Users\bruce\source\repos\CarlosKoffi\CPElite
```

n'est pas reconnu comme repository Git local au moment de l'audit.

Les commandes suivantes echouent dans ce dossier :

```powershell
git status
git remote -v
```

Erreur observee :

```text
fatal: not a git repository (or any of the parent directories): .git
```

Avant de preparer Coolify, il faut donc aligner le code local avec le repository GitHub `EliteTournament`.

## 3. Stack actuelle

Version .NET :

```text
net9.0
```

Solution :

```text
CPElite.sln
```

Projets principaux :

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

Backend :

```text
ASP.NET Core Web API
JWT
EF Core
Npgsql
Supabase/PostgreSQL
Background services
```

Frontend :

```text
Blazor WebAssembly
Bootstrap present
CSS custom
ApiBaseUrl configurable
```

Base de donnees :

```text
PostgreSQL / Supabase
Entity Framework Core migrations
```

## 4. Pages web existantes

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

Les routes `/demo-login` et `/demo-logout` sont utiles en local, mais ne doivent pas rester exposees en production.

## 5. Fonctionnalites backend deja presentes

Le backend couvre deja :

- inscription et connexion joueur ;
- profil joueur ;
- creation et gestion d'equipes ;
- membres d'equipe ;
- roles owner / co-owner / joueur ;
- demandes de joueur ;
- assets equipe ;
- creation de tournois ;
- inscriptions aux tournois ;
- liste d'attente ;
- tirage type poules puis elimination directe ;
- confirmations de presence des joueurs ;
- slots / acces tournoi ;
- endpoints Discord ;
- synchronisation EA ;
- cache EA ;
- matchs amicaux EA ;
- contenu traduisible depuis l'admin.

## 6. Ce qui fonctionne

- La solution compile.
- L'architecture est deja bien separee.
- Les contrats API sont separes dans `CPElite.Contracts`.
- L'API expose des endpoints metier clairs.
- Supabase est deja prevu.
- Les migrations EF Core existent.
- Des scripts de deploiement DB existent dans `deploy`.
- Les tests unitaires et integration existent.
- Le frontend consomme deja l'API via `ApiBaseUrl`.

## 7. Ce qui ne fonctionne pas encore / a verifier

- Le dossier local audite n'est pas un repo Git.
- Aucun `Dockerfile` n'est present.
- Aucun `.dockerignore` n'est present.
- Pas encore de documentation Coolify dediee.
- Le frontend production contient encore une URL placeholder :

```json
{
  "ApiBaseUrl": "https://YOUR-API-HOST"
}
```

- La cle JWT dans `appsettings.json` est une cle de developpement.
- Les routes demo doivent etre bloquees ou supprimees en production.
- Les endpoints Discord anonymes doivent etre securises.
- Les fichiers uploades dans `wwwroot/uploads` ne sont pas persistants en container sans volume.

## 8. Adaptation Blazor Web / ASP.NET Core

Le projet est bien adapte a Blazor Web + ASP.NET Core.

Architecture actuelle :

```text
Blazor WebAssembly -> API ASP.NET Core -> Supabase PostgreSQL
```

Pour Coolify, deux options existent :

### Option recommandee au debut

Deux services Coolify :

```text
elite-tournament-api
elite-tournament-web
```

Avantages :

- plus simple a comprendre ;
- chaque service a son role ;
- facile a debug ;
- compatible avec une future app mobile ;
- l'API pourra aussi servir le bot Discord.

### Option possible plus tard

Un seul service ASP.NET Core qui sert :

```text
API + Blazor WebAssembly statique
```

Avantage :

- un seul domaine ;
- pas ou peu de CORS ;
- deploiement plus compact.

Inconvenient :

- demande une petite restructuration.

## 9. Coolify : elements necessaires

Il faut ajouter :

- `Dockerfile` pour l'API ;
- `Dockerfile` pour le Web ou une strategie de build statique ;
- `.dockerignore` ;
- `README_DEPLOY.md` ;
- variables d'environnement ;
- configuration port `8080` ;
- configuration CORS ;
- procedure de migration Supabase ;
- procedure de seed du contenu traduisible ;
- desactivation des routes demo en production.

## 10. Port 8080

Coolify attend souvent que l'application ecoute sur un port expose, typiquement `8080`.

Pour l'API :

```text
ASPNETCORE_URLS=http://+:8080
```

Pour le web :

- soit Nginx sert les fichiers Blazor sur `8080` ;
- soit un petit serveur ASP.NET Core sert les fichiers statiques sur `8080`.

## 11. Variables d'environnement API

Variables recommandees pour Coolify :

```text
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
Database__Provider=Postgres
ConnectionStrings__Supabase=<SUPABASE_POSTGRES_CONNECTION_STRING>
Jwt__Issuer=EliteTournament
Jwt__Audience=EliteTournament
Jwt__SigningKey=<LONG_RANDOM_SECRET>
Jwt__ExpiryMinutes=60
EaSync__Enabled=true
EaSync__IntervalMinutes=120
EaMatchVerification__Enabled=true
EaMatchVerification__IntervalMinutes=2
Cors__AllowedOrigins__0=<WEB_APP_URL>
```

Ne jamais mettre de secret dans le repository Git.

## 12. Configuration frontend

Le frontend Blazor doit connaitre l'URL publique de l'API.

Fichier actuel :

```text
src/CPElite.Web/wwwroot/appsettings.Production.json
```

Valeur actuelle a remplacer :

```json
{
  "ApiBaseUrl": "https://YOUR-API-HOST"
}
```

Valeur attendue apres deploiement :

```json
{
  "ApiBaseUrl": "https://api.ton-domaine.com"
}
```

Important : ce fichier est public dans Blazor WebAssembly. Il ne doit contenir aucun secret.

## 13. Base de donnees

Le projet utilise Supabase/PostgreSQL.

Migrations detectees :

```text
20260628122250_InitialCreate
20260628231049_AddLocalizedContent
```

Scripts existants :

```text
deploy/supabase-initial.sql
deploy/supabase-localized-content.sql
deploy/seed-localized-content.sql
deploy/cleanup-auto-localized-content.sql
deploy/update-supabase-db.ps1
```

Pour le premier deploiement, recommandation :

1. garder Supabase comme base externe ;
2. appliquer les migrations manuellement depuis le poste local ;
3. ne pas executer automatiquement les migrations au demarrage de l'application en production au debut.

## 14. Mobile app ready

Le projet est deja pense de maniere compatible avec une future app mobile.

Pourquoi :

- la logique metier n'est pas dans le frontend ;
- l'API est separee ;
- les DTO sont dans `CPElite.Contracts` ;
- l'authentification JWT est reutilisable ;
- une app MAUI/native pourra appeler la meme API plus tard.

Architecture long terme recommandee :

```text
Web Blazor maintenant
API ASP.NET Core maintenant
Discord bot ensuite
Mobile MAUI/native plus tard
```

## 15. Risques techniques

Risques prioritaires :

- repo Git local non aligne avec GitHub ;
- absence de Dockerfile ;
- routes demo presentes ;
- secrets de production a configurer ;
- CORS a regler entre Web et API ;
- stockage des images non persistant en container ;
- endpoints Discord anonymes ;
- dependance EA non officielle et potentiellement instable ;
- contenu de prod a nettoyer des donnees de demo.

## 16. Plan recommande

### Etape 1 : remettre Git propre

Verifier ou se trouve le vrai repository local.

Si necessaire, cloner le repository GitHub :

```powershell
cd C:\Users\bruce\source\repos\CarlosKoffi
git clone https://github.com/CarlosKoffi/EliteTournament.git
cd EliteTournament
```

Puis verifier :

```powershell
git status
git remote -v
git branch
```

### Etape 2 : rapatrier le code si besoin

Si le code actuel est dans `CPElite` mais le repo Git est dans `EliteTournament`, copier le contenu applicatif dans le repo clone.

Elements a copier :

```text
src
tests
deploy
docs
CPElite.sln
.gitignore
```

Ne pas copier :

```text
bin
obj
.vs
TestResults
appsettings.Development.json avec secrets
```

### Etape 3 : verifier le build dans le repo Git

```powershell
dotnet restore
dotnet build
dotnet test
```

### Etape 4 : preparer Coolify

Ajouter ensuite :

```text
Dockerfile.api
Dockerfile.web
.dockerignore
README_DEPLOY.md
```

### Etape 5 : configurer Supabase

Appliquer les migrations :

```powershell
dotnet ef database update --project src/CPElite.Infrastructure --startup-project src/CPElite.Api
```

Ou utiliser le script existant :

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\update-supabase-db.ps1
```

### Etape 6 : deployer API

Dans Coolify :

```text
Service : elite-tournament-api
Port : 8080
Build : Dockerfile API
```

Variables :

```text
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
ConnectionStrings__Supabase=<SUPABASE_CONNECTION_STRING>
Jwt__SigningKey=<LONG_RANDOM_SECRET>
Cors__AllowedOrigins__0=<WEB_APP_URL>
```

Verifier :

```text
https://api.ton-domaine.com/health
https://api.ton-domaine.com/health/db
```

### Etape 7 : deployer Web

Dans Coolify :

```text
Service : elite-tournament-web
Port : 8080
Build : Dockerfile Web
```

Configurer :

```json
{
  "ApiBaseUrl": "https://api.ton-domaine.com"
}
```

### Etape 8 : tests production minimum

Tester :

- ouverture du site ;
- creation de compte ;
- connexion ;
- profil ;
- creation equipe ;
- recherche EA ;
- sync equipe ;
- page tournois ;
- admin contenu ;
- health API ;
- health DB.

## 17. Commandes Git utiles

### Verifier le repo

```powershell
git status
git remote -v
git branch
```

### Initialiser un repo si le dossier n'est pas encore Git

```powershell
git init
git branch -M main
git remote add origin https://github.com/CarlosKoffi/EliteTournament.git
```

### Ajouter les fichiers

```powershell
git add .
git status
```

### Premier commit

```powershell
git commit -m "Initial EliteTournament web and api structure"
```

### Envoyer vers GitHub

Si le repo GitHub est vide :

```powershell
git push -u origin main
```

Si le repo GitHub contient deja des fichiers :

```powershell
git pull origin main --rebase
git push -u origin main
```

### Verifier apres push

```powershell
git status
git log --oneline -5
git remote -v
```

## 18. Commandes .NET utiles

### Restaurer

```powershell
dotnet restore
```

### Compiler

```powershell
dotnet build
```

### Tester

```powershell
dotnet test
```

### Lancer API en local

```powershell
dotnet run --project src/CPElite.Api --urls http://localhost:5088
```

### Lancer Web en local

```powershell
dotnet run --project src/CPElite.Web --urls http://localhost:5041
```

### Verifier l'API locale

```text
http://localhost:5088/health
http://localhost:5088/health/db
```

### Ouvrir le site local

```text
http://localhost:5041
```

## 19. Decision recommandee maintenant

La prochaine action logique :

1. creer ou cloner le vrai repo local `EliteTournament` ;
2. s'assurer que le code compile depuis ce dossier ;
3. ajouter les fichiers Docker/Coolify ;
4. deployer d'abord l'API ;
5. deployer ensuite le Web.

Il vaut mieux ne pas commencer par MAUI maintenant. Le bon chemin est :

```text
Web propre -> API stable -> Coolify -> Supabase propre -> Discord bot -> mobile plus tard
```

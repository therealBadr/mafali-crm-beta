# Mafali CRM


**Technologies** : .NET 8 · ASP.NET Core · Blazor Server · Entity Framework Core 8 · PostgreSQL

---

## Démarrage rapide

Le plus court chemin — restaurer l'échantillon fourni, qui contient le schéma
et des données réelles (405 clients, 25 245 interactions, 72 pièces jointes) :

```bash
createdb -U postgres -E UTF8 mafali_crm_db && pg_restore -U postgres -d mafali_crm_db echantillon/echantillon_mafali.dump
```

Puis renseignez le mot de passe PostgreSQL dans
`webapp/appsettings.Development.json` et lancez l'application. Voir
**[echantillon/README.md](echantillon/README.md)**.

Pour partir d'une base **vide**, voir
**[docs/01-installation.md](docs/01-installation.md)**. En résumé :

```bash
# 1. Créer la base
createdb -U postgres mafali_crm_db

# 2. Appliquer les scripts SQL dans l'ordre numérique (voir doc pour 02_)
psql -U postgres -d mafali_crm_db -f database/01_tables.sql
psql -U postgres -d mafali_crm_db -f database/03_cle_opl_sequence.sql
# ... puis 04 à 28

# 3. Renseigner le mot de passe PostgreSQL
#    dans webapp/appsettings.Development.json

# 4. Lancer
dotnet run --project webapp --launch-profile http
```

L'application répond sur **http://localhost:5297**.

> **Première connexion** : les comptes sont créés sans mot de passe. Le mot de
> passe que vous saisissez à votre **première** connexion devient votre mot de
> passe. Voir [docs/02-connexion-et-roles.md](docs/02-connexion-et-roles.md).

---

## Documentation

| | Document | Pour qui |
|---|---|---|
| 01 | [Installation et lancement](docs/01-installation.md) | celui qui installe |
| 02 | [Connexion, comptes et rôles](docs/02-connexion-et-roles.md) | **tout le monde** |
| 03 | [Les écrans, un par un](docs/03-les-ecrans.md) | testeurs |
| 04 | [Guide de test](docs/04-guide-de-test.md) | **testeurs** |
| 05 | [Architecture technique](docs/05-architecture-technique.md) | développeurs |
| 06 | [Base de données](docs/06-base-de-donnees.md) | développeurs |
| 07 | [Syntaxe des filtres](docs/07-syntaxe-des-filtres.md) | testeurs et développeurs |

Index détaillé : **[docs/README.md](docs/README.md)**

---

## Contenu du dépôt

```
MafaliCrm.sln              Solution Visual Studio
webapp/                    L'application (projet unique)
  Program.cs               Démarrage, injection de dépendances, authentification
  Data/AppDbContext.cs     Modèle Entity Framework
  Models/                  Les 11 entités de la base
  Services/                Logique métier (35 fichiers)
  Components/
    Layout/                Gabarit général et menu de navigation
    Pages/                 Les 25 écrans
    Shared/                Composants réutilisables (dates, heures, popups…)
  wwwroot/                 CSS, JavaScript, polices
database/                  Les 28 scripts SQL, à appliquer dans l'ordre
echantillon/               Échantillon + scripts pour en refaire un
docs/                      Cette documentation
```

## Ce qui n'est pas dans ce dépôt

- **Les outils de reprise de données HFSQL → PostgreSQL** (export/import depuis
  l'ancienne application). Ils sont gérés séparément.
- **`webapp/AppData/`** — créé automatiquement au démarrage. Contient les clés
  de chiffrement des cookies et les modèles Excel de Bon de Commande déposés
  par les utilisateurs. Données d'exécution, volontairement non versionnées.

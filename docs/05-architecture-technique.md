# 05 — Architecture technique

[← Retour à l'index](README.md)

Pour qui doit reprendre ou modifier le code.

---

## 1. Pile technique

| Couche | Choix |
|---|---|
| Plateforme | .NET 8 (`net8.0`), `Nullable` et `ImplicitUsings` activés |
| Web | ASP.NET Core |
| Interface | **Blazor Server** (mode `InteractiveServer`) |
| Accès aux données | Entity Framework Core 8 + Npgsql |
| Base | PostgreSQL |
| Authentification | cookie ASP.NET Core + `PasswordHasher<User>` (PBKDF2) |

### Dépendances NuGet — cinq, toutes justifiées

| Paquet | Usage |
|---|---|
| `Npgsql.EntityFrameworkCore.PostgreSQL` | fournisseur EF Core pour PostgreSQL |
| `Microsoft.EntityFrameworkCore.Design` | outillage de conception (hors exécution) |
| `Microsoft.Extensions.Identity.Core` | **uniquement** `PasswordHasher<User>` — pas l'ensemble d'ASP.NET Identity |
| `ClosedXML` | lecture et écriture des fichiers Excel |
| `NodaTime` | fuseaux horaires (voir § 4.3) |
| `System.Linq.Dynamic.Core` | construction de prédicats LINQ à partir du texte des filtres |

**Aucune chaîne d'outils front-end.** Pas de Node.js, pas de npm, pas d'étape de
compilation JavaScript ou CSS. Le CSS et le JavaScript sont écrits à la main et
servis tels quels. Les polices sont embarquées dans `wwwroot/fonts/`.

---

## 2. Ce que « Blazor Server » implique

C'est la caractéristique qui explique le plus de choix du code. À lire avant de
modifier quoi que ce soit.

- L'interface **s'exécute sur le serveur**. Le navigateur reçoit du HTML initial,
  puis ouvre une connexion SignalR permanente — le *circuit*. Les événements
  (clic, saisie) partent au serveur ; celui-ci renvoie des **différences de
  HTML**.
- **Les données ne quittent pas le serveur.** Il n'y a pas d'API REST, pas de
  JSON sur le réseau, pas de modèle de données côté client. C'est pourquoi
  certaines requêtes lisent l'entité complète sans projection : il n'y a aucun
  coût de transport à optimiser.
- **Une « portée » (*scope*) d'injection de dépendances vaut une session de
  navigateur**, pas une requête HTTP. Un service `Scoped` vit aussi longtemps
  que l'onglet reste ouvert. Conséquence majeure, voir § 4.1.
- **Ce qu'un composant garde en mémoire reste sur le serveur** tant que l'onglet
  est ouvert. Le coût est donc par **utilisateur**, non par requête. Charger
  87 000 lignes dans un champ de composant, c'est 87 000 lignes retenues pour
  chaque personne qui ouvre l'écran.
- Les interactions à haute fréquence (glisser pour redimensionner une colonne)
  doivent rester **côté navigateur** : un aller-retour SignalR par pixel serait
  visiblement lent. D'où les modules JavaScript du § 6.

---

## 3. Organisation du projet

```
webapp/
  Program.cs                  démarrage : liaison réseau, DI, authentification,
                              autorisation, points d'entrée /login et /logout
  appsettings.json            journalisation
  appsettings.Development.json  + chaîne de connexion (à renseigner)

  Data/
    AppDbContext.cs           modèle EF : 11 DbSet, clés, relations, filtre global

  Models/                     les 11 entités, générées par scaffolding puis
                              annotées à la main

  Services/                   35 fichiers — toute la logique métier
  Components/
    App.razor                 document HTML, feuilles de style, scripts
    Routes.razor              routeur + traitement du « non autorisé »
    _Imports.razor            `@using` communs à tous les composants
    Layout/
      MainLayout.razor        bandeau, menu, construction par rôle
      BlankLayout.razor       gabarit nu (écran de connexion)
    Pages/                    25 écrans — un `.razor` + un `.razor.css`
    Shared/                   8 composants réutilisables
  wwwroot/
    app.css                   système de visuel global
    app.js                    4 modules d'interopérabilité JavaScript
    fonts/                    polices embarquées
```

### Conventions

- **Un écran = un `.razor` + un `.razor.css`.** Le CSS est *scopé* par Blazor :
  il ne s'applique qu'au composant. Le style transversal est dans `app.css`.
- **Les noms d'écran et de champ sont en français**, comme dans l'ancienne
  application, pour que le vocabulaire soit le même entre les utilisateurs, le
  code et la base.
- **Les entités reprennent le nommage PostgreSQL** via `[Table]` et `[Column]` :
  `france_optique` → `FranceOptique`, `raison_sociale` → `RaisonSociale`.
- **Chaque service est enregistré dans `Program.cs`** et injecté dans les
  composants par `@inject`.

---

## 4. Les quatre décisions structurantes

### 4.1 `AddDbContextFactory`, et non `AddDbContext`

```csharp
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
```

Un `AppDbContext` injecté en `Scoped` vivrait toute la session du navigateur, et
garderait donc **un seul traqueur de changements** pendant des heures. Un
`SaveChanges` en échec laisse son entité dans le traqueur, ce qui peut faire
échouer une opération ultérieure sans rapport.

Chaque service demande donc un contexte neuf par opération :

```csharp
await using var db = await _dbFactory.CreateDbContextAsync();
```

**À respecter** dans tout nouveau service. Ne pas injecter `AppDbContext`
directement.

### 4.2 Filtre global sur les clients refusés

```csharp
entity.HasQueryFilter(c => c.ValidationStatus != "refused");
```

EF Core ajoute cette condition à **toutes** les requêtes sur `FranceOptiques`,
y compris celles écrites plus tard. Un client refusé est donc invisible partout
par construction, sans qu'aucun appelant ait à y penser.

Deux endroits en sortent explicitement, avec `IgnoreQueryFilters()` :
`ClientValidationService.ListByAdderAsync` (« Mes Clients Ajoutés » : un
commercial doit voir que son client a été refusé, et pourquoi) et
`GetIncludingRefusedAsync` (consultation en lecture seule d'un client déjà
tranché).

À comparer avec `historique.deleted_at`, qui répète `DeletedAt == null` sur neuf
appels. Les deux approches coexistent : la première sur la table lue depuis une
quarantaine d'endroits, la seconde là où les appels sont peu nombreux.

> Deux avertissements EF au démarrage viennent de ce filtre (`FranceOptique`
> est le côté requis des relations venant de `Historique` et `Ca`). Analysés et
> laissés tels quels : ni `HistoriqueService` ni `CaService` ne chargent la
> navigation vers le client, donc le cas visé ne se produit pas.

### 4.3 Fuseaux horaires : NodaTime, pas le système d'exploitation

`Services/AppTimeZones.cs` est l'unique autorité en matière de fuseau horaire,
et **ne consulte pas** les règles du système hôte.

La raison : `TimeZoneInfo` prend les règles de changement d'heure dans le
registre Windows, donc la justesse dépend de l'état de mise à jour de la
machine. Le déploiement réel est un poste de bureau qui ne sera pas tenu à jour.
Le Maroc est précisément le cas problématique — passage à UTC+1 permanent le
20 septembre 2026.

NodaTime embarque sa propre copie de la base IANA : les règles voyagent avec le
binaire, identiques sur tous les postes, et leur mise à jour est une montée de
version NuGet volontaire. `AppTimeZones.TzdbVersion` expose la version en
vigueur.

Le fuseau de l'utilisateur est lu **dans le navigateur** (`BrowserClockProvider`
→ `UserClock`), une fois par onglet. `UserClock` remplace `DateTime.Now` pour
tout ce qui relève du « quelle heure est-il pour moi ». **Les horodatages
d'audit restent en UTC serveur**, volontairement.

### 4.4 Rappels : un service d'arrière-plan et trois pièces

Seul traitement de l'application qui tourne sans requête ni circuit.

| Pièce | Durée de vie | Rôle |
|---|---|---|
| `RappelScanService` | `IHostedService` (singleton) | examine périodiquement les rappels dus ; ouvre sa **propre portée DI** à chaque passage pour atteindre les services `Scoped` |
| `ConnectedUserRegistry` | `Singleton` | table partagée des sessions connectées, lisible depuis un circuit comme depuis le fil d'arrière-plan |
| `ConnectedUserCircuitHandler` | `Scoped`, enregistré sur le type **`CircuitHandler`** | une instance par circuit ; c'est par là que chaque circuit s'inscrit dans la table |
| `CircuitContext` | `Scoped` | donne à un composant l'identifiant de son propre circuit — Blazor n'offre pas d'API publique pour cela |
| `RappelPopupDispatcher` | `Singleton` | achemine la notification vers les circuits abonnés |

`CircuitContext` existe pour une raison précise : la livraison doit être décidée
**par écran**, pas par personne. Blazor garde un circuit interrompu en vie un
certain temps pour permettre la reprise, et l'envoi vers ce circuit réussit
toujours — sans cet identifiant, un utilisateur ayant un onglet vivant et un
onglet abandonné recevait la notification deux fois.

> `ConnectedUserCircuitHandler` doit être enregistré **sur le type de base
> `CircuitHandler`** : c'est ce que Blazor résout. Enregistré sur la classe
> concrète, il ne serait jamais appelé.

---

## 5. Authentification et autorisation

### Pourquoi `/login` est un point d'entrée minimal et non un composant

`HttpContext.SignInAsync` a besoin d'une réponse HTTP réelle et modifiable. Dès
qu'un écran devient interactif, le circuit SignalR a pris le relais et cette
réponse n'existe plus. La connexion ne peut donc pas se faire depuis un
gestionnaire d'événement Blazor : `Login.razor` poste vers un véritable
`<form method="post">` traité par `app.MapPost("/login", …)` dans `Program.cs`.

Le point d'entrée désactive la protection anti-contrefaçon (un jeton est lié à
l'identité en vigueur au moment où il est produit, ce qui casse le retour sur
`/login` quand on est déjà connecté) et autorise l'anonyme — c'est sa raison
d'être.

### Tout est fermé par défaut

```csharp
options.FallbackPolicy = new AuthorizationPolicyBuilder()
    .AddRequirements(new AuthenticatedUnlessBlazorInfrastructure())
    .Build();
```

Plutôt que d'ajouter `[Authorize]` à chaque page, une règle globale exige
l'authentification sur tout point d'entrée. L'exigence sur mesure
(`AuthenticatedUnlessBlazorInfrastructure`, dans
`Services/FrameworkAssetAuthorization.cs`) laisse passer `/_framework` et
`/_blazor`, sans quoi le moteur Blazor lui-même ne pourrait pas se charger
avant la connexion. L'exemption est dans la règle plutôt que dans un
intergiciel, et ne desserre rien d'autre.

Les restrictions de rôle sont déclarées page par page. Les actions sensibles
vérifient en plus côté serveur : `UserService.EnsureAdminAsync`,
`ClientImportExportService` pour l'import.

### Persistance des clés de chiffrement

```csharp
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(..., "AppData", "DataProtectionKeys")))
    .SetApplicationName("MafaliCrm");
```

Sans cela, ASP.NET Core produit une clé en mémoire à chaque démarrage, ce qui
invalide tous les cookies existants : tout le monde est déconnecté à chaque
redémarrage. **Ce dossier doit être conservé et sauvegardé.**

---

## 6. Interopérabilité JavaScript — `wwwroot/app.js`

Quatre modules, chacun pour une chose que Blazor Server ne peut pas faire seul.

| Module | Rôle |
|---|---|
| `mafaliScroll` | signale que l'utilisateur approche du bas d'un conteneur, pour charger la suite. Remplace `<Virtualize ItemsProvider>`, qui n'appelait jamais son fournisseur dans cette application |
| `mafaliFileDownload` | déclenche un véritable téléchargement. Blazor Server ne peut que pousser des octets dans le circuit ; le module en fait un `Blob` et un clic synthétique sur `<a download>` |
| `mafaliColumnResize` | redimensionnement des colonnes à la souris, **entièrement côté navigateur**. Seule la largeur finale remonte au serveur, en un aller-retour |
| `mafaliBonCommandeDestination` | dossier de destination des bons de commande, via l'API *File System Access*. **Chrome et Edge seulement**, détecté à l'exécution. L'autorisation est stockée dans l'IndexedDB du navigateur |

Le redimensionnement de la hauteur des grilles est également dans ce fichier.

---

## 7. Mise en page des grilles

Toutes les grilles de type `.header-row` / `.row` partagent la logique de
`Services/GridLayout.cs` : un conteneur dimensionné à la somme des largeurs de
colonnes, de sorte qu'une barre de défilement horizontale apparaisse au lieu de
voir les colonnes se réorganiser. C'est voulu : ce sont des grilles à colonnes
redimensionnables, comme dans un tableur.

Le conteneur utilise `width: 100%; min-width: <total>px` (il remplit quand il y
a de la place, défile sinon) et la dernière colonne reçoit `flex: 1 1 Npx` pour
absorber l'espace restant, les autres gardant `flex: 0 0 Npx`.

Classes utilitaires associées : `GridFilter`, `GridSort`, `ConflictCheck`,
`FriendlyError`.

---

## 8. Inventaire des services

### Données clients et historique
| Service | Rôle |
|---|---|
| `ClientService` | lecture, création, modification et suppression d'un client |
| `ClientGridQueryService` | requêtes paginées, triées et filtrées **en SQL** pour la grille |
| `ClientValidationService` | circuit de validation des clients créés par un commercial |
| `ClientImportExportService` | import et export Excel (import réservé aux administrateurs) |
| `HistoriqueService` | les interactions ; projection explicite colonne par colonne, hors pièces jointes |
| `CaService` | chiffre d'affaires par client et par année |

### Filtres
| Service | Rôle |
|---|---|
| `FiltreReelParser` | analyse la syntaxe héritée `Filtre_Réel` |
| `ClientFilterService` | traduit un filtre client en prédicat LINQ |
| `HistoriqueFilterService` | idem pour l'historique |
| `FiltreOperatriceService` | les filtres clients enregistrés |
| `FiltreHistoriqueService` | les filtres d'historique enregistrés |

### Données de référence
`FamilleService` · `FranchiseService` · `PaysService` · `AssistanteService`

### Comptes et session
`UserService` · `CurrentUserService` · `ConnectedUserRegistry` ·
`ConnectedUserCircuitHandler` · `CircuitContext` ·
`FrameworkAssetAuthorization`

### Rappels et temps
`RappelService` · `RappelScanService` · `RappelPopupDispatcher` ·
`RappelDueTime` · `UserClock` · `AppTimeZones`

### Bon de commande et suivi
`BonCommandeTemplateService` · `BonCommandeEnCoursService` ·
`DevisEnCoursService`

### Utilitaires
`GridLayout` · `GridFilter` · `GridSort` · `ConflictCheck` · `FriendlyError` ·
`MagasinFermeValues`

---

## 9. Points d'attention pour une modification

1. **Ne pas injecter `AppDbContext`.** Passer par `IDbContextFactory`.
2. **Ajouter une colonne à `Historique`** demande de l'ajouter **aussi** dans
   `HistoriqueService.WithoutFicStk`, la projection explicite utilisée par
   toutes les grilles. Oubliée là, la valeur est correcte en base et vide à
   l'écran — une erreur silencieuse et convaincante.
3. **Les champs de référence doivent être des listes déroulantes**, jamais du
   texte libre, partout où l'écran peut réécrire la valeur dans
   `france_optique`. Du texte libre y enregistrerait une faute de frappe.
4. **Les interactions à haute fréquence restent côté navigateur.**
5. **La valeur d'une liste restreinte se déclare une fois.** Exemple :
   `MagasinFermeValues` partage la liste entre le constructeur de filtres et la
   validation d'import, en accord avec la contrainte `CHECK` en base.
6. **Les horodatages d'audit sont en UTC serveur** ; tout ce qui est « heure
   pour l'utilisateur » passe par `UserClock`.
7. Les colonnes `timestamp without time zone` **refusent** un `DateTime` de
   `Kind = Utc`. `UserService.NowForDb()` montre le contournement.

---

[← Précédent : Guide de test](04-guide-de-test.md) · [Retour à l'index](README.md) · [Suivant : Base de données →](06-base-de-donnees.md)

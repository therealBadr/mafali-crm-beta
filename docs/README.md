# Documentation — Mafali CRM

## Où commencer ?

**Je dois installer l'application**
→ [01 — Installation et lancement](01-installation.md)

**Je dois tester l'application**
→ [02 — Connexion, comptes et rôles](02-connexion-et-roles.md) (indispensable :
explique pourquoi les comptes n'ont pas de mot de passe au départ)
→ puis [04 — Guide de test](04-guide-de-test.md)
→ gardez [03 — Les écrans](03-les-ecrans.md) ouvert à côté comme référence

**Je dois reprendre le code**
→ [05 — Architecture technique](05-architecture-technique.md)
→ puis [06 — Base de données](06-base-de-donnees.md)

---

## Les sept documents

### [01 — Installation et lancement](01-installation.md)
Prérequis, création de la base PostgreSQL, ordre d'application des 28 scripts
SQL, chaîne de connexion, lancement en développement et déploiement sur le
réseau local.

### [02 — Connexion, comptes et rôles](02-connexion-et-roles.md)
Comment fonctionnent les comptes (ils sont créés **sans mot de passe** : la
première connexion le définit), le verrouillage après 5 échecs, les trois rôles
`admin` / `assistant` / `commercial`, et le tableau complet des accès par écran.

### [03 — Les écrans, un par un](03-les-ecrans.md)
Les 25 écrans regroupés comme dans le menu, avec pour chacun son rôle, son
adresse et qui peut y accéder. Correspondance avec les fenêtres de l'ancienne
application WinDev quand elle existe.

### [04 — Guide de test](04-guide-de-test.md)
Scénarios de test par domaine fonctionnel, pièges connus et comportements
volontaires qui peuvent surprendre (clients refusés invisibles, suppression
réversible de l'historique, cases à cocher sans effet…). **À lire avant de
signaler une anomalie.**

### [05 — Architecture technique](05-architecture-technique.md)
Pile technique, organisation du projet, inventaire des services, et les
décisions de conception qui expliquent pourquoi le code est écrit ainsi
(fabrique de contexte EF, filtre global, gestion des fuseaux horaires,
rappels en arrière-plan).

### [06 — Base de données](06-base-de-donnees.md)
Les tables, les clés, les relations, et le détail des 28 scripts de migration :
lesquels sont nécessaires à l'application et lesquels existent seulement pour
la reprise de données.

### [07 — Syntaxe des filtres](07-syntaxe-des-filtres.md)
Le langage de filtre repris de l'ancienne application (champ `Filtre_Réel`) :
opérateurs, champs disponibles, format des dates, joker `%`, et exemples réels
commentés.

---

## Conventions de cette documentation

- Les chemins de fichiers sont donnés depuis la racine du dépôt :
  `webapp/Services/ClientService.cs`.
- Les noms d'écran sont ceux affichés dans l'application, en français
  (« Parcours Client », « Définir un Filtre »).
- Les noms de tables et de colonnes sont ceux de PostgreSQL, en minuscules
  avec des tirets bas (`france_optique`, `raison_sociale`).
- Les blocs marqués **⚠ À savoir** signalent un comportement volontaire qui
  peut être pris à tort pour une anomalie.

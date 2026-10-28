# Échantillon

Échantillon de la base de production, destiné à faire tourner l'application
sans disposer des 58 Go de données réelles.

| | |
|---|---|
| Fichier | `echantillon_mafali.dump` (3,3 Mo, format `pg_dump -Fc`) |
| Contenu | schéma complet **et** données, pièces jointes incluses |
| Produit depuis | `mafali_crm_db_new`, PostgreSQL 14 |
| Encodage | UTF8, collation `French_France.1252` |

---

## Restaurer

Deux commandes :

```bash
createdb -U postgres -E UTF8 mafali_crm_db
pg_restore -U postgres -d mafali_crm_db echantillon/echantillon_mafali.dump
```

Le dump contient le schéma : **il n'est pas nécessaire d'appliquer les 28
scripts de `database/`**. Ceux-ci restent la référence de l'historique du
schéma, et la marche à suivre pour partir d'une base vide est dans
[../docs/01-installation.md](../docs/01-installation.md).

Ensuite, renseignez `webapp/appsettings.Development.json` avec le nom de la base
choisie, et lancez l'application.

### Se connecter

**Tous les comptes ont un mot de passe vide**, ce qui veut dire que le mot de
passe saisi à la première connexion devient le mot de passe du compte. Aucun
hash réel n'est livré.

| Rôle | Compte à utiliser |
|---|---|
| `admin` | `BADR BADR` |
| `commercial` | `Bruno` |

La liste complète est dans la table `users` (22 comptes réels : 5 `admin`,
17 `commercial`).

**Il n'existe aucun compte de rôle `assistant`** — il n'en existe pas en
production non plus. Pour tester ce rôle, connectez-vous en administrateur et
créez-en un depuis **Mon Compte → Utilisateurs** : cela exerce au passage
l'écran d'administration des comptes.

---

## Ce que contient l'échantillon

| Table | Lignes | |
|---|---|---|
| `france_optique` | 405 | clients |
| `historique` | 25 245 | interactions, dont 72 avec pièce jointe |
| `ca` | 687 | chiffre d'affaires |
| `filtre_operatrice` | 81 | filtres enregistrés réels |
| `type_franchise` | 88 | **toutes** les franchises |
| `type_famille` | 22 | **toutes** les familles |
| `assistantes` | 16 | **toutes** |
| `pays` | 13 | **tous** |
| `users` | 22 | 5 `admin`, 17 `commercial`, 0 `assistant` |
| `parametrage` | 5 | |
| `filtre_historique` | 0 | vide en production aussi |
| `login_history` | 0 | volontairement exclu, se remplit aux connexions |

Contenu exploitable pour tester : 401 clients avec un rappel (130 en retard,
271 à venir), 34 avec `rappel_rdv`, 22 bloqués, 78 avec des franchises
multiples, 13 avec un numéro d'adhérent, 56 avec une raison sociale accentuée,
et les 81 filtres enregistrés réels.

Les tables de référence sont livrées **en entier** : elles sont minuscules, et
les prendre toutes évite toute violation de clé étrangère au chargement.

### Pièces jointes

72 fichiers, 2,8 Mo au total, dans `historique.fic_stk` (colonne `BYTEA`) — pas
dans des fichiers séparés. Elles voyagent donc dans le dump.

| Type | Nombre |
|---|---|
| xlsx | 18 |
| jpg | 17 |
| png | 17 |
| pdf | 16 |
| html | 2 |
| tif | 2 |

Seules les pièces de moins de 400 ko sont retenues : les 405 clients de
l'échantillon portent 4,5 Go de pièces jointes en production, et l'export en
hexadécimal double le volume. Sur les lignes non retenues, `fic_stk` **et**
`fic_stk_nom` valent `NULL` ensemble — un nom de fichier sans contenu
afficherait une pièce jointe introuvable.

### Comment les clients ont été choisis

Pas un `LIMIT 405` : un panier par caractéristique à tester, puis tout ce qui
pend sous ces clients (historique, chiffre d'affaires). Les paniers couvrent
les pièces jointes, un historique fourni, les rappels en retard et à venir,
`rappel_rdv`, les clients bloqués, le chiffre d'affaires, les franchises 2 à 4,
les numéros d'adhérent, les raisons sociales accentuées, chaque combinaison de
statuts, et les clients sans assistante commerciale.

Les clients à rappel sont choisis parmi ceux dont `assistante_commercial`
correspond à un compte réel : sinon les écrans par utilisateur (Rappels, Devis
en cours, filtres par opératrice) resteraient vides.

---

## Aucune donnée inventée

**Tout ce que contient cet échantillon vient de la production, tel quel.**
Clients, adresses, notes, historique, pièces jointes, chiffre d'affaires,
filtres enregistrés, comptes, rôles : rien n'a été fabriqué ni modifié.

Une seule exception, et ce n'est pas de la donnée métier : **les mots de passe
sont vidés.** Les hash PBKDF2 réels des comptes du personnel ne sont pas livrés,
parce que c'est du matériel d'authentification. Un `password_hash` vide
déclenche le mécanisme déjà prévu par l'application — la première connexion
définit le mot de passe. Logins, rôles et `is_active` sont ceux de la
production.

`02_completer.sql` écarte par ailleurs les lignes d'historique et de chiffre
d'affaires dont le client est absent (voir « Deux constats sur la production »
ci-dessous). C'est un filtrage, pas une invention : aucune ligne n'est modifiée.

---

## Ce que l'échantillon ne permet pas de tester

Ce n'est pas une limite de l'échantillon, c'est l'état réel de la production :
ces colonnes sont **vides sur les 87 383 clients**, personne ne les a jamais
remplies. Les écrans et champs correspondants s'afficheront donc vides.

| Fonctionnalité | Colonne concernée | Lignes en production |
|---|---|---|
| Circuit de validation des clients | `validation_status` = `pending` / `refused` | 0 — tous `validated` |
| Pas d'emailing | `pas_emailing` | 0 |
| État du magasin | `magasin_ferme` | 0 |
| SIRET / SIREN / TVA | `siret`, `siren`, `tva` | 0 (migration 7) |
| Second courriel | `email2` | 0 (migration 23) |
| Suppression réversible de l'historique | `historique.deleted_at` | 0 |
| Rôle assistante | `users.role` = `assistant` | 0 — 5 `admin`, 17 `commercial` |

Pour exercer ces fonctions, créez vous-mêmes les données depuis l'application —
c'est d'ailleurs le meilleur test. Par exemple : connectez-vous en commercial et
ajoutez un client, il arrivera en `pending` dans « Clients à valider » ; ou
renseignez un SIRET depuis Parcours Client ; ou supprimez une ligne d'historique
et vérifiez en base qu'elle porte un `deleted_at`.

Le compte `assistant` se crée depuis **Mon Compte → Utilisateurs** en tant
qu'administrateur.

---

## Deux constats sur la production

Découverts en constituant l'échantillon, et qui ne concernent pas l'application
mais les données :

**172 481 lignes d'`historique` (environ 16 %) référencent un `num_client` qui
n'existe pas dans `france_optique`** — 15 474 numéros de clients absents. Même
problème sur `ca` : 397 lignes pour 263 clients inexistants.

**Les quatre clés étrangères de `02_foreign_keys.sql` sont restées `NOT VALID`**
en production, ce qui explique que PostgreSQL ne l'ait jamais signalé. Un
`VALIDATE CONSTRAINT` sur la base de production échouerait aujourd'hui.

L'échantillon, lui, est propre : ses 8 clés étrangères sont posées **et
validées**, et `01_extraire.sql` comme `02_completer.sql` écartent les lignes
orphelines.

---

## Refaire un échantillon

Les deux scripts sont livrés pour que vous puissiez en produire un autre — plus
gros, ou ciblé autrement — sans repasser par nous.

```bash
# 1. extraction (LECTURE SEULE sur la production : ne crée que des tables
#    temporaires, n'écrit rien). Produit un CSV par table dans data/.
psql -U postgres -d mafali_crm_db_new -f echantillon/01_extraire.sql
```

```bash
# 2. base neuve avec le schéma, SANS 02_foreign_keys.sql (il vient en dernier)
createdb -U postgres -E UTF8 mafali_echantillon
```

```bash
# 3. charger, dans cet ordre (références avant france_optique, lui-même avant historique et ca)
cd echantillon/data && for t in type_famille type_franchise pays assistantes users parametrage france_optique historique ca filtre_operatrice; do psql -U postgres -d mafali_echantillon -c "\copy $t FROM '$t.csv' CSV"; done
```

```bash
# 4. nettoyage des orphelins + neutralisation des mots de passe
psql -U postgres -d mafali_echantillon -f echantillon/02_completer.sql
```

```bash
# 5. clés étrangères, en dernier
psql -U postgres -d mafali_echantillon -f database/02_foreign_keys.sql
```

```bash
# 6. produire le fichier
pg_dump -U postgres -Fc -Z9 -f echantillon/echantillon_mafali.dump mafali_echantillon
```

### Trois pièges

**Forcez `PGCLIENTENCODING=UTF8`** avant toute chose, et ne mettez jamais de
caractère accentué dans un `psql -c` : selon le terminal, les accents arrivent
en Latin-1 et PostgreSQL rejette la requête. Mettez le SQL dans un fichier
encodé en UTF-8 et utilisez `psql -f`. C'est la raison des `chr(233)` dans
`02_completer.sql`.

**Le chargement est positionnel.** `\copy ... FROM ... CSV` affecte les colonnes
dans l'ordre de la table. Les deux bases doivent avoir exactement les mêmes
colonnes dans le même ordre — c'est le cas ici, vérifié. Si une migration est
ajoutée d'un côté seulement, les données partiraient silencieusement dans les
mauvaises colonnes.

**`02_foreign_keys.sql` en dernier.** Il crée les contraintes en `NOT VALID`,
donc appliqué sur une base vide avant le chargement il perdrait son intérêt et
rejetterait les lignes orphelines. Pour vérifier la cohérence du résultat,
validez-les explicitement ensuite :

```sql
ALTER TABLE historique VALIDATE CONSTRAINT fk_historique_num_client;
ALTER TABLE ca VALIDATE CONSTRAINT fk_ca_cle_opl;
ALTER TABLE france_optique VALIDATE CONSTRAINT fk_france_optique_famille;
ALTER TABLE france_optique VALIDATE CONSTRAINT fk_france_optique_pays;
```

### Pseudonymiser

Si l'échantillon doit circuler plus largement, ajoutez un script à la suite de
`02_completer.sql` : des `UPDATE` sur `raison_sociale`, `rue`, `telephone`,
`email`, `note` et `note_perm`, et un remplacement des `fic_stk` par un fichier
témoin. Les mots de passe sont déjà neutralisés.

---

## Attention

Ce dump contient de **vraies données clients** : raisons sociales, adresses,
téléphones, courriels, notes commerciales, et de vrais devis et bons de commande
signés en pièces jointes.

**Ne le déposez jamais sur un partage ouvert ni dans un dépôt public.** Le
dossier `webapp/AppData/`, créé à l'exécution, relève de la même catégorie : il
contient les modèles de Bon de Commande déposés par les utilisateurs et les clés
de chiffrement des cookies.

### Si vous mettez ce projet sous contrôle de version

Deux précautions :

**Marquez `*.dump` comme binaire.** Sous git, ajoutez `*.dump binary` à un
`.gitattributes`. Sans cela, la normalisation des fins de ligne peut corrompre
le fichier **silencieusement** — il ne se restaurera plus et rien ne le signalera
avant l'échec.

**Excluez `echantillon/data/`** si vous régénérez un échantillon : ces CSV
contiennent les mêmes données clients que le dump, mais en clair et non
compressées. Excluez aussi `webapp/AppData/`, `bin/` et `obj/`.

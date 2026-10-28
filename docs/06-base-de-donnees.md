# 06 — Base de données

[← Retour à l'index](README.md)

---

## 1. Principe de conception

Le schéma est **fidèle à HFSQL**, délibérément. Les choix structurants :

- **Les clés primaires sont les clés métier réelles** quand il en existe une :
  `cle_opl`, `nom_famille`, `nom_franchise`, `prenom_nom`, `login`, ou les clés
  composites que HFSQL déclarait déjà. Pas d'identifiant de substitution
  inventé. Une chaîne « PRÉNOM NOM » sert donc de clé et se raccorde
  naturellement aux références par nom déjà présentes partout
  (`assistante_commercial`, `nom_operateur`).
- **Pas de normalisation supplémentaire** : pas de table de liaison pour les
  franchises, pas de table d'énumération pour les statuts. Les quatre créneaux
  de franchise restent quatre colonnes, les statuts restent du texte.
- **Les clés étrangères sont un ajout.** HFSQL n'en déclarait aucune
  (`Nb. liaisons: 0`) ; toute l'intégrité référentielle vivait dans le code
  WLangage. Celles de `02_foreign_keys.sql` sont une amélioration, pas une
  reprise.
- **L'authentification n'a pas d'équivalent HFSQL** : `users` et
  `login_history` sont entièrement nouvelles et remplacent le composant GPW.

---

## 2. Les tables

### `france_optique` — les clients

La table centrale. Clé primaire `cle_opl` (`BIGINT`). Environ 93 000 lignes en
production.

Groupes de colonnes :

| Groupe | Colonnes |
|---|---|
| Identité | `cle_opl`, `raison_sociale`, `complement`, `famille` |
| Franchises | `franchise`, `franchise2`, `franchise3`, `franchise4` + `n_adherent`, `n_adherent2`, `n_adherent3`, `n_adherent4` |
| Adresse | `rue`, `localisation_1`, `localisation_2`, `cp`, `ville`, `pays` |
| Contact | `telephone`, `tel_bis`, `portable`, `fax`, `email`, `email2` |
| Personnes | `assistante_commercial`, `responsable_achat`, `representant` |
| Commercial | `op_en_cours`, `status_vente`, `statuts_clients`, `etat_client`, `production`, `magasin_principal` |
| Rappel | `date_rappel`, `heure_rappel`, `rappel_rdv` |
| Saisie | `date_saisie`, `heure_saisie` |
| Notes | `note`, `note_perm` |
| Indicateurs | `bloque`, `pas_emailing`, `magasin_ferme` |
| Facturation | `siret`, `siren`, `tva`, `facturation_electronique` |
| Validation | `validation_status`, `added_by`, `validated_by`, `validated_at`, `refusal_reason` |

Points à connaître :

- **Les numéros d'adhérent sont appariés par créneau** : `n_adherent` va avec
  `franchise`, `n_adherent2` avec `franchise2`, etc. Cet appariement **est** le
  sens du champ. C'est pourquoi la recherche compare chaque case à sa propre
  colonne, contrairement aux cases Franchise juste au-dessus qui cherchent dans
  les quatre créneaux indifféremment.
- **`n_adherent`, `cp`, `siret`, `siren`, `tva` sont du texte**, pas des
  nombres : ce sont des codes attribués, jamais calculés, et les zéros en tête
  ou les lettres doivent survivre.
- **`magasin_ferme` n'est pas un booléen**, contrairement aux autres
  indicateurs. `NULL` = magasin ouvert ; sinon l'un des deux libellés autorisés
  par la contrainte `CHECK` : `Fermé temporairement` ou `Fermé définitivement`.
  Le libellé français est stocké tel quel, pour que l'export et le constructeur
  de filtres restent lisibles sans table de correspondance.
- **`added_by` et `validated_by` sont `NULL` pour les clients repris** de
  l'ancienne application : personne ne les a créés dans cette application, et
  leur attribuer un auteur serait inventer une donnée.
- **`etat_client` contient des valeurs héritées** (`0`, `-1`, vide). L'affichage
  les ramène à « Client » / « Prospect », mais la valeur n'est jamais réécrite
  en masse.

Index : `date_rappel + heure_rappel`, `famille`, `franchise`, `pays`, et un
index composite sur la clé de tri par défaut de la grille (script 21).

### `historique` — les interactions

Clé primaire `id_histo` (`IDENTITY`). Plus d'un million de lignes en production.

Rattachée au client par `num_client` → `france_optique.cle_opl`, en
`ON DELETE RESTRICT` : **un client ayant de l'historique ne peut pas être
supprimé.**

| Groupe | Colonnes |
|---|---|
| Rattachement | `num_client` |
| Saisie | `date_saisie`, `heure_saisie`, `assistante_commercial` |
| Rappel | `date_rappel`, `heure_rappel` |
| Interaction | `operation`, `status_vente`, `statuts_clients`, `note` |
| Instantané client | `raison_sociale`, `cp`, `ville`, `magasin_principal`, `franchise`, `franchise2`, `franchise3`, `franchise4`, `n_adherent`, `n_adherent2`, `n_adherent3`, `n_adherent4` |
| Pièce jointe | `fic_stk` (`BYTEA`), `fic_stk_nom` |
| Suppression | `deleted_at` |
| Reprise | `id_histo_source` |

Deux particularités :

- **Les colonnes « instantané client » sont volontairement redondantes** avec
  `france_optique`. Elles figent l'état du client au moment de l'interaction.
  Renommer une franchise ne doit pas réécrire ce que dit une ligne de 2019 —
  c'est pourquoi ces colonnes **n'ont pas** de clé étrangère, contrairement à
  leurs homonymes sur `france_optique`. Leurs largeurs sont identiques à celles
  de la source : un instantané plus étroit tronquerait précisément ce qu'il est
  censé conserver.
- **`deleted_at` implémente la suppression réversible.** Contrairement au filtre
  global des clients refusés, cette condition est répétée à la main sur les
  neuf requêtes concernées.

### `ca` — chiffre d'affaires

Clé primaire composite `(cle_opl, annee)`, plus `id_ca` en `IDENTITY`. Une
ligne par client et par année. `ON DELETE RESTRICT` vers le client.

### `filtre_operatrice` et `filtre_historique` — filtres enregistrés

Clé primaire composite `(nom_operateur, nom_filtre)` : chaque opératrice a ses
propres filtres, et deux opératrices peuvent utiliser le même nom. Le texte du
filtre est dans `filtre_reel` (`VARCHAR(512)`), dans la syntaxe décrite en
[07](07-syntaxe-des-filtres.md).

> `filtre_historique` avait d'abord été créée comme une simple liste nommée,
> avec `nom_filtre` seul en clé. Le script 15 a ajouté `nom_operateur` à la clé
> pour l'aligner sur `filtre_operatrice`.

### Tables de référence

| Table | Clé | Colonnes |
|---|---|---|
| `type_famille` | `nom_famille` VARCHAR(30) | la seule |
| `type_franchise` | `nom_franchise` VARCHAR(50) | la seule |
| `pays` | `id_pays` IDENTITY | `nom_pays` (UNIQUE), `indicatif`, `masque` |
| `assistantes` | `prenom_nom` VARCHAR(100) | `service`, `all_filtres`, `commentaire` |

> **`assistantes.all_filtres` n'est plus lu par l'application.** Les droits
> viennent entièrement de `users.role`. La colonne subsiste et l'interface la
> laisse modifiable, mais la cocher n'a aucun effet.

### `users` et `login_history` — authentification

```sql
users (
    login                VARCHAR(100) PRIMARY KEY,
    password_hash        VARCHAR(255) NOT NULL,   -- vide = à définir à la 1re connexion
    role                 VARCHAR(20)  NOT NULL CHECK (role IN ('admin','assistant','commercial')),
    must_change_password BOOLEAN      NOT NULL DEFAULT true,   -- n'est lu par aucun écran
    is_active            BOOLEAN      NOT NULL DEFAULT true,
    last_login_at        TIMESTAMP,
    created_at           TIMESTAMP    NOT NULL DEFAULT now(),
    failed_login_count   INTEGER      NOT NULL DEFAULT 0,
    lockout_until        TIMESTAMP
)
```

`login_history` enregistre les connexions **réussies** uniquement :
`id`, `login` → `users.login`, `logged_in_at`.

### `parametrage` et `fiche_a_supprimer`

Créées par le script 28 pour la fidélité de la reprise de données.
**L'application ne les lit pas.** `parametrage` contient notamment les chemins
de dossiers et de modèles de Bon de Commande côté HFSQL — l'application gère ses
propres modèles dans `webapp/AppData/BonCommandeTemplates/`.

---

## 3. Relations

```
type_famille ──┐
type_franchise ├──(SET NULL)──> france_optique ──(RESTRICT)──> historique
pays ──────────┘                      │
                                      └──(RESTRICT)──> ca

users ──> login_history
```

| Relation | Au départ de | Suppression |
|---|---|---|
| `famille` | `type_famille.nom_famille` | `SET NULL` |
| `franchise`, `franchise2`, `3`, `4` | `type_franchise.nom_franchise` | `SET NULL` |
| `pays` | `pays.nom_pays` | `SET NULL` |
| `historique.num_client` | `france_optique.cle_opl` | `RESTRICT` |
| `ca.cle_opl` | `france_optique.cle_opl` | `RESTRICT` |
| `login_history.login` | `users.login` | — |

Supprimer une valeur de référence **détache** les clients concernés
(`SET NULL`) ; supprimer un client ayant de l'historique ou du chiffre
d'affaires est **refusé** (`RESTRICT`).

`france_optique.added_by` et `validated_by` sont de véritables clés étrangères
vers `users.login` en base, mais ne sont pas modélisées en navigation EF : les
écrans affichent la chaîne directement.

### Séquences

| Séquence | Usage |
|---|---|
| `france_optique_cle_opl_seq` | nouvelle clé client, **à partir de 200 000** |
| `bon_commande_number_seq` | numéro de bon de commande |

Le point de départ à 200 000 est choisi pour rester à l'écart des deux amas
présents dans les données réelles : la plage basse (maximum observé 185 644) et
une poignée de clients près de 999 999 999, conséquence du balayage décroissant
de l'ancienne application.

---

## 4. Les 28 scripts

Aucun outil de migration : application manuelle, dans l'ordre numérique. Chaque
fichier porte un en-tête expliquant son intention.

| # | Fichier | Effet |
|---|---|---|
| 01 | `tables` | les 9 tables issues de HFSQL |
| 02 | `foreign_keys` | les clés étrangères, en `NOT VALID` — **voir l'encadré de [01 — Installation](01-installation.md#3-appliquer-les-scripts-sql)** |
| 03 | `cle_opl_sequence` | séquence pour les nouveaux clients, remplace le balayage max+1 |
| 04 | `historique_soft_delete` | ajoute `deleted_at` |
| 05 | `users_and_roles` | crée `users`, un compte par assistante existante |
| 06 | `login_history` | crée `login_history` |
| 07 | `siret_siren_facturation` | `siret`, `siren`, `facturation_electronique` |
| 08 | `note_widen` | élargit `note` |
| 09 | `statuts_clients_default` | valeur par défaut de `statuts_clients` |
| 10 | `fic_stk_filename` | ajoute `fic_stk_nom` |
| 11 | `bon_commande_number_seq` | séquence des numéros de bon de commande |
| 12 | `tva` | ajoute `tva` |
| 13 | `bon_commande_number_reset` | remise à zéro ponctuelle de la séquence |
| 14 | `telbis_widen` | élargit `tel_bis` (données réelles plus longues que prévu) |
| 15 | `filtre_historique_operateur` | ajoute `nom_operateur` à la clé primaire |
| 16 | `login_lockout` | `failed_login_count`, `lockout_until` |
| 17 | `pays_fuseau_horaire` | ajoute `pays.fuseau_horaire` — **annulé par le 26** |
| 18 | `france_optique_en_attente` | première version du circuit de validation — **remplacé par le 19** |
| 19 | `france_optique_validation` | le circuit de validation retenu, sur la table client |
| 20 | `drop_france_optique_en_attente` | retire ce qu'avait créé le 18 |
| 21 | `france_optique_raison_sociale_index` | index composite sur le tri par défaut de la grille |
| 22 | `pas_emailing_magasin_ferme` | deux champs, Parcours Client seulement |
| 23 | `email2_n_adherent` | `email2` et les quatre numéros d'adhérent |
| 24 | `historique_n_adherent` | `n_adherent` sur l'historique |
| 25 | `historique_franchise_2_4` | créneaux de franchise 2 à 4 sur l'historique |
| 26 | `drop_pays_fuseau_horaire` | annule le 17 (les fuseaux viennent du navigateur) |
| 27 | `historique_id_histo_source` | `id_histo_source` — **reprise de données uniquement** |
| 28 | `parametrage_and_fiche_a_supprimer` | deux tables — **reprise de données uniquement** |

### Deux paires d'aller-retour, et c'est normal

**17 puis 26** : `pays.fuseau_horaire` a été ajouté pour un sélecteur de fuseau
par pays, puis retiré quand le fuseau est passé à une lecture dans le
navigateur.

**18 puis 19 et 20** : le circuit de validation a d'abord été conçu avec une
table d'attente séparée, puis refait avec des colonnes sur la table client.

Sur une base neuve, on applique quand même les quatre dans l'ordre : ils
ajoutent puis retirent. Les passer dans l'ordre est plus sûr que de chercher à
les sauter, car les scripts suivants supposent l'état produit par les
précédents.

### Les scripts 27 et 28

Ils servent l'import depuis HFSQL, non l'application :
`historique.id_histo_source` est la clé sur laquelle l'import se raccorde, et
`parametrage` / `fiche_a_supprimer` existaient en production sans équivalent
PostgreSQL. **Rien dans l'application ne lit ces trois objets.** Les appliquer
est sans risque et garde le schéma identique à la production.

---

## 5. Correspondance avec le modèle EF

`webapp/Data/AppDbContext.cs` déclare 11 `DbSet`. Les entités sont dans
`webapp/Models/`, annotées `[Table]` / `[Column]`.

| Table PostgreSQL | Entité |
|---|---|
| `france_optique` | `FranceOptique` |
| `historique` | `Historique` |
| `ca` | `Ca` |
| `type_famille` | `TypeFamille` |
| `type_franchise` | `TypeFranchise` |
| `pays` | `Pays` |
| `assistantes` | `Assistante` |
| `filtre_operatrice` | `FiltreOperatrice` |
| `filtre_historique` | `FiltreHistorique` |
| `users` | `User` |
| `login_history` | `LoginHistory` |

Pas d'entité pour `parametrage` ni `fiche_a_supprimer`.

> **Si vous régénérez les entités par scaffolding**, l'ordre des propriétés suit
> l'ordre des colonnes en base. C'est pourquoi des champs logiquement liés sont
> parfois éloignés dans les fichiers (`Historique.NAdherent` se trouve entre
> `FicStkNom` et `Franchise2`) : le regroupement logique est dans l'interface,
> pas dans l'ordre du fichier. Les annotations et commentaires ajoutés à la main
> seraient perdus.

---

[← Précédent : Architecture technique](05-architecture-technique.md) · [Retour à l'index](README.md) · [Suivant : Syntaxe des filtres →](07-syntaxe-des-filtres.md)

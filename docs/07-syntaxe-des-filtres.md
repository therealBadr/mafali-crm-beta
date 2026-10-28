# 07 — Syntaxe des filtres

[← Retour à l'index](README.md)

Syntaxe du texte de filtre enregistré dans « Filtres Opératrice » et « Filtres
Historique » — le champ `filtre_reel`.

Cette grammaire décrit ce que l'application **interprète réellement**
(`webapp/Services/FiltreReelParser.cs`). Elle a été établie à partir des
66 filtres effectivement enregistrés dans la base de l'ancienne application,
non définie à l'avance.

---

## 1. Structure générale

Un filtre est une suite de conditions :

```
CHAMP OPÉRATEUR valeur
```

reliées par **ET** / **OU** et regroupées par des parenthèses.

Les mots `ET` et `OU` sont en français et **insensibles à la casse** : `ET`,
`et`, `Et` fonctionnent tous.

Exemple réel :

```sql
(Famille = 'Opticien FRANCE') ET (Telephone LIKE '03%' ou Telephone LIKE '05%')
```

→ les clients dont la Famille est exactement « Opticien FRANCE » **et** dont le
téléphone commence par 03 **ou** par 05.

> Les parenthèses ne sont pas obligatoires partout, mais **fortement
> recommandées** dès qu'on mélange `ET` et `OU` dans une même expression, pour
> lever toute ambiguïté sur l'ordre de priorité.

---

## 2. Opérateurs

| Opérateur | Sens |
|---|---|
| `=` | égal à |
| `<>` | différent de |
| `<` | inférieur à |
| `<=` | inférieur ou égal à |
| `>` | supérieur à |
| `>=` | supérieur ou égal à |
| `LIKE` | contient / commence par (voir § 5) |
| `NOT LIKE` | ne contient pas / ne commence pas par |
| `BETWEEN … AND …` | compris entre deux valeurs, **bornes incluses** |

Exemple avec `BETWEEN` :

```sql
Date_Rappel BETWEEN '20201124' AND '20260724'
```

---

## 3. Champs disponibles

Les champs portent leur nom **historique** (celui de l'ancienne application),
pas le nom de la colonne PostgreSQL. Ils sont **insensibles à la casse** :
`FAMILLE`, `Famille` et `famille` sont équivalents.

### Filtres clients — 42 champs

| | | |
|---|---|---|
| `Famille` | `Raison_Sociale` | `Complement` |
| `Franchise` | `Franchise2` | `Franchise3` |
| `Franchise4` | `N_Adherent` | `N_Adherent2` |
| `N_Adherent3` | `N_Adherent4` | `Rue` |
| `Localisation_1` | `Localisation_2` | `CP` |
| `Ville` | `Pays` | `Telephone` |
| `Tel_Bis` | `Portable` | `Email` |
| `Email2` | `Assistante_Commerciale` | `Responsable_Achat` |
| `Représentant` | `Op_En_Cours` | `Status_Vente` |
| `Statuts_Clients` | `Etat_Client` | `Production` |
| `Magasin_Principal` | `Date_Saisie` | `Heure_Saisie` |
| `Date_Rappel` | `Heure_Rappel` | `Note` |
| `Note_Perm` | `Bloque` | `Pas_Emailing` |
| `Magasin_Ferme` | `Rappel_RDV` | `Cle_Opl` |

> **`Fax` n'est pas filtrable**, bien que la colonne existe en base. Le champ a
> été retiré de Parcours Client et du vocabulaire des filtres.

### Filtres historique — 22 champs

Le vocabulaire de l'historique est plus restreint, et porte sur **ses propres**
colonnes :

| | | |
|---|---|---|
| `Num_Client` | `Date_Saisie` | `Heure_Saisie` |
| `Assistante_Commerciale` | `Date_Rappel` | `Heure_Rappel` |
| `Operation` | `Status_Vente` | `Statuts_Clients` |
| `Note` | `Raison_Sociale` | `CP` |
| `Ville` | `Magasin_Principal` | `Franchise` |
| `N_Adherent` | `Franchise2` | `N_Adherent2` |
| `Franchise3` | `N_Adherent3` | `Franchise4` |
| `N_Adherent4` | | |

> Noter `Num_Client` : c'est le rattachement au client côté historique. Il n'y a
> **pas** de `Cle_Opl` dans un filtre historique, et inversement `Num_Client`
> n'existe pas dans un filtre client. Les deux vocabulaires sont distincts même
> si la grammaire est la même.

Un champ absent de la liste applicable fait **échouer le filtre à l'exécution**
(voir § 6).

---

## 4. Valeurs

Les valeurs textuelles **et les dates** s'écrivent toujours entre apostrophes
`'…'`.

### Le mot-clé `SYSDATE`

Un seul mot-clé spécial, à écrire **sans apostrophes** :

| Mot-clé | Sens |
|---|---|
| `SYSDATE` | la date du jour — utilisable uniquement sur un champ de type date |

```sql
Date_Rappel < SYSDATE
```

→ les rappels déjà échus.

### Format des dates

Les dates s'écrivent en **`AAAAMMJJ`**, sans séparateur. **Jamais** au format
`JJ/MM/AAAA`.

```sql
Date_Rappel BETWEEN '20201124' AND '20260724'
```

---

## 5. Le joker `%`

Le caractère `%` dans une valeur est **toujours** traité comme un joker, **même
si l'opérateur est `=` et non `LIKE`**.

Ce n'est pas une tolérance ajoutée : c'est le comportement réel, confirmé par
l'usage. Le filtre

```sql
Famille = '%Agence Pub%'
```

apparaît tel quel — avec `=` — dans 15 filtres enregistrés par quatre
opératrices différentes.

### Positions reconnues

| Écriture | Sens |
|---|---|
| `'texte%'` | commence par « texte » |
| `'%texte%'` | contient « texte » |

Un `%` **uniquement en début** (`'%texte'`, sans `%` final) n'apparaît dans
aucun filtre réel et **n'est pas pris en charge**.

### Avec `NOT LIKE`

Même fonctionnement, en négatif :

```sql
Op_En_Cours NOT LIKE 'Pas Interesse'
Franchise   NOT LIKE '%Generale%'
```

---

## 6. En cas d'erreur

Si le texte ne respecte pas cette grammaire — champ inconnu, opérateur non
reconnu, parenthèse manquante — l'application **refuse de l'exécuter** et
affiche une erreur au lancement depuis « Filtres Prédéfinis ».

> **Le filtre reste enregistré tel quel, sans modification.** Il suffit de
> corriger le texte pour qu'il redevienne utilisable. Aucune donnée n'est
> perdue, et le filtre n'est jamais réécrit automatiquement.

C'est un point de test utile : enregistrer un filtre volontairement invalide
doit produire une erreur à l'exécution **et** laisser le filtre intact.

---

## 7. Exemples réels commentés

### Exemple 1

```sql
(Date_Rappel BETWEEN '20201124' AND '20260724')
et (Famille='Opticien FRANCE')
ET ( Telephone LIKE '03%' ou Telephone LIKE '05%')
et (Op_En_Cours NOT LIKE 'Pas Interesse')
```

- date de rappel dans la période donnée
- **et** Famille exactement « Opticien FRANCE »
- **et** téléphone commençant par 03 **ou** par 05
- **et** opération en cours différente de « Pas Interesse »

### Exemple 2

```sql
(Famille ='%Agence Pub%' ou Famille = 'SOCIETE')
Et (telephone LIKE '02%')
ET (PAYS = 'FRANCE')
Et ( CP NOT LIKE '97%')
Et (Date_Rappel < SYSDATE)
```

- Famille contenant « Agence Pub » **ou** exactement « SOCIETE »
- **et** téléphone commençant par 02
- **et** pays exactement « FRANCE »
- **et** code postal ne commençant pas par 97 *(exclut les DOM)*
- **et** date de rappel déjà passée

Noter au passage la casse libre : `Et`, `ET`, `ou`, `telephone`, `PAYS`.

### Exemple 3

```sql
(Famille='Opticien FRANCE')
ET ( Telephone LIKE '03%' ou Telephone LIKE '05%')
ET ( Note <>'9999' et Note <>'5555' et Note <>'4444')
```

- Famille exactement « Opticien FRANCE »
- **et** téléphone commençant par 03 **ou** par 05
- **et** Note différente de chacune de ces trois valeurs

> `9999`, `5555` et `4444` semblent être des codes internes à exclure. Leur
> signification n'a pas été documentée par l'ancienne application.

---

## 8. Où cela s'écrit et s'exécute

| Écran | Rôle |
|---|---|
| **Définir un Filtre** (`/definir-filtre`) | construit le texte sans l'écrire à la main |
| **Fichier des Filtres** (`/fichier-filtres`) | consulte, renomme, supprime |
| **Filtres Prédéfinis** (`/filtres-predefinis`) | exécute et affiche les clients correspondants |

Les équivalents côté historique sont « Définir un Filtre Historique » et
« Fichier des Filtres Historique ».

### Chaîne de traitement dans le code

```
filtre_reel (texte)
   └─> FiltreReelParser          analyse la grammaire ci-dessus
         └─> ClientFilterService     traduit en prédicat LINQ
         └─> HistoriqueFilterService (équivalent pour l'historique)
               └─> System.Linq.Dynamic.Core
                     └─> SQL PostgreSQL
```

Pour étendre la grammaire, deux endroits :

- **un nouvel opérateur** → `FiltreReelParser` (analyseur et modèles
  d'opérateurs) ;
- **un nouveau champ** → `ClientFilterService.LegacyFieldName` ou
  `HistoriqueFilterService.LegacyFieldName`. Ce sont ces deux dictionnaires qui
  font autorité pour les listes du § 3 ; `FiltreReelParser` les retourne
  simplement pour résoudre un nom écrit dans un filtre.

---

[← Précédent : Base de données](06-base-de-donnees.md) · [Retour à l'index](README.md)

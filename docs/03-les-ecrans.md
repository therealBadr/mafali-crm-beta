# 03 — Les écrans, un par un

[← Retour à l'index](README.md)

25 écrans, présentés dans l'ordre du menu. Pour chacun : à quoi il sert, son
adresse, qui y a accès, et la fenêtre WinDev correspondante quand elle existe.

Le tableau complet des accès par rôle est dans
[02 — Connexion et rôles](02-connexion-et-roles.md#5-tableau-des-accès-par-écran).

---

## Accueil

### Tableau de bord — `/`
*Tous les rôles*

Page d'arrivée après connexion. Tuiles de synthèse construites à partir des
requêtes que d'autres écrans exécutent déjà : nombre de rappels dus, filtres
enregistrés, devis et bons de commande en cours, et pour un administrateur
quelques statistiques sur les comptes. Pas de table ni de suivi dédié.

Avant, « / » redirigeait selon le rôle vers un écran de travail ; c'est
maintenant un vrai tableau de bord pour tous les rôles.

---

## Menu « Fichiers » *(admin et assistant)*

Les huit écrans de gestion des données de référence et des fichiers principaux.

### Fichier des Clients — `/clients`
Équivalent de **Fen_Fichier_Client**. La liste maîtresse des clients : parcourir,
créer, modifier, supprimer, importer et exporter en Excel.

La grille est **paginée côté base de données** : seules les lignes affichées sont
chargées, avec tri et filtres traduits en SQL. Important, la table réelle
compte plus de 93 000 clients. Les colonnes se redimensionnent à la souris
comme dans Excel, et la hauteur de la grille s'ajuste en tirant sa bordure
supérieure.

Création et modification passent par `/clients/new` et `/clients/{id}/edit`.

### Fichier des Familles — `/familles`
Table de référence des familles de clients (`type_famille`). Une seule colonne :
le nom. Alimente les listes déroulantes « Famille » partout ailleurs.

### Fichier des Franchises — `/franchises`
Table de référence des franchises (`type_franchise`). Même structure que les
familles. Un client peut porter jusqu'à quatre franchises.

### Liste Pays — `/pays`
Table de référence des pays (`pays`) : nom, indicatif téléphonique et masque de
saisie du numéro.

### Fichier des Assistantes — `/assistantes`
Table de référence des assistantes commerciales (`assistantes`) : nom, service,
commentaire. C'est cette liste qui alimente les champs « Assistante
Commerciale » et qui a servi à créer les comptes utilisateurs.

> **⚠ À savoir** — la case **« Accès à tous les Filtres »** de cet écran ne
> produit plus aucun effet. Les droits sont entièrement déterminés par le rôle
> depuis l'introduction de l'authentification. La colonne est restée en base et
> la case reste modifiable, mais rien ne la lit. Cocher ou décocher ne change
> rien. Voir [04 — Guide de test](04-guide-de-test.md).

### Fichier des Filtres — `/fichier-filtres`
Liste des filtres clients enregistrés (`filtre_operatrice`), par opératrice.
Permet de consulter, renommer et supprimer. L'écriture du filtre lui-même se
fait dans « Définir un Filtre ».

### Fichier des Filtres Historique — `/fichier-filtres-historique`
Même chose pour les filtres portant sur l'historique
(`filtre_historique`).

### Fichier des Historiques — `/fichier-historique`
Équivalent de **Fen_Filtre** côté historique. La liste de toutes les
interactions enregistrées, toutes fiches confondues. Création et modification
par `/historiques/new` et `/historiques/{id}/edit`.

> **⚠ À savoir** — la suppression d'une ligne d'historique est **réversible** :
> la ligne est marquée supprimée (`deleted_at`) et disparaît de tous les écrans,
> mais reste en base. L'ancienne application supprimait définitivement. C'est un
> changement volontaire, pour conserver la trace des échanges.

---

## Filtres

### Définir un Filtre — `/definir-filtre`
*admin et assistant*

Constructeur de filtres sur les clients. On empile des conditions
(champ, opérateur, valeur), reliées par ET/OU, et on enregistre le résultat
sous un nom. Les champs de référence (Famille, Franchise, Pays, Assistante)
sont des listes déroulantes alimentées par les tables de référence, pas du
texte libre — pour éviter d'enregistrer une faute de frappe qui ne
correspondrait à aucun client.

Le filtre est stocké sous forme de texte dans `filtre_operatrice.filtre_reel`,
dans la syntaxe héritée de l'ancienne application. Cette syntaxe est décrite
dans [07 — Syntaxe des filtres](07-syntaxe-des-filtres.md).

### Définir un Filtre Historique — `/definir-filtre-historique`
*admin et assistant*

Le même constructeur, appliqué aux champs de l'historique.

### Filtres Prédéfinis — `/filtres-predefinis`
*Tous les rôles*

Équivalent de **Fen_Filtre**. On choisit un filtre enregistré, on voit la liste
des clients correspondants, et en cliquant sur un client son historique
s'affiche dessous.

Le sélecteur d'opératrice est contraint par le rôle : un administrateur ou une
assistante peut consulter les filtres de n'importe quelle opératrice ; un
commercial est verrouillé sur son propre nom — la liste est pré-remplie et
désactivée plutôt que masquée, pour que la mise en page reste la même.

Le bouton **« Travailler sur les enregistrements »** ouvre Parcours Client sur
le résultat du filtre, ce qui permet de traiter les fiches une par une.

### Rechercher un Client — `/recherche-client`
*Tous les rôles*

Équivalent de **Fen_Recherche_Client**. Recherche multicritère : on remplit un
ou plusieurs champs et on obtient les clients qui correspondent à **tous** les
critères renseignés.

Deux comportements à connaître :

- Les champs de **texte libre** proposent les valeurs déjà présentes en base au
  fur et à mesure de la saisie, et recherchent en **« contient »**.
- Les champs de **référence** (Famille, Franchise 1 à 4, Pays, Assistante
  Commerciale) sont des listes déroulantes et recherchent en **égalité
  exacte** — puisqu'une valeur choisie dans une liste est forcément exacte.

L'écran permet aussi d'enregistrer une modification sur le client trouvé, ce
qui explique le choix des listes déroulantes : du texte libre pourrait écrire
une faute de frappe dans une fiche réelle.

---

## Suivi commercial

### Parcours Client — `/parcours-client`
*Tous les rôles*

Équivalent de **Fen_Parcours_Client**, et l'écran le plus riche de
l'application. La fiche complète d'un client : coordonnées, familles et
franchises, état commercial, notes, rappels, chiffre d'affaires, et
l'historique complet des interactions.

C'est ici que se fait l'essentiel du travail quotidien :

- enregistrer une interaction (opération, statut de vente, note, pièce jointe) ;
- programmer un rappel — à une date et une heure précises, ou dans N mois ;
- faire défiler les clients un par un, dans l'ordre du fichier ou dans celui
  d'un filtre ;
- **générer un fichier Bon de Commande** pour le client affiché.

Le modèle de Bon de Commande est choisi automatiquement d'après la Famille du
client : si elle commence par « Opticien », c'est le modèle *Optique*, sinon le
modèle *Revendeur*. Le bouton est désactivé si le client est bloqué.

Certains champs sont masqués pour le rôle commercial (notamment l'état du
magasin et l'indicateur « Pas d'Emailing »), et un commercial ne peut pas
décider qu'un client est bloqué.

### Rappels — `/rappels`
*Tous les rôles*

Écran nouveau, sans équivalent direct : la fenêtre **Fen_Rappel** de l'ancienne
application ne montrait que les rappels du jour exactement, sans cumul des
retards. Cet écran liste les rappels **dus**, retards compris.

Chaque utilisateur voit les rappels qui le concernent. Une fenêtre surgissante
avertit à l'échéance, y compris si l'utilisateur est sur un autre écran : un
service en arrière-plan examine régulièrement les rappels et pousse la
notification vers les sessions ouvertes.

> Les heures de rappel sont interprétées dans le **fuseau horaire du poste de
> l'utilisateur**, lu dans le navigateur. Voir
> [05 — Architecture](05-architecture-technique.md) pour le détail.

### Devis en cours — `/devis-en-cours`
*Tous les rôles*

Les clients dont le statut de vente est « Devis en cours », pour l'utilisateur
connecté. Liste de relance.

### Bons de Commande en cours — `/bon-commande-en-cours`
*Tous les rôles*

Les clients ayant un bon de commande en cours, pour l'utilisateur connecté.

### Bon de Commande — `/bon-commande`
*Tous les rôles*

Écran de configuration, en deux parties indépendantes :

1. **Source** — remplacer les deux modèles Excel (*Optique* et *Revendeur*)
   utilisés pour générer les bons de commande. Les fichiers sont stockés sur le
   serveur, dans `webapp/AppData/BonCommandeTemplates/`.
2. **Destination** — autoriser l'application à écrire dans un dossier local du
   poste, pour que les bons de commande générés y arrivent directement.

> **⚠ À savoir — la destination ne fonctionne que sur Chrome et Edge**
>
> Elle repose sur l'API *File System Access*, que Firefox et Safari
> n'implémentent pas. L'application détecte le cas et masque la fonction si le
> navigateur ne sait pas la gérer. L'autorisation est mémorisée par le
> navigateur lui-même, pas par l'application : elle est donc propre à un profil
> de navigateur sur un poste, et ne suit pas l'utilisateur d'une machine à
> l'autre. **Testez de préférence sur Chrome ou Edge.**

---

## Pour le rôle commercial uniquement

### Ajouter un Client — `/ajouter-client`
Formulaire de création de client destiné aux commerciaux. Le client créé est
immédiatement utilisable, et porte le statut « en attente de validation ». Il
n'est pas mis en quarantaine : le commercial peut travailler dessus tout de
suite.

### Mes Clients Ajoutés — `/mes-clients-ajoutes`
La liste des clients que le commercial connecté a créés, avec leur état de
validation. **Un client refusé y reste visible, avec le motif du refus** —
c'est le seul endroit de l'application où un client refusé apparaît.

---

## Validation *(admin et assistant)*

### Clients à valider — `/clients-a-valider`
La file des clients créés par des commerciaux et pas encore examinés. Le détail
s'ouvre sur `/clients-a-valider/{id}`, où le client est accepté ou refusé avec
un motif.

Trois états possibles, dans `france_optique.validation_status` :

| État | Effet |
|---|---|
| `pending` | en attente — le client est **visible et utilisable partout** |
| `validated` | validé — comportement identique à `pending` |
| `refused` | refusé — **masqué dans toute l'application** |

> **⚠ À savoir** — `pending` et `validated` se comportent exactement de la même
> façon. La différence est une question de responsabilité, pas de
> fonctionnement : valider, c'est acter qu'une assistante a vu la fiche. Seul
> `refused` change quelque chose.

---

## Administration *(admin uniquement)*

### Mon Compte — `/mon-compte`
Deux usages réunis :

- **Aperçu** — statistiques et connexions récentes ;
- **Utilisateurs** — créer un compte, changer un rôle, réinitialiser un mot de
  passe, activer ou désactiver un compte.

Contient aussi le changement de son propre mot de passe.

L'écran est réservé aux administrateurs, et les méthodes d'administration
vérifient le rôle **côté serveur** en plus du masquage de l'interface.

### Historique des Connexions — `/historique-connexions`
Les connexions réussies (`login_history`) : qui s'est connecté et quand. Les
échecs n'y figurent pas.

---

## Écrans techniques

| Écran | Adresse | Rôle |
|---|---|---|
| Connexion | `/login` | seul écran accessible sans être connecté |
| Accès refusé | `/access-denied` | affiché quand le rôle ne suffit pas |
| Erreur | `/Error` | page d'erreur générique |

---

## Composants réutilisés partout

Dans `webapp/Components/Shared/` — utile à connaître pour localiser un défaut
d'interface, puisqu'un même composant sert sur plusieurs écrans :

| Composant | Rôle |
|---|---|
| `DateInput` | saisie de date au format français avec masque |
| `TimeInput` | saisie d'heure |
| `AutocompleteInput` | champ texte suggérant les valeurs déjà en base |
| `RappelPopup` | fenêtre surgissante d'alerte de rappel |
| `ColumnFilterIcon` | icône de filtre dans l'en-tête des colonnes |
| `GridFilterClearAll` | bouton « effacer tous les filtres » d'une grille |
| `FiltreDetailModal` | aperçu du texte d'un filtre enregistré |
| `BrowserClockProvider` | lit le fuseau horaire et l'heure du poste |

---

[← Précédent : Connexion et rôles](02-connexion-et-roles.md) · [Retour à l'index](README.md) · [Suivant : Guide de test →](04-guide-de-test.md)

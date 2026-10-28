# 04 — Guide de test

[← Retour à l'index](README.md)

Ce document sert deux buts : proposer un parcours de test organisé, et lister
les comportements **volontaires** qui ressemblent à des anomalies. La section 2
est la plus importante : elle évitera de signaler des défauts qui n'en sont pas.

---

## 1. Préparer l'environnement de test

1. Installer et lancer l'application → [01 — Installation](01-installation.md).
2. Créer un compte par rôle → [02 — Connexion](02-connexion-et-roles.md#2-créer-le-premier-compte).
3. Alimenter les tables de référence **avant** de créer des clients : sans
   familles, franchises, pays ni assistantes, les listes déroulantes des
   formulaires client sont vides. Dans cet ordre :
   - Fichier des Familles (`/familles`)
   - Fichier des Franchises (`/franchises`)
   - Liste Pays (`/pays`)
   - Fichier des Assistantes (`/assistantes`)
4. Créer quelques clients (`/clients/new`), puis quelques interactions
   d'historique depuis Parcours Client.

**Navigateur recommandé : Chrome ou Edge.** L'application fonctionne sur
Firefox et Safari, à l'exception du choix du dossier de destination des bons de
commande, qui repose sur une API que ces navigateurs n'implémentent pas.

---

## 2. Comportements volontaires — à ne pas signaler

| Observation | Explication |
|---|---|
| Le premier mot de passe saisi devient le mot de passe du compte | Les comptes sont créés sans mot de passe ; la première connexion le définit. Reprend le mécanisme de l'ancienne application. [Détail](02-connexion-et-roles.md#1-les-comptes-sont-créés-sans-mot-de-passe) |
| La case « Accès à tous les Filtres » (Fichier des Assistantes) ne change rien | Remplacée par les rôles. La colonne subsiste en base mais plus rien ne la lit. |
| Aucun écran n'oblige à changer son mot de passe | La colonne `must_change_password` existe mais n'est lue par aucun écran. |
| Un client refusé disparaît de partout | Volontaire, appliqué globalement au niveau de l'accès aux données. Seul « Mes Clients Ajoutés » le montre encore, avec son motif de refus. |
| Valider un client ne change rien de visible | `pending` et `validated` se comportent de façon identique. Seul `refused` modifie le comportement. |
| Une ligne d'historique supprimée reste en base | Suppression réversible (`deleted_at`), volontairement différente de l'ancienne application qui supprimait définitivement. |
| Changer le rôle d'un utilisateur ne prend pas effet tout de suite | Le rôle est inscrit dans le cookie à la connexion. Il faut se déconnecter et se reconnecter. |
| Un administrateur ne voit pas « Ajouter un Client » ni « Mes Clients Ajoutés » | Ces deux écrans sont réservés au rôle commercial. L'administrateur a ses équivalents : `/clients/new` et `/clients-a-valider`. |
| Chaque entrée du menu ouvre un nouvel onglet | Volontaire : reproduit les fenêtres indépendantes de l'ancienne application. |
| `Failed to determine the https port` au démarrage | Normal : déploiement en HTTP simple. La requête passe, ce n'est pas une boucle de redirection. |
| Deux avertissements EF au démarrage, sur `FranceOptique` | Connus et analysés : conséquence du filtre global sur les clients refusés. Sans effet ici. |
| Le champ « État Client » contient parfois `0`, `-1` ou du vide | Valeurs héritées des données d'origine. L'affichage les ramène à « Client » / « Prospect », mais la valeur stockée n'est pas réécrite tant que le champ n'est pas explicitement modifié. |
| Le dossier de destination des bons de commande est à redéfinir sur un autre poste | L'autorisation est mémorisée par le navigateur, pas par l'application. Elle est propre à un profil de navigateur. |

---

## 3. Parcours de test par domaine

### 3.1 Connexion et droits

- [ ] Première connexion : le mot de passe saisi est bien enregistré
- [ ] Deuxième connexion avec ce mot de passe : acceptée
- [ ] Mot de passe erroné : refusé, avec un message d'identifiants incorrects
- [ ] 5 mots de passe erronés : compte verrouillé 15 minutes
- [ ] Verrouillage : message distinct du message d'identifiants incorrects
- [ ] Compte désactivé (`is_active = false`) : message distinct également
- [ ] Déconnexion : retour à l'écran de connexion, session terminée
- [ ] Redémarrer l'application : les sessions ouvertes restent valides
      *(si tout le monde est déconnecté, le dossier `AppData/DataProtectionKeys`
      n'est pas conservé ou n'est pas accessible en écriture)*
- [ ] **Saisir directement une adresse interdite à son rôle** (par exemple
      `/mon-compte` en tant qu'assistant) : doit afficher « accès refusé »,
      pas l'écran de connexion
- [ ] Pour chacun des trois rôles, comparer le menu au
      [tableau des accès](02-connexion-et-roles.md#5-tableau-des-accès-par-écran)

### 3.2 Données de référence

Pour chacun des quatre écrans (Familles, Franchises, Pays, Assistantes) :

- [ ] Créer, modifier, supprimer
- [ ] Refuser un doublon
- [ ] Refuser un nom vide
- [ ] Respecter la longueur maximale (30 pour une famille, 50 pour une
      franchise, voir [06 — Base de données](06-base-de-donnees.md))
- [ ] Supprimer une valeur **utilisée par un client** : doit être refusé ou
      laisser le client sans valeur, jamais produire une erreur technique brute
- [ ] Renommer une valeur utilisée : vérifier que les clients concernés suivent

### 3.3 Clients

- [ ] Créer un client avec le minimum d'informations
- [ ] Créer un client avec tous les champs renseignés
- [ ] Modifier, puis vérifier la persistance après rechargement
- [ ] Supprimer un client **ayant de l'historique** : doit être refusé
      proprement, avec un message compréhensible
- [ ] Tri sur chaque colonne de la grille
- [ ] Filtre par colonne, puis « effacer tous les filtres »
- [ ] Redimensionner une colonne à la souris ; la largeur persiste au défilement
- [ ] Ajuster la hauteur de la grille en tirant sa bordure supérieure
- [ ] Défilement sur un grand volume : les lignes se chargent à la demande
- [ ] **Modification simultanée** : ouvrir la même fiche dans deux onglets,
      modifier dans les deux, enregistrer les deux. Le second doit détecter le
      conflit et non écraser silencieusement
- [ ] Un commercial crée un client : il apparaît dans « Clients à valider »
- [ ] Valider ce client : il disparaît de la file
- [ ] Refuser un client avec un motif : il disparaît partout **sauf** de
      « Mes Clients Ajoutés », où le motif est visible

### 3.4 Historique

- [ ] Ajouter une interaction depuis Parcours Client
- [ ] Joindre un fichier à une interaction, puis le retélécharger
- [ ] Modifier une interaction
- [ ] Supprimer une interaction : elle disparaît des écrans
- [ ] Vérifier en base que la ligne existe toujours avec `deleted_at` renseigné
- [ ] Les franchises et numéros d'adhérent enregistrés dans l'historique
      conservent la valeur du moment de la saisie, même si la fiche client
      change ensuite

### 3.5 Filtres

C'est le sous-système le plus complexe. La syntaxe est documentée dans
[07 — Syntaxe des filtres](07-syntaxe-des-filtres.md).

- [ ] Construire un filtre simple à une condition, l'enregistrer, l'exécuter
- [ ] Filtre à plusieurs conditions reliées par ET
- [ ] Filtre mêlant ET et OU avec des parenthèses : vérifier la priorité
- [ ] Chaque opérateur : `=`, `<>`, `<`, `<=`, `>`, `>=`, `LIKE`, `NOT LIKE`,
      `BETWEEN … AND …`
- [ ] Joker `%` en fin (`'texte%'`) et en encadrement (`'%texte%'`)
- [ ] `SYSDATE` sur un champ date
- [ ] Date au format `AAAAMMJJ`
- [ ] **Filtre volontairement invalide** (champ inexistant, parenthèse
      manquante) : doit afficher une erreur à l'exécution, et surtout
      **conserver le filtre enregistré tel quel**
- [ ] Rôle commercial : le sélecteur d'opératrice est verrouillé sur son nom
- [ ] « Travailler sur les enregistrements » ouvre Parcours Client sur le
      résultat du filtre
- [ ] Mêmes vérifications sur les filtres d'historique

### 3.6 Rappels

- [ ] Programmer un rappel à une date et une heure
- [ ] Programmer un rappel « dans N mois »
- [ ] Le rappel apparaît dans l'écran Rappels à l'échéance
- [ ] Un rappel en retard reste visible (contrairement à l'ancienne
      application, qui ne montrait que le jour exact)
- [ ] La fenêtre surgissante s'affiche alors que l'on est sur un autre écran
- [ ] Un rappel ne s'affiche que pour l'utilisateur concerné
- [ ] **Fuseau horaire** : l'heure affichée correspond bien à l'heure du poste.
      À vérifier en particulier si le serveur et le poste ne sont pas dans le
      même fuseau
- [ ] Ouvrir deux onglets : la notification ne doit pas arriver en double

### 3.7 Import / export Excel

L'import est **réservé aux administrateurs** — restriction appliquée à la fois
dans l'interface et dans le service.

- [ ] Exporter la liste des clients
- [ ] Réimporter le fichier exporté sans le modifier : tout doit ressortir en
      « inchangé », rien en « ajouté » ni « modifié »
- [ ] Modifier une cellule puis réimporter : une seule ligne en « modifié »
- [ ] Ajouter une ligne sans `Cle_Opl` : comptée en « ajoutée »
- [ ] Ligne avec une famille ou un pays inexistant : signalée dans le rapport
      de références invalides
- [ ] Fichier malformé : message clair, aucune donnée écrite
- [ ] Tenter l'import en tant qu'assistant : doit être refusé

### 3.8 Bon de Commande

- [ ] Déposer un modèle *Optique* et un modèle *Revendeur*
- [ ] Générer un bon depuis Parcours Client pour un client dont la famille
      commence par « Opticien » : le modèle *Optique* doit être utilisé
- [ ] Pour une autre famille : le modèle *Revendeur*
- [ ] Le numéro de bon de commande s'incrémente
- [ ] Client bloqué : le bouton est désactivé
- [ ] Famille sans modèle configuré : message invitant à contacter un
      administrateur
- [ ] Sur Chrome ou Edge : autoriser un dossier, le fichier y est écrit
- [ ] Sur Firefox : la fonction est masquée et la génération reste possible
- [ ] Vérifier le contenu du fichier produit : raison sociale, adresse,
      localisation, code postal et ville, et le contact repris du
      « Responsable Achats »

### 3.9 Robustesse

- [ ] Perdre le réseau puis le retrouver : Blazor Server rétablit la session.
      Vérifier qu'un écran rempli n'est pas vidé
- [ ] Deux utilisateurs différents en parallèle : aucune donnée de l'un
      n'apparaît chez l'autre
- [ ] Laisser un écran ouvert longtemps, puis agir : pas d'erreur technique brute
- [ ] Accentuation et caractères français (é, è, ç, œ) dans tous les champs,
      à l'écran, à l'export Excel et dans le bon de commande

---

## 4. Limites connues

Signalées pour éviter de les découvrir comme des surprises.

1. **Aucun test automatisé côté application.** Il n'existe pas de projet de
   tests dans la solution. Toute vérification est manuelle. Les trois endroits
   où des tests seraient les plus rentables sont l'analyseur de filtres
   (`FiltreReelParser`), le calcul d'échéance des rappels (`RappelDueTime`) et
   la validation des clients (`ClientValidationService`) — logique pure, peu de
   dépendances.
2. **Pas de système de migration de schéma.** Les 28 scripts SQL s'appliquent à
   la main. Rien ne vérifie en base quels scripts ont déjà été passés.
3. **Pas de HTTPS** en déploiement réseau local. Choix assumé, voir
   [01 — Installation](01-installation.md#6-déploiement-sur-le-réseau-local).
4. **Import/export Excel synchrone.** Pas de file de traitement : l'utilisateur
   attend la fin de l'opération. Volontaire, conforme à l'ancienne application.
5. **Le choix du dossier de destination des bons de commande** ne fonctionne que
   sur Chrome et Edge.
6. **Les tables `parametrage` et `fiche_a_supprimer`** (script 28) existent en
   base mais ne sont **pas lues par l'application**. Elles servent la fidélité
   de la reprise de données. Idem pour la colonne `historique.id_histo_source`
   (script 27).
7. **Pas de journal des échecs de connexion.** `login_history` n'enregistre que
   les connexions réussies.

---

## 5. Signaler une anomalie utilement

Avant de signaler, vérifier qu'il ne s'agit pas d'un point de la section 2.

Éléments à joindre :

| | |
|---|---|
| **Écran et adresse** | « Parcours Client — `/parcours-client` » |
| **Rôle utilisé** | admin / assistant / commercial |
| **Navigateur** | Chrome, Edge, Firefox… et sa version |
| **Étapes** | la suite exacte d'actions, depuis la connexion |
| **Attendu / obtenu** | les deux, séparément |
| **Capture d'écran** | si l'anomalie est visuelle |
| **Console du navigateur** | F12 → Console, s'il y a une erreur en rouge |
| **Sortie du serveur** | la fenêtre où `dotnet run` tourne, s'il y a une exception |
| **Reproductible ?** | systématique, ou une seule fois |

Les erreurs côté serveur sont les plus utiles : elles portent la pile d'appels
complète, avec le fichier et la ligne.

---

[← Précédent : Les écrans](03-les-ecrans.md) · [Retour à l'index](README.md) · [Suivant : Architecture technique →](05-architecture-technique.md)

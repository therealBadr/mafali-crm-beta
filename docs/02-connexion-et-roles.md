# 02 — Connexion, comptes et rôles

[← Retour à l'index](README.md)

Ce document explique un comportement qui surprend systématiquement à la
première utilisation. **À lire avant de tester.**

---

## 1. Les comptes sont créés sans mot de passe

Dans la table `users`, la colonne `password_hash` peut être **vide**. Ce n'est
pas une erreur : c'est le signal qui déclenche la définition du mot de passe.

> **Le mot de passe que vous saisissez à votre toute première connexion devient
> votre mot de passe.** Il n'y a pas d'écran « choisissez un mot de passe » :
> vous tapez un identifiant et un mot de passe, l'application constate que le
> compte n'en a pas encore, enregistre celui que vous venez de taper, et vous
> connecte.

Aux connexions suivantes, ce mot de passe est vérifié normalement.

Ce fonctionnement reprend celui de l'ancienne application WinDev (le mécanisme
`MotPasseASaisir` de GPWLogin), pour que la reprise se fasse sans distribuer de
mots de passe provisoires. Le code est dans
`webapp/Services/UserService.cs`, méthode `LoginAsync`.

### Conséquence pour les tests

Le premier qui se connecte sur un compte en fixe le mot de passe. Si deux
testeurs partagent un identifiant, le second devra utiliser le mot de passe
choisi par le premier. Mettez-vous d'accord, ou créez un compte par testeur.

---

## 2. Créer le premier compte

Les comptes sont normalement créés à partir de la table `assistantes` par le
script `database/05_users_and_roles.sql` : un compte par assistante existante,
avec le rôle `admin` pour celles qui avaient l'indicateur `all_filtres`, et
`commercial` pour les autres.

**Sur une base vide, `assistantes` est vide : aucun compte n'est donc créé, et
personne ne peut se connecter.** L'écran de gestion des utilisateurs étant
réservé aux administrateurs, il faut créer le premier compte en SQL :

```sql
-- Un administrateur. Le mot de passe sera défini à la première connexion.
INSERT INTO users (login, password_hash, role)
VALUES ('ADMIN TEST', '', 'admin');
```

Puis, pour couvrir les trois rôles pendant les tests :

```sql
INSERT INTO users (login, password_hash, role) VALUES
    ('ASSISTANT TEST',  '', 'assistant'),
    ('COMMERCIAL TEST', '', 'commercial');
```

Connectez-vous ensuite avec `ADMIN TEST` et le mot de passe de votre choix.

> Les identifiants sont des chaînes libres de la forme « PRÉNOM NOM », en
> majuscules par convention. C'est volontaire : la même chaîne sert de clé dans
> `assistantes.prenom_nom`, `france_optique.assistante_commercial` et
> `filtre_operatrice.nom_operateur`, ce qui évite d'introduire un identifiant
> numérique parallèle.

### Remettre un mot de passe à zéro

Pour repartir d'un compte sans mot de passe (la prochaine connexion le
redéfinira) :

```sql
UPDATE users SET password_hash = '', failed_login_count = 0, lockout_until = NULL
WHERE login = 'ADMIN TEST';
```

### Désactiver un compte

```sql
UPDATE users SET is_active = false WHERE login = 'COMMERCIAL TEST';
```

La connexion affichera alors un message de compte inactif, distinct du message
d'identifiants incorrects.

---

## 3. Verrouillage après échecs

**5 mots de passe erronés consécutifs verrouillent le compte pendant 15
minutes.** Le compteur est remis à zéro par une connexion réussie.

Le verrouillage ne protège que les comptes qui existent : un identifiant
inconnu n'a pas de ligne en base, donc pas de compteur. Le mécanisme freine
donc les tentatives de mot de passe sur un compte réel, pas la recherche
d'identifiants valides.

Pour déverrouiller sans attendre :

```sql
UPDATE users SET failed_login_count = 0, lockout_until = NULL WHERE login = '…';
```

Paramètres dans `webapp/Services/UserService.cs` (`MaxFailedAttempts`,
`LockoutDuration`).

> **⚠ À savoir — `must_change_password` ne force rien**
>
> La colonne existe, elle est écrite à la création et à la réinitialisation d'un
> compte, et effacée à la connexion — mais **aucun écran ne la lit**. Il n'y a
> pas d'écran de changement de mot de passe obligatoire : la connexion aboutit
> quelle que soit sa valeur. Ne vous appuyez pas dessus comme sur une garantie.

---

## 4. Les trois rôles

| Rôle | Pour qui | Principe |
|---|---|---|
| **`admin`** | direction, administrateurs | accès total, y compris la gestion des utilisateurs et l'historique des connexions |
| **`assistant`** | assistantes commerciales | accès à tout le fonctionnel, sans l'administration des comptes |
| **`commercial`** | commerciaux terrain | accès restreint : consultation et saisie sur ses propres clients, pas d'accès aux fichiers de référence |

Le rôle est stocké dans `users.role`, avec une contrainte `CHECK` qui n'accepte
que ces trois valeurs. Ce ne sont pas des rôles configurables : ils sont fixes,
contrairement au moteur de permissions par élément d'interface de l'ancienne
application.

Le rôle est placé dans le cookie d'authentification à la connexion. **Changer le
rôle d'un utilisateur en base ne prend effet qu'à sa prochaine connexion.**
Pensez-y en test : déconnectez-vous et reconnectez-vous après un changement de
rôle.

---

## 5. Tableau des accès par écran

| Écran | Adresse | admin | assistant | commercial |
|---|---|:---:|:---:|:---:|
| Accueil | `/` | ✅ | ✅ | ✅ |
| Parcours Client | `/parcours-client` | ✅ | ✅ | ✅ |
| Rechercher un Client | `/recherche-client` | ✅ | ✅ | ✅ |
| Rappels | `/rappels` | ✅ | ✅ | ✅ |
| Filtres Prédéfinis | `/filtres-predefinis` | ✅ | ✅ | ✅ |
| Bon de Commande | `/bon-commande` | ✅ | ✅ | ✅ |
| Bons de Commande en cours | `/bon-commande-en-cours` | ✅ | ✅ | ✅ |
| Devis en cours | `/devis-en-cours` | ✅ | ✅ | ✅ |
| Fichier des Clients | `/clients` | ✅ | ✅ | ❌ |
| Créer / modifier un client | `/clients/new`, `/clients/{id}/edit` | ✅ | ✅ | ❌ |
| Clients à valider | `/clients-a-valider` | ✅ | ✅ | ❌ |
| Fichier des Familles | `/familles` | ✅ | ✅ | ❌ |
| Fichier des Franchises | `/franchises` | ✅ | ✅ | ❌ |
| Liste Pays | `/pays` | ✅ | ✅ | ❌ |
| Fichier des Assistantes | `/assistantes` | ✅ | ✅ | ❌ |
| Fichier des Filtres | `/fichier-filtres` | ✅ | ✅ | ❌ |
| Fichier des Filtres Historique | `/fichier-filtres-historique` | ✅ | ✅ | ❌ |
| Fichier des Historiques | `/fichier-historique` | ✅ | ✅ | ❌ |
| Créer / modifier un historique | `/historiques/new`, `/historiques/{id}/edit` | ✅ | ✅ | ❌ |
| Définir un Filtre | `/definir-filtre` | ✅ | ✅ | ❌ |
| Définir un Filtre Historique | `/definir-filtre-historique` | ✅ | ✅ | ❌ |
| Ajouter un Client | `/ajouter-client` | ❌ | ❌ | ✅ |
| Mes Clients Ajoutés | `/mes-clients-ajoutes` | ❌ | ❌ | ✅ |
| Mon Compte (gestion des utilisateurs) | `/mon-compte` | ✅ | ❌ | ❌ |
| Historique des Connexions | `/historique-connexions` | ✅ | ❌ | ❌ |

**À noter** : `/ajouter-client` et `/mes-clients-ajoutes` sont réservés au rôle
`commercial`. Un administrateur n'y a **pas** accès — il passe par
`/clients/new` et `/clients-a-valider`, qui sont les équivalents de son côté.
Ce n'est pas une anomalie.

### Comment c'est appliqué

Deux niveaux, tous deux côté serveur :

1. **Par défaut, tout est fermé.** Une règle globale (`FallbackPolicy` dans
   `Program.cs`) exige une authentification sur chaque point d'entrée. Seules
   la page de connexion et le point d'entrée `/login` y échappent
   explicitement. Un écran ajouté plus tard est donc protégé sans rien faire.
2. **Les restrictions de rôle** sont déclarées en haut de chaque page par
   `@attribute [Authorize(Roles = "…")]`. Les pages sans cet attribut sont
   accessibles à tout utilisateur connecté, quel que soit son rôle — c'est le
   cas des huit premières lignes du tableau.

Si un utilisateur connecté demande une adresse interdite à son rôle, il arrive
sur `/access-denied`, qui affiche « accès refusé » — et non sur l'écran de
connexion, qui laisserait croire à une session expirée.

> **⚠ À savoir — masquer n'est pas interdire**
>
> Le menu cache les entrées auxquelles le rôle n'a pas droit, mais c'est de
> l'affichage. La protection réelle vient des deux niveaux ci-dessus. Tester en
> tapant une adresse interdite directement dans la barre d'adresse est un test
> valable : vous devez arriver sur « accès refusé ». Les méthodes
> d'administration des comptes font en plus leur propre vérification côté
> serveur (`EnsureAdminAsync` dans `UserService`).

---

## 6. Suivi des connexions

Chaque connexion réussie ajoute une ligne dans `login_history`
(identifiant + horodatage). Les échecs ne sont **pas** journalisés : c'est un
suivi d'activité, pas un journal de sécurité.

Un administrateur consulte ces lignes via **Historique des Connexions**
(`/historique-connexions`).

---

## 7. Comportement du menu

Toutes les entrées du menu ouvrent l'écran **dans un nouvel onglet**
(`target="_blank"`). C'est volontaire et reproduit la façon de travailler de
l'ancienne application, où chaque fenêtre était indépendante : on garde la
liste des clients ouverte pendant qu'on travaille sur une fiche.

Conséquence pratique en test : vous accumulez vite des onglets. Ce n'est pas un
défaut d'implémentation.

---

[← Précédent : Installation](01-installation.md) · [Retour à l'index](README.md) · [Suivant : Les écrans →](03-les-ecrans.md)

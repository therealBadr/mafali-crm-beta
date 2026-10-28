# 01 — Installation et lancement

[← Retour à l'index](README.md)

---

## 1. Prérequis

| Logiciel | Version | Pourquoi |
|---|---|---|
| **.NET SDK** | 8.0 | L'application cible `net8.0`. Le SDK 9 ou 10 sait aussi compiler du net8.0, mais le SDK 8 est ce qui a servi au développement. |
| **PostgreSQL** | **14 ou plus** | La base. Les scripts eux-mêmes se contentent de la version 10, mais l'échantillon (`echantillon/`) a été produit par `pg_dump` 14 : une restauration vers une version antérieure échoue. |

Vérification :

```bash
dotnet --version
psql --version
```

> **Si `psql` répond « commande introuvable », c'est normal :** l'installateur
> PostgreSQL sous Windows n'ajoute pas ses outils en ligne de commande au PATH.
> Les binaires (`psql`, `createdb`, `pg_restore`) sont dans
> `C:\Program Files\PostgreSQL\<version>\bin`. Ajoutez ce dossier au PATH, ou
> préfixez les commandes de ce document par ce chemin.
>
> Sous Linux, installez `postgresql-client` ; les outils y sont déjà dans le PATH,
> mais la connexion locale se fait par défaut en authentification `peer` — il faut
> donc soit passer par `sudo -u postgres`, soit définir un mot de passe sur le rôle
> `postgres` pour pouvoir se connecter en TCP comme le fait l'application.

Aucun autre outil n'est nécessaire : pas de Node.js, pas de npm, pas de
compilation front-end. Le CSS et le JavaScript sont écrits à la main et servis
tels quels.

---

## Le plus court chemin : restaurer l'échantillon

Si vous voulez simplement voir l'application fonctionner, **n'appliquez pas les
28 scripts** : restaurez l'échantillon fourni, qui contient déjà le schéma,
405 clients, 25 245 interactions et 72 pièces jointes.

```bash
createdb -U postgres -E UTF8 mafali_crm_db
```

```bash
pg_restore -U postgres -d mafali_crm_db echantillon/echantillon_mafali.dump
```

Puis passez directement à l'étape 4 (chaîne de connexion). Tous les comptes ont
un mot de passe vide : celui que vous saisissez à la première connexion devient
le mot de passe. Détails et contenu exact :
[../echantillon/README.md](../echantillon/README.md).

Les étapes 2 et 3 ci-dessous décrivent l'autre chemin — partir d'une base
**vide** — qui reste la référence pour comprendre le schéma et pour une
installation réelle.

---

## 2. Créer la base de données

```bash
createdb -U postgres mafali_crm_db
```

Ou depuis `psql` :

```sql
CREATE DATABASE mafali_crm_db;
```

---

## 3. Appliquer les scripts SQL

Les scripts sont dans `database/`, numérotés de `01` à `28`. **Il n'y a pas de
système de migration automatique** : les scripts s'appliquent à la main, dans
l'ordre numérique. Chaque fichier commence par un commentaire qui explique ce
qu'il fait et pourquoi.

### Cas courant : base vide, pour tester

Appliquez `01`, puis `03` à `28`. **Sautez `02_foreign_keys.sql` pour l'instant**
(voir l'encadré ci-dessous), ou appliquez-le aussi si vous saisissez toutes vos
données à la main.

Sous Windows (PowerShell), depuis la racine du dépôt :

```powershell
Get-ChildItem database\*.sql | Sort-Object Name | ForEach-Object {
    Write-Host "-> $($_.Name)"
    psql -U postgres -d mafali_crm_db -f $_.FullName
}
```

Sous Git Bash ou Linux :

```bash
for f in database/*.sql; do echo "-> $f"; psql -U postgres -d mafali_crm_db -f "$f"; done
```

> **⚠ À savoir — le cas particulier de `02_foreign_keys.sql`**
>
> Ce script ajoute les clés étrangères en `NOT VALID`, c'est-à-dire sans
> vérifier les lignes déjà présentes. C'est volontaire : les données réelles
> issues de HFSQL contiennent des fautes de frappe et des valeurs vides dans
> `famille`, `franchise` et `pays`, qui feraient échouer une vérification
> stricte.
>
> - **Si vous partez d'une base vide et saisissez les données à la main** :
>   appliquez `02` avec les autres. Les clés étrangères seront respectées pour
>   tout ce que vous créerez, ce qui est le comportement souhaitable en test.
> - **Si vous comptez importer les données de l'ancienne application ensuite** :
>   appliquez `02` *après* l'import, sinon l'import sera rejeté par les
>   contraintes. Le script le dit lui-même dans son en-tête.
>
> Dans les deux cas, `VALIDATE CONSTRAINT` se lance plus tard, table par table,
> une fois les incohérences corrigées.

### Vérifier que tout est passé

```sql
\dt
```

Vous devez voir 11 tables : `assistantes`, `ca`, `filtre_historique`,
`filtre_operatrice`, `france_optique`, `historique`, `login_history`, `pays`,
`type_famille`, `type_franchise`, `users` — plus `parametrage` et
`fiche_a_supprimer` si vous avez appliqué le script `28`.

Le détail de chaque script est dans [06 — Base de données](06-base-de-donnees.md).

---

## 4. Renseigner la chaîne de connexion

Ouvrez `webapp/appsettings.Development.json` et remplacez `A_REMPLACER` par le
mot de passe de votre utilisateur PostgreSQL :

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=mafali_crm_db;Username=postgres;Password=A_REMPLACER"
  }
}
```

> **Ne versionnez pas un mot de passe réel.** Pour une installation durable,
> préférez les *user secrets* de .NET, qui restent sur la machine :
>
> ```bash
> cd webapp
> dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Database=mafali_crm_db;Username=postgres;Password=…"
> ```
>
> Le projet a déjà un identifiant de secrets configuré dans son `.csproj`.
> `appsettings.Local.json` et `appsettings.Production.json` sont également
> ignorés par git si vous préférez cette voie.

---

## 5. Lancer l'application

### En développement

```bash
dotnet run --project webapp --launch-profile http
```

→ **http://localhost:5297**

Le profil `https` existe aussi (`https://localhost:7077`), mais il demande un
certificat de développement :

```bash
dotnet dev-certs https --trust
```

### Compiler sans lancer

```bash
dotnet build MafaliCrm.sln
```

---

## 6. Déploiement sur le réseau local

L'application est conçue pour tourner sur un poste du bureau, lancée en
double-cliquant l'exécutable, et être consultée depuis les autres postes.

### Produire le paquet

```bash
dotnet publish webapp -c Release -r win-x64 --self-contained true -o publish
```

`--self-contained` embarque le runtime .NET : la machine cible n'a pas besoin
d'avoir .NET installé.

### Ce qui se passe au lancement

Hors environnement de développement, l'application écoute sur
**`http://0.0.0.0:5298`** — toutes les interfaces réseau, pas seulement
`localhost`. C'est ce qui la rend joignable depuis les autres postes à l'adresse
`http://<IP-du-poste>:5298`. Ce comportement est codé dans `webapp/Program.cs`.

### À prévoir sur la machine qui héberge

1. **Chaîne de connexion** : via *user secrets*, une variable
   d'environnement `ConnectionStrings__Default`, ou un
   `appsettings.Production.json` déposé à côté de l'exécutable.
2. **Pare-feu Windows** : autoriser le port 5298 en entrée.
3. **Le dossier `AppData/`** se crée tout seul à côté de l'exécutable. Il
   contient les clés de chiffrement des cookies de session et les modèles de
   Bon de Commande. **À sauvegarder** : perdre les clés déconnecte tout le monde.

> **⚠ À savoir — pas de HTTPS**
>
> Le déploiement est en HTTP simple, volontairement : un certificat réel n'est
> pas praticable pour une application interne, et un certificat auto-signé
> devrait être approuvé à la main sur chaque poste. `UseHttpsRedirection()` est
> présent dans le code mais sans effet faute de point d'écoute HTTPS — il
> journalise un avertissement au démarrage et laisse passer la requête. Ce n'est
> pas une anomalie.

---

## 7. Problèmes fréquents au premier lancement

| Symptôme | Cause | Solution |
|---|---|---|
| Erreur de connexion à la base au premier affichage | chaîne de connexion absente ou fausse | étape 4 ci-dessus |
| `relation "france_optique" does not exist` | scripts SQL non appliqués | étape 3 |
| `password authentication failed` | mot de passe PostgreSQL incorrect | vérifier avec `psql -U postgres` |
| La page de connexion s'affiche mais refuse tous les comptes | aucun compte en base | voir [02 — Connexion](02-connexion-et-roles.md) § « Créer le premier compte » |
| Tout le monde est déconnecté après un redémarrage | dossier `AppData/DataProtectionKeys` supprimé ou non accessible en écriture | vérifier les droits sur `AppData/` |
| Avertissement `Failed to determine the https port` | normal en HTTP simple | aucune — voir l'encadré ci-dessus |

---

[← Retour à l'index](README.md) · [Suivant : Connexion, comptes et rôles →](02-connexion-et-roles.md)

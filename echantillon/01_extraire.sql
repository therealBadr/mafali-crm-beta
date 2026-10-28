-- =====================================================================
-- 01_extraire.sql -- extrait un echantillon de la base de production.
--
-- LECTURE SEULE : ne cree que des tables TEMPORAIRES, n'ecrit rien dans
-- la base interrogee. Produit un CSV par table dans echantillon/data/.
--
--   set PGCLIENTENCODING=UTF8
--   psql -U postgres -d mafali_crm_db_new -f echantillon/01_extraire.sql
--
-- Les tables de reference partent EN ENTIER : elles sont minuscules, et
-- les prendre toutes evite toute violation de cle etrangere au chargement.
-- =====================================================================
\set ON_ERROR_STOP on
\set d 'C:/Users/Badr/crm-mafali/echantillon/data'

-- Les logins reels. On privilegie les clients dont assistante_commercial
-- correspond a un compte, sinon les ecrans par utilisateur (Rappels,
-- Devis en cours, filtres par operatrice) restent vides dans l'echantillon.
CREATE TEMP TABLE compte AS
SELECT DISTINCT lower(btrim(login)) AS login FROM users;

-- Les plus petites pieces jointes de chaque extension, pour couvrir
-- pdf / xlsx / jpg / png sans faire exploser le volume.
CREATE TEMP TABLE piece AS
SELECT id_histo, num_client, ext, n FROM (
    SELECT h.id_histo, h.num_client,
           lower(regexp_replace(h.fic_stk_nom, '^.*\.', '')) AS ext,
           octet_length(h.fic_stk) AS n,
           row_number() OVER (
               PARTITION BY lower(regexp_replace(h.fic_stk_nom, '^.*\.', ''))
               ORDER BY octet_length(h.fic_stk)) AS rn
    FROM historique h
    WHERE h.fic_stk IS NOT NULL AND h.fic_stk_nom IS NOT NULL
      AND octet_length(h.fic_stk) BETWEEN 8000 AND 400000
) t WHERE rn <= 18;

-- Un panier de clients temoins par caracteristique a tester.
CREATE TEMP TABLE seed (cle_opl bigint PRIMARY KEY, motif text);

-- Le JOIN sur france_optique n'est pas decoratif : 172 481 lignes
-- d'historique de production referencent un num_client qui n'existe pas
-- dans france_optique (15 474 clients absents). Les quatre cles etrangeres
-- sont NOT VALID, donc PostgreSQL ne l'a jamais signale. Sans ce filtre,
-- l'echantillon embarquerait des lignes orphelines.
INSERT INTO seed
SELECT DISTINCT p.num_client, 'piece_jointe'
FROM piece p JOIN france_optique fo ON fo.cle_opl = p.num_client
ON CONFLICT DO NOTHING;

INSERT INTO seed
SELECT fo.cle_opl, 'historique_riche' FROM france_optique fo
JOIN compte c ON c.login = lower(btrim(fo.assistante_commercial))
JOIN (SELECT num_client, count(*) n FROM historique GROUP BY 1) h ON h.num_client = fo.cle_opl
WHERE h.n BETWEEN 20 AND 120 ORDER BY h.n DESC LIMIT 60
ON CONFLICT DO NOTHING;

INSERT INTO seed
SELECT fo.cle_opl, 'rappel_retard' FROM france_optique fo
JOIN compte c ON c.login = lower(btrim(fo.assistante_commercial))
WHERE fo.date_rappel < current_date ORDER BY fo.date_rappel DESC LIMIT 40
ON CONFLICT DO NOTHING;

INSERT INTO seed
SELECT fo.cle_opl, 'rappel_a_venir' FROM france_optique fo
JOIN compte c ON c.login = lower(btrim(fo.assistante_commercial))
WHERE fo.date_rappel >= current_date ORDER BY fo.date_rappel LIMIT 25
ON CONFLICT DO NOTHING;

INSERT INTO seed SELECT cle_opl, 'bloque' FROM france_optique WHERE bloque
ON CONFLICT DO NOTHING;

INSERT INTO seed SELECT cle_opl, 'rappel_rdv' FROM france_optique WHERE rappel_rdv
ORDER BY cle_opl LIMIT 15
ON CONFLICT DO NOTHING;

-- Meme precaution que pour `piece` : 397 lignes de ca referencent un
-- cle_opl absent de france_optique (263 clients).
INSERT INTO seed SELECT DISTINCT c.cle_opl, 'chiffre_affaires'
FROM ca c JOIN france_optique fo ON fo.cle_opl = c.cle_opl
ORDER BY c.cle_opl LIMIT 30
ON CONFLICT DO NOTHING;

INSERT INTO seed SELECT cle_opl, 'franchises_multiples' FROM france_optique
WHERE franchise2 IS NOT NULL ORDER BY cle_opl LIMIT 20
ON CONFLICT DO NOTHING;

INSERT INTO seed SELECT cle_opl, 'n_adherent' FROM france_optique
WHERE n_adherent IS NOT NULL
ON CONFLICT DO NOTHING;

INSERT INTO seed SELECT cle_opl, 'accents' FROM france_optique
WHERE raison_sociale ~ '[^[:ascii:]]' ORDER BY cle_opl LIMIT 25
ON CONFLICT DO NOTHING;

INSERT INTO seed SELECT cle_opl, 'statuts' FROM (
    SELECT cle_opl, row_number() OVER (
        PARTITION BY etat_client, status_vente, statuts_clients ORDER BY cle_opl) rn
    FROM france_optique) t WHERE rn <= 2
ON CONFLICT DO NOTHING;

INSERT INTO seed SELECT cle_opl, 'sans_assistante' FROM france_optique
WHERE coalesce(btrim(assistante_commercial), '') = '' ORDER BY cle_opl LIMIT 15
ON CONFLICT DO NOTHING;

INSERT INTO seed SELECT cle_opl, 'divers' FROM france_optique ORDER BY cle_opl LIMIT 30
ON CONFLICT DO NOTHING;

\echo ''
\echo '--- Clients retenus par motif ---'
SELECT motif, count(*) FROM seed GROUP BY motif ORDER BY 2 DESC;

\echo '--- Volume ---'
SELECT (SELECT count(*) FROM seed) AS clients,
       (SELECT count(*) FROM historique WHERE num_client IN (SELECT cle_opl FROM seed)) AS historique,
       (SELECT count(*) FROM ca WHERE cle_opl IN (SELECT cle_opl FROM seed)) AS ca,
       (SELECT count(*) FROM piece) AS pieces_retenues,
       (SELECT pg_size_pretty(sum(n)::bigint) FROM piece) AS poids_retenu,
       (SELECT pg_size_pretty(coalesce(sum(octet_length(fic_stk)),0)::bigint) FROM historique
         WHERE num_client IN (SELECT cle_opl FROM seed) AND fic_stk IS NOT NULL) AS poids_brut;

\echo '--- Export des CSV ---'
\copy (SELECT * FROM type_famille) TO 'C:/Users/Badr/crm-mafali/echantillon/data/type_famille.csv' CSV
\copy (SELECT * FROM type_franchise) TO 'C:/Users/Badr/crm-mafali/echantillon/data/type_franchise.csv' CSV
\copy (SELECT * FROM pays) TO 'C:/Users/Badr/crm-mafali/echantillon/data/pays.csv' CSV
\copy (SELECT * FROM assistantes) TO 'C:/Users/Badr/crm-mafali/echantillon/data/assistantes.csv' CSV
\copy (SELECT * FROM users) TO 'C:/Users/Badr/crm-mafali/echantillon/data/users.csv' CSV
\copy (SELECT * FROM filtre_operatrice) TO 'C:/Users/Badr/crm-mafali/echantillon/data/filtre_operatrice.csv' CSV
\copy (SELECT * FROM parametrage) TO 'C:/Users/Badr/crm-mafali/echantillon/data/parametrage.csv' CSV
\copy (SELECT fo.* FROM france_optique fo JOIN seed s ON s.cle_opl = fo.cle_opl ORDER BY fo.cle_opl) TO 'C:/Users/Badr/crm-mafali/echantillon/data/france_optique.csv' CSV
\copy (SELECT c.* FROM ca c JOIN seed s ON s.cle_opl = c.cle_opl ORDER BY c.cle_opl, c.annee) TO 'C:/Users/Badr/crm-mafali/echantillon/data/ca.csv' CSV
-- La piece jointe n'est conservee que sur les lignes retenues dans `piece`.
-- Les clients temoins portent 4,5 Go de pieces jointes au total : tout
-- prendre produirait un CSV de ~9 Go (l'hexadecimal double le volume).
-- fic_stk ET fic_stk_nom passent a NULL ensemble : un nom de fichier sans
-- contenu afficherait une piece jointe introuvable.
-- Liste de colonnes generee depuis information_schema, donc exhaustive et
-- dans l'ordre de la table -- a regenerer si une colonne est ajoutee.
\copy (SELECT h.id_histo, h.num_client, h.date_saisie, h.heure_saisie, h.assistante_commercial, h.date_rappel, h.heure_rappel, h.operation, h.status_vente, h.note, h.franchise, h.raison_sociale, h.cp, h.ville, h.statuts_clients, h.magasin_principal, CASE WHEN p.id_histo IS NULL THEN NULL ELSE h.fic_stk END AS fic_stk, h.deleted_at, CASE WHEN p.id_histo IS NULL THEN NULL ELSE h.fic_stk_nom END AS fic_stk_nom, h.n_adherent, h.franchise2, h.franchise3, h.franchise4, h.n_adherent2, h.n_adherent3, h.n_adherent4, h.id_histo_source FROM historique h JOIN seed s ON s.cle_opl = h.num_client LEFT JOIN piece p ON p.id_histo = h.id_histo ORDER BY h.num_client, h.id_histo) TO 'C:/Users/Badr/crm-mafali/echantillon/data/historique.csv' CSV

\echo '--- Termine ---'

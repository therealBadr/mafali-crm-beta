-- =====================================================================
-- 02_completer.sql -- rend l'echantillon chargeable et utilisable.
--
-- A lancer contre mafali_echantillon APRES le chargement des CSV et
-- AVANT 02_foreign_keys.sql :
--
--   set PGCLIENTENCODING=UTF8
--   psql -U postgres -d mafali_echantillon -f echantillon/02_completer.sql
--
-- Ce script N'INVENTE AUCUNE DONNEE. Il ne fait que deux choses :
--   1. ecarter les lignes orphelines heritees de la production ;
--   2. neutraliser les mots de passe, pour ne pas livrer de materiel
--      d'authentification reel.
-- Tout le reste de l'echantillon provient de la production, tel quel.
-- =====================================================================
\set ON_ERROR_STOP on
BEGIN;

-- ---------------------------------------------------------------------
-- 1. Lignes orphelines
--
-- La production contient 172 481 lignes d'historique et 397 lignes de ca
-- dont le client n'existe pas dans france_optique. Les quatre cles
-- etrangeres de 02_foreign_keys.sql sont NOT VALID, donc PostgreSQL ne
-- l'a jamais signale. 01_extraire.sql filtre deja les clients temoins,
-- mais ce nettoyage garantit un echantillon coherent meme si l'etat de
-- la production evolue -- sans quoi VALIDATE CONSTRAINT echouerait.
--
-- C'est un filtrage, pas une invention : aucune ligne n'est modifiee,
-- seules les lignes sans client sont ecartees.
-- ---------------------------------------------------------------------
DELETE FROM historique h
 WHERE NOT EXISTS (SELECT 1 FROM france_optique fo WHERE fo.cle_opl = h.num_client);

DELETE FROM ca c
 WHERE NOT EXISTS (SELECT 1 FROM france_optique fo WHERE fo.cle_opl = c.cle_opl);

-- ---------------------------------------------------------------------
-- 2. Mots de passe
--
-- Les hash PBKDF2 reels des comptes du personnel ne sont pas livres :
-- c'est du materiel d'authentification. Un password_hash vide declenche
-- le mecanisme deja documente (UserService.LoginAsync) : la PREMIERE
-- connexion definit le mot de passe. L'echantillon est donc utilisable
-- immediatement, avec n'importe quel compte reel.
--
-- Aucune autre colonne de users n'est touchee : logins, roles et
-- is_active restent ceux de la production.
-- ---------------------------------------------------------------------
UPDATE users
   SET password_hash = '', must_change_password = true,
       failed_login_count = 0, lockout_until = NULL;

COMMIT;

\echo ''
\echo '--- Etat de l-echantillon (100% donnees de production) ---'
SELECT 'clients' AS element, count(*)::text AS n FROM france_optique
UNION ALL SELECT '  dont bloque', count(*)::text FROM france_optique WHERE bloque
UNION ALL SELECT '  avec rappel', count(*)::text FROM france_optique WHERE date_rappel IS NOT NULL
UNION ALL SELECT '    dont en retard', count(*)::text FROM france_optique WHERE date_rappel < current_date
UNION ALL SELECT '    dont a venir', count(*)::text FROM france_optique WHERE date_rappel >= current_date
UNION ALL SELECT '  avec rappel_rdv', count(*)::text FROM france_optique WHERE rappel_rdv
UNION ALL SELECT '  avec franchise2', count(*)::text FROM france_optique WHERE franchise2 IS NOT NULL
UNION ALL SELECT '  avec n_adherent', count(*)::text FROM france_optique WHERE n_adherent IS NOT NULL
UNION ALL SELECT '  raison sociale accentuee', count(*)::text FROM france_optique WHERE raison_sociale ~ '[^[:ascii:]]'
UNION ALL SELECT 'historique', count(*)::text FROM historique
UNION ALL SELECT '  avec piece jointe', count(*)::text FROM historique WHERE fic_stk IS NOT NULL
UNION ALL SELECT 'chiffre affaires', count(*)::text FROM ca
UNION ALL SELECT 'filtres enregistres', count(*)::text FROM filtre_operatrice
UNION ALL SELECT 'comptes', count(*)::text FROM users
UNION ALL SELECT '  admin', count(*)::text FROM users WHERE role='admin'
UNION ALL SELECT '  commercial', count(*)::text FROM users WHERE role='commercial'
UNION ALL SELECT '  assistant', count(*)::text FROM users WHERE role='assistant'
UNION ALL SELECT '  hash non vide (doit etre 0)', count(*)::text FROM users WHERE password_hash <> ''
UNION ALL SELECT 'orphelins (doit etre 0)',
       (SELECT count(*) FROM historique h WHERE NOT EXISTS (SELECT 1 FROM france_optique fo WHERE fo.cle_opl=h.num_client))::text;

\echo ''
\echo 'Colonnes vides en production, donc vides ici :'
\echo '  validation_status autre que validated, pas_emailing, magasin_ferme,'
\echo '  siret, siren, tva, email2, historique.deleted_at, role assistant.'
\echo 'Voir echantillon/README.md, section "Ce que l-echantillon ne permet pas de tester".'

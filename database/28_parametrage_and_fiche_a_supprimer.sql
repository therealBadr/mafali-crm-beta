-- Deux tables presentes en production HFSQL, pour la fidelite de la reprise.
-- L'application ne lit ni l'une ni l'autre.

\set ON_ERROR_STOP on

BEGIN;

CREATE TABLE IF NOT EXISTS parametrage (
    id_parametrage        INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    id_parametrage_source INTEGER,
    categorie             VARCHAR(50)  NOT NULL,
    cle                   VARCHAR(50)  NOT NULL,
    valeur                TEXT,
    CONSTRAINT parametrage_categorie_cle_key UNIQUE (categorie, cle)
);

COMMENT ON TABLE parametrage IS
    'Application settings carried from HFSQL parametrage. Five rows as of '
    '2026-10-26, four of them Bon de Commande paths and one the purchase-order '
    'counter. Upserted on (categorie, cle).';

COMMENT ON COLUMN parametrage.id_parametrage_source IS
    'HFSQL parametrage.idparametrage. Kept for traceability only -- these rows '
    'are read by (categorie, cle), never by number.';

ALTER TABLE france_optique ADD COLUMN IF NOT EXISTS fiche_a_supprimer BIGINT;

COMMENT ON COLUMN france_optique.fiche_a_supprimer IS
    'HFSQL France_Optique.fiche_a_supprimer: the legacy CRM''s own flag marking '
    'a client record for deletion. 0 for 87,382 clients and 1 for one as of '
    '2026-10-26. NULL means the row has no legacy source. This is the legacy '
    'flag only -- it is not the app''s soft delete, which is deleted_at.';

DO $check$
DECLARE
    n_param   BIGINT;
    n_flagged BIGINT;
BEGIN
    SELECT count(*) INTO n_param FROM parametrage;
    SELECT count(*) INTO n_flagged
      FROM france_optique WHERE fiche_a_supprimer IS NOT NULL
                            AND fiche_a_supprimer <> 0;
    RAISE NOTICE 'parametrage has % row(s); % client(s) flagged for deletion',
                 n_param, n_flagged;
    IF n_param = 0 THEN
        RAISE NOTICE 'parametrage is empty. Expected before the first import '
                     'that includes it -- but if the app is already issuing '
                     'Bons de Commande, check its counter against the legacy '
                     'value before anyone sends one out.';
    END IF;
END
$check$;

COMMIT;

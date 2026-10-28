-- Cle sur laquelle l'import HFSQL se raccorde. L'application ne lit pas cette colonne.

\set ON_ERROR_STOP on
BEGIN;
ALTER TABLE historique ADD COLUMN IF NOT EXISTS id_histo_source BIGINT;
COMMENT ON COLUMN historique.id_histo_source IS
    'HFSQL Historique.id_histo. NULL means the row has no legacy source (created '
    'in this app) or its source id was unusable. The import upserts on this '
    'column; id_histo is a surrogate and cannot serve.';
CREATE UNIQUE INDEX IF NOT EXISTS historique_id_histo_source_key
    ON historique (id_histo_source);
COMMIT;

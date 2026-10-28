CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_france_optique_raison_cle
    ON france_optique (raison_sociale, cle_opl);

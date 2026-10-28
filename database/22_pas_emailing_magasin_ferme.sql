ALTER TABLE france_optique
    ADD COLUMN pas_emailing BOOLEAN NOT NULL DEFAULT false,
    ADD COLUMN magasin_ferme VARCHAR(30);

ALTER TABLE france_optique
    ADD CONSTRAINT france_optique_magasin_ferme_check
    CHECK (magasin_ferme IS NULL
           OR magasin_ferme IN ('Fermé temporairement', 'Fermé définitivement'));

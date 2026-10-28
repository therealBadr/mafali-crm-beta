ALTER TABLE filtre_historique
    ADD COLUMN nom_operateur VARCHAR(50) NOT NULL;

ALTER TABLE filtre_historique
    DROP CONSTRAINT filtre_historique_pkey;

ALTER TABLE filtre_historique
    ADD PRIMARY KEY (nom_operateur, nom_filtre);

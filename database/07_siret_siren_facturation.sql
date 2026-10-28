ALTER TABLE france_optique
    ADD COLUMN siret VARCHAR(14),
    ADD COLUMN siren VARCHAR(9),
    ADD COLUMN facturation_electronique BOOLEAN NOT NULL DEFAULT false;

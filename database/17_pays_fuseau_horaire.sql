ALTER TABLE pays ADD COLUMN fuseau_horaire VARCHAR(50);

UPDATE pays SET fuseau_horaire = 'Europe/Berlin'     WHERE nom_pays = 'Allemagne';
UPDATE pays SET fuseau_horaire = 'Europe/Andorra'    WHERE nom_pays = 'Andorre';
UPDATE pays SET fuseau_horaire = 'Europe/Brussels'   WHERE nom_pays = 'Belgique';
UPDATE pays SET fuseau_horaire = 'Europe/Madrid'     WHERE nom_pays = 'Espagne';
UPDATE pays SET fuseau_horaire = 'Europe/Paris'      WHERE nom_pays = 'France';
UPDATE pays SET fuseau_horaire = 'Europe/Luxembourg' WHERE nom_pays = 'Luxembourg';
UPDATE pays SET fuseau_horaire = 'Africa/Casablanca' WHERE nom_pays = 'Maroc';
UPDATE pays SET fuseau_horaire = 'Europe/Monaco'     WHERE nom_pays = 'Monaco';
UPDATE pays SET fuseau_horaire = 'Europe/Amsterdam'  WHERE nom_pays = 'Pays-Bas';
UPDATE pays SET fuseau_horaire = 'Europe/London'     WHERE nom_pays = 'Royaume-Uni';
UPDATE pays SET fuseau_horaire = 'Europe/Zurich'     WHERE nom_pays = 'Suisse';

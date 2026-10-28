ALTER TABLE france_optique ALTER COLUMN statuts_clients SET DEFAULT '-1';
ALTER TABLE historique ALTER COLUMN statuts_clients SET DEFAULT '-1';

UPDATE france_optique SET statuts_clients = '-1' WHERE statuts_clients = '0';

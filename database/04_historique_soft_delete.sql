ALTER TABLE historique ADD COLUMN deleted_at TIMESTAMP NULL;

CREATE INDEX idx_historique_deleted_at ON historique (deleted_at);

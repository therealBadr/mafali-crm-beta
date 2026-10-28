ALTER TABLE france_optique
    ADD COLUMN validation_status VARCHAR(20) NOT NULL DEFAULT 'pending'
        CHECK (validation_status IN ('pending', 'validated', 'refused')),
    ADD COLUMN added_by        VARCHAR(100) NULL REFERENCES users(login) ON UPDATE CASCADE,
    ADD COLUMN validated_by    VARCHAR(100) NULL REFERENCES users(login) ON UPDATE CASCADE,
    ADD COLUMN validated_at    TIMESTAMP NULL,
    ADD COLUMN refusal_reason  VARCHAR(250) NULL;

UPDATE france_optique SET validation_status = 'validated';

CREATE INDEX idx_france_optique_validation_status ON france_optique (validation_status);

CREATE INDEX idx_france_optique_added_by ON france_optique (added_by);

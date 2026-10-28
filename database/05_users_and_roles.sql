CREATE TABLE users (
    login                 VARCHAR(100) PRIMARY KEY,
    password_hash         VARCHAR(255) NOT NULL,
    role                  VARCHAR(20) NOT NULL CHECK (role IN ('admin', 'assistant', 'commercial')),
    must_change_password  BOOLEAN NOT NULL DEFAULT true,
    is_active             BOOLEAN NOT NULL DEFAULT true,
    last_login_at         TIMESTAMP NULL,
    created_at            TIMESTAMP NOT NULL DEFAULT now()
);

INSERT INTO users (login, password_hash, role, must_change_password)
SELECT
    prenom_nom,
    '',
    CASE WHEN all_filtres THEN 'admin' ELSE 'commercial' END,
    true
FROM assistantes;

COMMENT ON COLUMN assistantes.all_filtres IS
    'Deprecated 2026-08-14: superseded by users.role (admin/assistant see all, commercial sees own only). Kept until the app is fully switched over to reading users.role.';

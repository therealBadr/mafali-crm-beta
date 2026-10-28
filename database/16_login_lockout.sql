ALTER TABLE users
    ADD COLUMN failed_login_count INT NOT NULL DEFAULT 0,
    ADD COLUMN lockout_until TIMESTAMP NULL;

CREATE TABLE login_history (
    id           BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    login        VARCHAR(100) NOT NULL REFERENCES users (login),
    logged_in_at TIMESTAMP NOT NULL DEFAULT now()
);

CREATE INDEX idx_login_history_login ON login_history (login);
CREATE INDEX idx_login_history_logged_in_at ON login_history (logged_in_at);

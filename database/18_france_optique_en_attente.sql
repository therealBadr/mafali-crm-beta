CREATE TABLE france_optique_en_attente (
    id_attente              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    famille                 VARCHAR(30),
    raison_sociale          VARCHAR(60),
    complement              VARCHAR(60),
    franchise               VARCHAR(50),
    franchise2              VARCHAR(50),
    franchise3              VARCHAR(50),
    franchise4              VARCHAR(50),
    rue                     VARCHAR(60),
    localisation_1          VARCHAR(60),
    localisation_2          VARCHAR(60),
    cp                      VARCHAR(5),
    ville                   VARCHAR(40),
    pays                    VARCHAR(30),
    telephone               VARCHAR(20),
    tel_bis                 VARCHAR(20),
    portable                VARCHAR(20),
    fax                     VARCHAR(20),
    email                   VARCHAR(50),
    assistante_commercial   VARCHAR(50),
    responsable_achat       VARCHAR(50),
    representant            VARCHAR(100),
    op_en_cours             VARCHAR(50),
    status_vente            VARCHAR(50),
    statuts_clients         VARCHAR(50) DEFAULT '0',
    etat_client             VARCHAR(50) DEFAULT '0',
    production              VARCHAR(50),
    magasin_principal       VARCHAR(50),
    note                    VARCHAR(250),
    note_perm               TEXT,

    submitted_by            VARCHAR(100) NOT NULL REFERENCES users(login) ON UPDATE CASCADE,
    submitted_at            TIMESTAMP NOT NULL DEFAULT now(),
    status                  VARCHAR(20) NOT NULL DEFAULT 'pending'
                                CHECK (status IN ('pending', 'approved', 'rejected')),
    reviewed_by             VARCHAR(100) NULL REFERENCES users(login) ON UPDATE CASCADE,
    reviewed_at             TIMESTAMP NULL,
    rejection_reason        VARCHAR(250) NULL,
    approved_cle_opl        BIGINT NULL REFERENCES france_optique(cle_opl)
);

CREATE INDEX idx_france_optique_en_attente_status ON france_optique_en_attente (status);

CREATE INDEX idx_france_optique_en_attente_submitted_by ON france_optique_en_attente (submitted_by);

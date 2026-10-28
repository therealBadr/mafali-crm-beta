CREATE TABLE type_famille (
    nom_famille VARCHAR(30) PRIMARY KEY
);

CREATE TABLE type_franchise (
    nom_franchise VARCHAR(50) PRIMARY KEY
);

CREATE TABLE pays (
    id_pays   BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nom_pays  VARCHAR(50) NOT NULL UNIQUE,
    indicatif VARCHAR(50),
    masque    VARCHAR(50)
);

CREATE TABLE assistantes (
    prenom_nom  VARCHAR(100) PRIMARY KEY,
    service     VARCHAR(30),
    all_filtres BOOLEAN NOT NULL DEFAULT false,
    commentaire VARCHAR(50)
);

CREATE TABLE france_optique (
    cle_opl                BIGINT PRIMARY KEY CHECK (cle_opl >= 0),
    famille                VARCHAR(30),
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
    date_saisie             DATE,
    heure_saisie            TIME,
    date_rappel             DATE,
    heure_rappel            TIME,
    note                    VARCHAR(250),
    note_perm               TEXT,
    bloque                  BOOLEAN NOT NULL DEFAULT false,
    rappel_rdv              BOOLEAN NOT NULL DEFAULT false
);

CREATE INDEX idx_france_optique_date_heure_rappel ON france_optique (date_rappel, heure_rappel);
CREATE INDEX idx_france_optique_famille   ON france_optique (famille);
CREATE INDEX idx_france_optique_franchise ON france_optique (franchise);
CREATE INDEX idx_france_optique_pays      ON france_optique (pays);

CREATE TABLE historique (
    id_histo                BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    num_client              BIGINT NOT NULL,
    date_saisie             DATE,
    heure_saisie            TIME,
    assistante_commercial   VARCHAR(50),
    date_rappel             DATE,
    heure_rappel            TIME,
    operation                VARCHAR(50),
    status_vente             VARCHAR(50),
    note                     VARCHAR(250),
    franchise                VARCHAR(50),
    raison_sociale           VARCHAR(60),
    cp                       VARCHAR(5),
    ville                    VARCHAR(40),
    statuts_clients          VARCHAR(50) DEFAULT '0',
    magasin_principal        VARCHAR(50),
    fic_stk                  BYTEA
);

CREATE INDEX idx_historique_num_client ON historique (num_client);

CREATE TABLE ca (
    id_ca   BIGINT GENERATED ALWAYS AS IDENTITY,
    cle_opl BIGINT NOT NULL,
    annee   INTEGER NOT NULL DEFAULT 0,
    ca      INTEGER DEFAULT 0,
    PRIMARY KEY (cle_opl, annee)
);

CREATE TABLE filtre_operatrice (
    nom_operateur VARCHAR(50) NOT NULL,
    nom_filtre    VARCHAR(50) NOT NULL,
    filtre_reel   VARCHAR(512),
    PRIMARY KEY (nom_operateur, nom_filtre)
);

CREATE TABLE filtre_historique (
    nom_filtre  VARCHAR(50) PRIMARY KEY,
    filtre_reel VARCHAR(512)
);

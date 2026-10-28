-- Demarre a 200000 pour rester a l'ecart des deux plages presentes dans les donnees
-- reelles (max observe 185644, et quelques clients pres de 999999999).

CREATE SEQUENCE IF NOT EXISTS france_optique_cle_opl_seq;
SELECT setval('france_optique_cle_opl_seq', 200000, false);

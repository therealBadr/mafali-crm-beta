-- Remise a zero ponctuelle. setval(..., false) fait que le prochain nextval() rend
-- exactement 50000, et non 50001.

SELECT setval('bon_commande_number_seq', 50000, false);

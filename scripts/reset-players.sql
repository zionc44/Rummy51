-- Development only. Empties every account and everything hanging off one, so
-- the next sign-up starts from nothing. TRUNCATE takes all five tables in one
-- statement, which is what lets it ignore the foreign keys between them.
--
--   psql -h localhost -U postgres -d rummy51 -f scripts/reset-players.sql
--
-- Sessions outlive their account: a cookie issued before this runs still
-- decrypts, finds no player, and is rejected. Signing out clears it.
TRUNCATE TABLE
    player_tokens,
    player_logins,
    player_claims,
    player_roles,
    players;

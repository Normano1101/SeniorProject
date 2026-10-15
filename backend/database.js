const Database = require("better-sqlite3");

const db = new Database("game.db");

db.pragma("foreign_keys = ON");

db.exec(`
    CREATE TABLE IF NOT EXISTS GameSaves (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        player_name TEXT,
        current_wave INTEGER DEFAULT 1,
        player_health INTEGER DEFAULT 100,
        currency INTEGER DEFAULT 0,
        difficulty REAL DEFAULT 1.0
    );

    CREATE TABLE IF NOT EXISTS SaveTowers (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        save_id INTEGER NOT NULL,
        tower_type TEXT NOT NULL,
        position_x REAL NOT NULL,
        position_y REAL NOT NULL,
        level INTEGER DEFAULT 1,

        FOREIGN KEY (save_id)
        REFERENCES GameSaves(id)
        ON DELETE CASCADE
    );
`);
module.exports = db;
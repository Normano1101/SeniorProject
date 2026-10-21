const express = require("express");
const db = require("./database");

const app = express();
const PORT = 3000;
const allowedEnemyTypes = new Set(["Basic", "Runner", "Heavy"]);
const allowedTowerTypes = new Set(["BasicTower", "RapidTower", "CannonTower"]);

// Allow API to receive JSON
app.use(express.json());

// Test API
app.get("/", (req, res) => {
    res.json({
        message: "Adaptive Tower Defense API running"
    });
});

// SAVE GAME
app.post("/api/save", (req, res) => {
    console.log("Received save request:", req.body);

    const {
        save_id = 0,
        player_name,
        current_wave,
        player_health,
        currency,
        difficulty,
        enemies_spawned,
        enemies,
        towers
    } = req.body;

    if (
        !Number.isSafeInteger(save_id) ||
        save_id < 0 ||
        typeof player_name !== "string" ||
        !Number.isSafeInteger(current_wave) ||
        current_wave < 1 ||
        !Number.isSafeInteger(player_health) ||
        player_health < 0 ||
        !Number.isSafeInteger(currency) ||
        currency < 0 ||
        typeof difficulty !== "number" ||
        !Number.isFinite(difficulty) ||
        difficulty <= 0 ||
        !Number.isSafeInteger(enemies_spawned) ||
        enemies_spawned < 0 ||
        !Array.isArray(enemies) ||
        enemies.length > enemies_spawned ||
        !enemies.every(enemy =>
            enemy !== null &&
            typeof enemy === "object" &&
            Number.isFinite(enemy.x) &&
            Number.isFinite(enemy.y) &&
            Number.isSafeInteger(enemy.health) &&
            enemy.health > 0 &&
            Number.isSafeInteger(enemy.waypoint) &&
            enemy.waypoint >= 0 &&
            (enemy.enemy_type === undefined ||
                allowedEnemyTypes.has(enemy.enemy_type))
            ) ||
            !Array.isArray(towers) ||
            !towers.every(tower =>
                tower !== null &&
                typeof tower === "object" &&
                allowedTowerTypes.has(tower.tower_type) &&
                Number.isFinite(tower.position_x) &&
                Number.isFinite(tower.position_y) &&
                Number.isSafeInteger(tower.level) &&
                tower.level >= 1
            )
    ) {
        return res.status(400).json({
            error: "Invalid save data"
        });
    }

    const enemiesJson = JSON.stringify(enemies);

    const saveTransaction = db.transaction(() => {
    let savedId = save_id;
    let updated = false;

    if (save_id > 0) {
        const result = db.prepare(`
            UPDATE GameSaves
            SET player_name = ?,
                current_wave = ?,
                player_health = ?,
                currency = ?,
                difficulty = ?,
                enemies_spawned = ?,
                enemies = ?
            WHERE id = ?
        `).run(
            player_name,
            current_wave,
            player_health,
            currency,
            difficulty,
            enemies_spawned,
            enemiesJson,
            save_id
        );

        if (result.changes === 0) {
            return null;
        }

        updated = true;
    } else {
        const result = db.prepare(`
            INSERT INTO GameSaves (
                player_name,
                current_wave,
                player_health,
                currency,
                difficulty,
                enemies_spawned,
                enemies
            )
            VALUES (?, ?, ?, ?, ?, ?, ?)
        `).run(
            player_name,
            current_wave,
            player_health,
            currency,
            difficulty,
            enemies_spawned,
            enemiesJson
        );

        savedId = Number(result.lastInsertRowid);
    }

    // Remove previously saved towers for this save ID
    db.prepare(`
        DELETE FROM SaveTowers
        WHERE save_id = ?
    `).run(savedId);

    // Insert the towers currently placed in Unity
    const insertTower = db.prepare(`
        INSERT INTO SaveTowers (
            save_id,
            tower_type,
            position_x,
            position_y,
            level
        )
        VALUES (?, ?, ?, ?, ?)
    `);

    for (const tower of towers) {
        insertTower.run(
            savedId,
            tower.tower_type,
            tower.position_x,
            tower.position_y,
            tower.level
        );
    }

    return {
        success: true,
        save_id: String(savedId),
        updated
    };
});

const result = saveTransaction();

if (result === null) {
    return res.status(404).json({
        error: "Save not found"
    });
}

return res.json(result);
});


// LOAD GAME
app.get("/api/load/:id", (req, res) => {

    const id = Number(req.params.id);

    if (!Number.isSafeInteger(id) || id < 1) {
        return res.status(400).json({
            error: "Invalid save ID"
        });
    }

    const save = db.prepare(`
        SELECT *
        FROM GameSaves
        WHERE id = ?
    `).get(id);

    if (!save) {
        return res.status(404).json({
            error: "Save not found"
        });
    }

    const towers = db.prepare(`
        SELECT
            tower_type,
            position_x,
            position_y,
            level
        FROM SaveTowers
        WHERE save_id = ?
    `).all(id);

    res.json({
        ...save,
        enemies_spawned: save.enemies_spawned ?? 0,
        enemies: JSON.parse(save.enemies || "[]"),
        towers
    });
});

// Start server
app.listen(PORT, "127.0.0.1", () => {
    console.log(`API running at http://localhost:${PORT}`);
});
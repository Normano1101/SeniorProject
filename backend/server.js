const express = require("express");
const db = require("./database");

const app = express();
const PORT = 3000;

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

    const {
        player_name,
        current_wave,
        player_health,
        currency,
        difficulty
    } = req.body;

    if (
        typeof player_name !== "string" ||
        !Number.isSafeInteger(current_wave) ||
        current_wave < 1 ||
        !Number.isSafeInteger(player_health) ||
        player_health < 0 ||
        !Number.isSafeInteger(currency) ||
        currency < 0 ||
        typeof difficulty !== "number" ||
        !Number.isFinite(difficulty) ||
        difficulty <= 0
    ) {
        return res.status(400).json({
            error: "Invalid save data"
        });
    }

    const result = db.prepare(`
        INSERT INTO GameSaves (
            player_name,
            current_wave,
            player_health,
            currency,
            difficulty
        )
        VALUES (?, ?, ?, ?, ?)
    `).run(
        player_name,
        current_wave,
        player_health,
        currency,
        difficulty
    );

    res.json({
        success: true,
        save_id: result.lastInsertRowid.toString()
    });
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

    res.json(save);
});

// Start server
app.listen(PORT, "127.0.0.1", () => {
    console.log(`API running at http://localhost:${PORT}`);
});
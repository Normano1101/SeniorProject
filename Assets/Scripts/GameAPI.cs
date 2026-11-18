using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;

[Serializable]
public class GameSaveData
{
    public int save_id;
    public string player_name;
    public int current_wave;
    public int player_health;
    public int currency;
    public float difficulty;

    // Number of enemies already spawned in this wave
    public int enemies_spawned;

    // Living enemies and their saved information
    public EnemySaveData[] enemies;
    public TowerSaveData[] towers;
}


[Serializable]
public class SaveResponse
{
    public bool success;
    public string save_id;
}
public class GameAPI : MonoBehaviour
{
    // Use 127.0.0.1, not "localhost": on macOS "localhost" resolves to IPv6
    // (::1) first, but server.js only listens on IPv4 127.0.0.1. Any other
    // app on port 3000 (e.g. a class project's index.js) can then answer
    // instead and return "Cannot POST /api/save" (404).
    private const string API_URL = "http://127.0.0.1:3000";

    public WaveManager waveManager;
    public PlayerHealth playerHealth;
    public TowerManager towerManager;

    // Remembers the most recent save during this game session.
    private int currentSaveId = -1;

    // ---- Debug: what the last successful save in THIS session contained,
    // so a later load can be checked against it. ----
    private bool hasSessionSave;
    private int pendingSaveEnemyCount;
    private int lastSavedWave;
    private float lastSavedDifficulty;
    private int lastSavedEnemyCount;

    void Start()
    {
        Debug.Log("GAME API STARTED");
    }

    void Update()
    {
        // Debug hotkey: press F9 to print the current difficulty state.
        // Use it before saving and after loading to compare.
        if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame)
        {
            LogDifficultyState("[F9]");
        }
    }

    private void LogDifficultyState(string tag)
    {
        if (waveManager == null || waveManager.enemySpawner == null)
        {
            Debug.LogWarning(tag + " WaveManager or EnemySpawner missing.");
            return;
        }

        Debug.Log(
            tag + " Difficulty state\n" +
            "Wave: " + waveManager.currentWave + "\n" +
            "Current wave difficulty (saved): " + waveManager.CurrentWaveDifficulty.ToString("F2") + "\n" +
            "Next wave difficulty (not saved): " + waveManager.NextDifficulty.ToString("F2") + "\n" +
            "Wave enemy count: " + waveManager.enemySpawner.enemiesToSpawn +
            " (spawned " + waveManager.enemySpawner.enemiesSpawned +
            ", alive " + waveManager.enemiesRemaining + ")\n" +
            "In intermission: " + waveManager.IsIntermission
        );
    }

    // Compares the loaded game against the save data and prints PASS/FAIL.
    private void VerifyDifficultyRestore(GameSaveData data)
    {
        const float tolerance = 0.001f;
        bool allPassed = true;

        float savedDifficulty = data.difficulty > 0f ? data.difficulty : 1f;
        float restoredDifficulty = waveManager.CurrentWaveDifficulty;

        // 1. The wave's difficulty equals the saved value.
        bool difficultyOk = Mathf.Abs(restoredDifficulty - savedDifficulty) < tolerance;
        allPassed &= difficultyOk;
        LogCheck(
            difficultyOk,
            "Difficulty restored: saved " + savedDifficulty.ToString("F3") +
            ", restored " + restoredDifficulty.ToString("F3") +
            (difficultyOk ? "" : " (difficulty was not restored)")
        );

        // 2. Loading must not change difficulty by itself. The next change
        //    should only happen after the restored wave is completed.
        //    (For an intermission save the wave is already complete, so the
        //    next wave uses the saved difficulty.)
        bool nextOk = Mathf.Abs(waveManager.NextDifficulty - restoredDifficulty) < tolerance;
        allPassed &= nextOk;
        LogCheck(
            nextOk,
            "No difficulty change on load: next wave difficulty " +
            waveManager.NextDifficulty.ToString("F3")
        );

        // 3. The wave's enemy count is the one the saved difficulty produces.
        int expectedCount = waveManager.enemySpawner.CalculateEnemyCount(
            waveManager.currentWave,
            savedDifficulty
        );
        int restoredCount = waveManager.enemySpawner.enemiesToSpawn;
        bool countOk = restoredCount == expectedCount;
        allPassed &= countOk;
        LogCheck(
            countOk,
            "Enemy count uses saved difficulty: expected " + expectedCount +
            ", restored " + restoredCount
        );

        // 4. If this session saved the same wave, compare with the exact
        //    count from save time (catches any formula drift).
        if (hasSessionSave && lastSavedWave == data.current_wave &&
            Mathf.Abs(lastSavedDifficulty - savedDifficulty) < tolerance)
        {
            bool sameAsSave = restoredCount == lastSavedEnemyCount;
            allPassed &= sameAsSave;
            LogCheck(
                sameAsSave,
                "Enemy count matches count at save time: " + lastSavedEnemyCount
            );
        }
        else
        {
            Debug.Log("[DifficultyCheck] SKIP Save-time count check (no matching save this session)");
        }

        // 5. Spawned count was not pushed past the wave size.
        bool spawnedOk = waveManager.enemySpawner.enemiesSpawned <= restoredCount;
        allPassed &= spawnedOk;
        LogCheck(
            spawnedOk,
            "Spawned count within wave size: " + waveManager.enemySpawner.enemiesSpawned +
            " / " + restoredCount
        );

        if (allPassed)
        {
            Debug.Log("[DifficultyCheck] ALL PASSED - difficulty restored correctly");
        }
        else
        {
            Debug.LogError("[DifficultyCheck] FAILED - see the FAIL lines above");
        }
    }

    private static void LogCheck(bool passed, string message)
    {
        if (passed)
        {
            Debug.Log("[DifficultyCheck] PASS " + message);
        }
        else
        {
            Debug.LogError("[DifficultyCheck] FAIL " + message);
        }
    }

    // Called by the Save button in the pause menu.
    public void SaveCurrentGame()
    {
        if (waveManager == null || playerHealth == null || towerManager == null)
        {
            Debug.LogError("GameAPI: Missing WaveManager, PlayerHealth, or TowerManager reference.");
            return;
        }

        if (CurrencyManager.Instance == null)
        {
            Debug.LogError("GameAPI: CurrencyManager reference is missing.");
            return;
        }

        GameSaveData data = new GameSaveData
        {
            save_id = PlayerPrefs.GetInt("LastSaveId", 0),
            player_name = "UnityPlayer",
            current_wave = waveManager.currentWave,
            player_health = playerHealth.currentHealth,
            currency = CurrencyManager.Instance.CurrentCurrency,
            // Difficulty of the wave being saved, so loading rebuilds it
            // with the same enemy count.
            difficulty = waveManager.CurrentWaveDifficulty,

            enemies_spawned = waveManager.enemySpawner.enemiesSpawned,

            enemies = waveManager.enemySpawner
                .GetEnemiesSaveData()
                .ToArray(),

            towers = towerManager.GetTowersSaveData()
        };
        Debug.Log("Sending Save ID: " + data.save_id);

        pendingSaveEnemyCount = waveManager.enemySpawner.enemiesToSpawn;
        LogDifficultyState("[Save]");

        StartCoroutine(SaveGame(data));
    }

    public IEnumerator SaveGame(GameSaveData data)
    {
        string json = JsonUtility.ToJson(data);
        string url = API_URL + "/api/save";

        Debug.Log("[Save] Starting save request");
        Debug.Log("[Save] API URL: " + url);
        Debug.Log("[Save] Wave: " + data.current_wave);
        Debug.Log("[Save] Difficulty: " + data.difficulty.ToString("F2"));
        Debug.Log(
            "[Save] Enemies spawned: " + data.enemies_spawned +
            ", alive: " + (data.enemies?.Length ?? 0) +
            ", towers: " + (data.towers?.Length ?? 0)
        );

        using (UnityWebRequest request =
            new UnityWebRequest(url, "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);

            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            Debug.Log("[Save] HTTP Status: " + request.responseCode);

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[Save] Save successful: " + request.downloadHandler.text);

                // Remember what was saved so the next load can be checked.
                hasSessionSave = true;
                lastSavedWave = data.current_wave;
                lastSavedDifficulty = data.difficulty;
                lastSavedEnemyCount = pendingSaveEnemyCount;
                Debug.Log(
                    "[Save] Recorded for load check: wave " + lastSavedWave +
                    ", difficulty " + lastSavedDifficulty.ToString("F2") +
                    ", enemy count " + lastSavedEnemyCount
                );

                SaveResponse response =
                    JsonUtility.FromJson<SaveResponse>(
                        request.downloadHandler.text
                    );
                if (response != null &&
                    int.TryParse(response.save_id, out int savedId) &&
                    savedId > 0)
                {
                    currentSaveId = savedId;
                    PlayerPrefs.SetInt("LastSaveId", savedId);
                    PlayerPrefs.Save();

                    Debug.Log("Remembered Save ID: " + savedId);
                }
            }
            else
            {
                 Debug.LogError(
                "[Save] Save failed: " + request.error +
                "\nServer response: " + request.downloadHandler.text +
                "\nIf this says 'Cannot POST /api/save', a different server " +
                "is on port 3000. Run 'node server.js' in backend/.");
            }
        }
    }

    // Called by the Load button in the pause menu.
    public void LoadCurrentGame()
    {
        int saveId = PlayerPrefs.GetInt("LastSaveId", -1);

        if (saveId == -1)
        {
            Debug.LogWarning("No saved game found. Save your game first.");
            return;
        }

        StartCoroutine(LoadGame(saveId));
    }

    public IEnumerator LoadGame(int saveId)
    {
        string url = API_URL + "/api/load/" + saveId;
        Debug.Log("[Load] Loading saved game " + saveId + " from " + url);

        using (UnityWebRequest request =
            UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            Debug.Log("[Load] HTTP Status: " + request.responseCode);

            if (request.result == UnityWebRequest.Result.Success)
            {
                GameSaveData data =
                    JsonUtility.FromJson<GameSaveData>(
                        request.downloadHandler.text
                    );

                if (data == null)
                {
                    Debug.LogError("[Load] Save response could not be parsed.");
                    yield break;
                }

                Debug.Log("[Load] Saved wave: " + data.current_wave);
                Debug.Log("[Load] Saved difficulty: " + data.difficulty.ToString("F2"));

                if (waveManager == null || playerHealth == null || towerManager == null)
                {
                    Debug.LogError("GameAPI: Missing manager references.");
                    yield break;
                }

                if (CurrencyManager.Instance == null)
                {
                    Debug.LogError("GameAPI: CurrencyManager reference is missing.");
                    yield break;
                }

                // Restore difficulty BEFORE the wave is prepared.
                // Older saves without a valid value fall back to 1.0.
                waveManager.SetDifficulty(
                    data.difficulty > 0f ? data.difficulty : 1f
                );
                Debug.Log(
                    "[Load] Difficulty restored before wave preparation: " +
                    waveManager.CurrentWaveDifficulty.ToString("F2")
                );

                waveManager.RestoreWave(
                data.current_wave,
                data.enemies_spawned,
                new System.Collections.Generic.List<EnemySaveData>(
                    data.enemies ?? Array.Empty<EnemySaveData>()
                )
            );

            playerHealth.LoadHealth(data.player_health);
            CurrencyManager.Instance.SetCurrency(data.currency);
           
            Debug.Log("Towers received from database: " +
                (data.towers == null ? "NULL" : data.towers.Length.ToString()));
            
            towerManager.RestoreTowers(
                data.towers ?? Array.Empty<TowerSaveData>()
            );
            Debug.Log("Loaded wave: " + data.current_wave);
            Debug.Log("Loaded health: " + data.player_health);
            Debug.Log(
                "[Load] Restored enemy count: " + waveManager.enemySpawner.enemiesToSpawn +
                " for wave (" + waveManager.enemySpawner.enemiesSpawned + " already spawned, " +
                waveManager.enemiesRemaining + " alive)"
            );
            Debug.Log(
                "[Load] Health: " + playerHealth.currentHealth +
                ", currency: $" + CurrencyManager.Instance.CurrentCurrency
            );
            VerifyDifficultyRestore(data);
            LogDifficultyState("[Load]");
            Debug.Log("[Load] Load successful");
            }
            else
            {
                Debug.LogError(
                    "[Load] Load failed: " + request.error +
                    "\nServer response: " + request.downloadHandler.text
                );
            }
        }
    }
}
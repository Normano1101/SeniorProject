using System;
using System.Collections;
using System.Text;
using UnityEngine;
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
    private const string API_URL = "http://localhost:3000";

    public WaveManager waveManager;
    public PlayerHealth playerHealth;
    public TowerManager towerManager;

    // Remembers the most recent save during this game session.
    private int currentSaveId = -1;

    void Start()
    {
        Debug.Log("GAME API STARTED");
    }

    // Called by the Save button in the pause menu.
    public void SaveCurrentGame()
    {
        if (waveManager == null || playerHealth == null || towerManager == null)
        {
            Debug.LogError("GameAPI: Missing WaveManager, PlayerHealth, or TowerManager reference.");
            return;
        }

        GameSaveData data = new GameSaveData
        {
            save_id = PlayerPrefs.GetInt("LastSaveId", 0),
            player_name = "UnityPlayer",
            current_wave = waveManager.currentWave,
            player_health = playerHealth.currentHealth,
            currency = 0,
            difficulty = 1.0f,

            enemies_spawned = waveManager.enemySpawner.enemiesSpawned,

            enemies = waveManager.enemySpawner
                .GetEnemiesSaveData()
                .ToArray(),

            towers = towerManager.GetTowersSaveData()
        };
        Debug.Log("Sending Save ID: " + data.save_id);

        StartCoroutine(SaveGame(data));
    }

    public IEnumerator SaveGame(GameSaveData data)
    {
        string json = JsonUtility.ToJson(data);

        using (UnityWebRequest request =
            new UnityWebRequest(API_URL + "/api/save", "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);

            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Game saved: " + request.downloadHandler.text);

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
                "Save failed: " + request.error +
                "\nServer response: " + request.downloadHandler.text);
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
        using (UnityWebRequest request =
            UnityWebRequest.Get(API_URL + "/api/load/" + saveId))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                GameSaveData data =
                    JsonUtility.FromJson<GameSaveData>(
                        request.downloadHandler.text
                    );

                if (waveManager == null || playerHealth == null || towerManager == null)
                {
                    Debug.LogError("GameAPI: Missing manager references.");
                    yield break;
                }

                waveManager.RestoreWave(
                data.current_wave,
                data.enemies_spawned,
                new System.Collections.Generic.List<EnemySaveData>(
                    data.enemies ?? Array.Empty<EnemySaveData>()
                )
            );

            playerHealth.LoadHealth(data.player_health);

            Debug.Log("Towers received from database: " +
                (data.towers == null ? "NULL" : data.towers.Length.ToString()));

            towerManager.RestoreTowers(
                data.towers ?? Array.Empty<TowerSaveData>()
            );
            Debug.Log("Loaded wave: " + data.current_wave);
            Debug.Log("Loaded health: " + data.player_health);
            }
            else
            {
                Debug.LogError("Load failed: " + request.error);
            }
        }
    }
}
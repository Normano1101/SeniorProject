using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.InputSystem;

[Serializable]
public class GameSaveData
{

    public string player_name;
    public int current_wave;
    public int player_health;
    public int currency;
    public float difficulty;
}

public class GameAPI : MonoBehaviour
{
    private const string API_URL = "http://localhost:3000";
    public WaveManager waveManager;
    public PlayerHealth playerHealth;
    public void SaveCurrentGame()
    {
        GameSaveData data = new GameSaveData
        {
            player_name = "UnityPlayer",
            current_wave = waveManager.currentWave,
            player_health = playerHealth.currentHealth,
            currency = 0,
            difficulty = 1.0f
        };

        StartCoroutine(SaveGame(data));
    }
//game api test
    void Start()
    {
        Debug.Log("GAME API STARTED");
    }
    
    void Update()
    {
        if (Keyboard.current != null &&
        Keyboard.current.f5Key.wasPressedThisFrame)
        {
            SaveCurrentGame();
        }
        if (Keyboard.current.f9Key.wasPressedThisFrame)
        {
            StartCoroutine(LoadGame(5));
        }
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
            }
            else
            {
                Debug.LogError("Save failed: " + request.error);
            }
        }
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

                Debug.Log("Loaded wave: " + data.current_wave);
                Debug.Log("Loaded health: " + data.player_health);
                waveManager.currentWave = data.current_wave;
                playerHealth.LoadHealth(data.player_health);
            }
            else
            {
                Debug.LogError("Load failed: " + request.error);
            }
        }
    }
}
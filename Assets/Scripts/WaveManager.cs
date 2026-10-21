using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public int currentWave = 1;
    public int enemiesRemaining = 0;
    public EnemySpawner enemySpawner;

    [SerializeField] private float intermissionDuration = 10f;
    [SerializeField] private int earlyStartBonus = 20;

    private Coroutine waveCoroutine;
    private bool isSpawning;
    private bool isIntermission;
    private float intermissionRemaining;

    public float IntermissionRemaining => intermissionRemaining;
    public bool IsSpawning => isSpawning;
    public bool IsIntermission => isIntermission;

    void Start()
    {
        if (enemySpawner == null)
        {
            Debug.LogError("WaveManager requires an EnemySpawner.");
            enabled = false;
            return;
        }

        enemiesRemaining = 0;
        enemySpawner.waveManager = this;
        StartWave();
    }

    void Update()
    {
        if (!isIntermission)
        {
            return;
        }

        intermissionRemaining -= Time.deltaTime;
        if (intermissionRemaining <= 0f)
        {
            isIntermission = false;
            currentWave++;
            StartWave();
        }
    }

    public void StartWave()
    {
        if (enemySpawner == null || isSpawning || enemiesRemaining > 0)
        {
            return;
        }

        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
            waveCoroutine = null;
        }

        enemySpawner.ClearEnemies();
        enemiesRemaining = 0;
        enemySpawner.enemiesSpawned = 0;
        enemySpawner.PrepareWave(currentWave);
        isIntermission = false;
        isSpawning = true;

        if (!enemySpawner.HasValidPath)
        {
            isSpawning = false;
            Debug.LogError("Wave cannot start because the enemy path is incomplete.");
            return;
        }

        Debug.Log("Starting wave: " + currentWave);
        waveCoroutine = StartCoroutine(SpawnCurrentWave());
    }

    private IEnumerator SpawnCurrentWave()
    {
        isSpawning = true;
        yield return enemySpawner.SpawnWave();
        isSpawning = false;
        waveCoroutine = null;
        CheckWaveComplete();
    }

    private IEnumerator ResumeSavedWave()
    {
        isSpawning = true;
        yield return enemySpawner.SpawnRemainingEnemies();
        isSpawning = false;
        waveCoroutine = null;
        CheckWaveComplete();
    }

    public void LoadWave(int savedWave)
    {
        currentWave = Mathf.Max(1, savedWave);
        Debug.Log("Restarting saved wave: " + currentWave);
        StartWave();
    }

    public void StartNextWaveEarly()
    {
        if (!isIntermission)
        {
            return;
        }

        bool earnsBonus = intermissionRemaining > 1f;
        isIntermission = false;
        currentWave++;

        if (earnsBonus && CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddCurrency(earlyStartBonus);
        }

        StartWave();
    }

    public void RestoreWave(
        int savedWave,
        int savedSpawnedCount,
        List<EnemySaveData> savedEnemies)
    {
        if (enemySpawner == null)
        {
            Debug.LogError("WaveManager cannot restore without an EnemySpawner.");
            return;
        }

        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
            waveCoroutine = null;
        }

        currentWave = Mathf.Max(1, savedWave);
        isIntermission = false;
        enemySpawner.PrepareWave(currentWave);
        enemySpawner.enemiesSpawned = Mathf.Clamp(
            savedSpawnedCount,
            0,
            enemySpawner.enemiesToSpawn
        );
        enemySpawner.RestoreEnemies(savedEnemies, currentWave);

        enemiesRemaining = 0;
        if (savedEnemies != null)
        {
            foreach (EnemySaveData enemy in savedEnemies)
            {
                if (enemy != null)
                {
                    enemiesRemaining++;
                }
            }
        }

        if (enemySpawner.enemiesSpawned < enemySpawner.enemiesToSpawn)
        {
            isSpawning = true;
            waveCoroutine = StartCoroutine(ResumeSavedWave());
        }
        else
        {
            isSpawning = false;
            CheckWaveComplete();
        }

        Debug.Log(
            "Restored wave " + currentWave +
            " with " + enemiesRemaining + " enemies. " +
            "Already spawned: " + enemySpawner.enemiesSpawned
        );
    }

    public void EnemySpawned()
    {
        enemiesRemaining++;
    }

    public void EnemyRemoved()
    {
        enemiesRemaining = Mathf.Max(0, enemiesRemaining - 1);
        Debug.Log("Enemy removed. Remaining: " + enemiesRemaining);
        CheckWaveComplete();
    }

    private void CheckWaveComplete()
    {
        if (!isSpawning && enemiesRemaining == 0 && !isIntermission)
        {
            Debug.Log("Wave " + currentWave + " complete!");
            isIntermission = true;
            intermissionRemaining = intermissionDuration;
        }
    }
}

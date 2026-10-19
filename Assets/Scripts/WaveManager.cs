using System.Collections;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public int currentWave = 1;
    public int enemiesRemaining = 0;
    public EnemySpawner enemySpawner;

    private Coroutine waveCoroutine;
    private bool isSpawning = false;

    void Start()
    {
        enemySpawner.waveManager = this;
        StartWave();
    }

    public void StartWave()
    {
        // Stop the previous spawning process
        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
            waveCoroutine = null;
        }

        // Remove existing enemies
        enemySpawner.ClearEnemies();

        // Reset counters
        enemiesRemaining = 0;
        enemySpawner.enemiesSpawned = 0;
        isSpawning = true;

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

    public void RestoreWave(
        int savedWave,
        int savedSpawnedCount,
        System.Collections.Generic.List<EnemySaveData> savedEnemies)
    {
        // Stop the old spawning coroutine.
        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
            waveCoroutine = null;
        }

        // Restore wave number.
        currentWave = Mathf.Max(1, savedWave);

        // Restore spawned count.
        enemySpawner.enemiesSpawned = Mathf.Clamp(
            savedSpawnedCount,
            0,
            enemySpawner.enemiesToSpawn
        );

        // Restore enemies at saved positions.
        enemySpawner.RestoreEnemies(savedEnemies);

        // Restore number of living enemies.
        enemiesRemaining = savedEnemies != null
            ? savedEnemies.Count
            : 0;

        // Resume spawning only if some enemies haven't appeared yet.
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
        if (!isSpawning && enemiesRemaining == 0)
        {
            Debug.Log("Wave " + currentWave + " complete!");

            currentWave++;
        }
    }
}
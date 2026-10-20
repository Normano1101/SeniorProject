using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public PathManager pathManager;
    public WaveManager waveManager;

    public float spawnInterval = 2f;
    public int enemiesToSpawn = 10;
    public int enemiesSpawned = 0;

    private List<Vector3> path;

    // Keep track of enemies created by this spawner.
    private readonly List<GameObject> spawnedEnemies = new List<GameObject>();

    void Awake()
    {
        path = pathManager.GetPath();
    }

    public IEnumerator SpawnWave()
    {
        enemiesSpawned = 0;

        yield return SpawnRemainingEnemies();
    }

    public IEnumerator SpawnRemainingEnemies()
    {
        while (enemiesSpawned < enemiesToSpawn)
        {
            SpawnEnemy();

            enemiesSpawned++;

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnEnemy()
    {
        if (path == null || path.Count == 0)
        {
            Debug.LogError("No path was found!");
            return;
        }

        GameObject newEnemy = Instantiate(
            enemyPrefab,
            path[0],
            Quaternion.identity
        );

        spawnedEnemies.Add(newEnemy);

        waveManager.EnemySpawned();

        Enemy enemy = newEnemy.GetComponent<Enemy>();

        if (enemy != null)
        {
            enemy.waveManager = waveManager;
            enemy.SetPath(path);
        }
    }

    public List<EnemySaveData> GetEnemiesSaveData()
    {
        List<EnemySaveData> enemies = new List<EnemySaveData>();

        foreach (GameObject enemyObject in spawnedEnemies)
        {
            if (enemyObject == null || !enemyObject.activeInHierarchy)
                continue;

            Enemy enemy = enemyObject.GetComponent<Enemy>();

            if (enemy != null)
            {
                enemies.Add(enemy.GetSaveData());
            }
        }

        return enemies;
    }
    public void RestoreEnemies(List<EnemySaveData> savedEnemies)
    {
        // Remove enemies currently on the map
        ClearEnemies();

        if (savedEnemies == null)
            return;

        foreach (EnemySaveData data in savedEnemies)
        {
            GameObject newEnemy = Instantiate(
                enemyPrefab,
                path[0],
                Quaternion.identity
            );

            spawnedEnemies.Add(newEnemy);

            Enemy enemy = newEnemy.GetComponent<Enemy>();

            if (enemy != null)
            {
                enemy.waveManager = waveManager;

                // Set the path before restoring the saved position
                enemy.SetPath(path);
                enemy.LoadSaveData(data);
            }
        }

        Debug.Log("Restored " + savedEnemies.Count + " enemies.");
    }

    // Remove enemies from the previous wave before loading.
    public void ClearEnemies()
    {
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                // Disable immediately so it cannot move or trigger callbacks.
                enemy.SetActive(false);
                Destroy(enemy);
            }
        }

        spawnedEnemies.Clear();
    }
}
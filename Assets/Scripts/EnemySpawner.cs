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
    private int baseEnemiesToSpawn;

    // Keep track of enemies created by this spawner.
    private readonly List<GameObject> spawnedEnemies = new List<GameObject>();

    void Awake()
    {
        baseEnemiesToSpawn = Mathf.Max(1, enemiesToSpawn);

        if (pathManager == null)
        {
            Debug.LogError("EnemySpawner requires a PathManager.");
            path = new List<Vector3>();
            return;
        }

        path = pathManager.GetPath();
    }

    public bool HasValidPath => path != null && path.Count > 1;

    public void PrepareWave(int wave)
    {
        enemiesToSpawn = baseEnemiesToSpawn + Mathf.Max(0, wave - 1) * 2;
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
            if (!SpawnEnemy(ChooseEnemyKind(enemiesSpawned, waveManager.currentWave)))
            {
                yield break;
            }

            enemiesSpawned++;

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private EnemyKind ChooseEnemyKind(int spawnIndex, int wave)
    {
        if (wave >= 3 && spawnIndex % 5 == 4)
        {
            return EnemyKind.Heavy;
        }

        if (wave >= 2 && spawnIndex % 3 == 2)
        {
            return EnemyKind.Runner;
        }

        return EnemyKind.Basic;
    }

    private bool SpawnEnemy(EnemyKind kind)
    {
        if (!HasValidPath || enemyPrefab == null || waveManager == null)
        {
            Debug.LogError("EnemySpawner is missing its path, enemy prefab, or wave manager.");
            return false;
        }

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
            enemy.Configure(kind, waveManager.currentWave);
            enemy.SetPath(path);
            waveManager.EnemySpawned();
        }
        else
        {
            Debug.LogError("The enemy prefab has no Enemy component.");
            Destroy(newEnemy);
            spawnedEnemies.Remove(newEnemy);
            return false;
        }

        return true;
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
    public void RestoreEnemies(List<EnemySaveData> savedEnemies, int wave)
    {
        // Remove enemies currently on the map
        ClearEnemies();

        if (savedEnemies == null)
            return;

        foreach (EnemySaveData data in savedEnemies)
        {
            if (data == null)
            {
                Debug.LogWarning("Skipping an empty enemy save entry.");
                continue;
            }

            if (!HasValidPath || enemyPrefab == null)
            {
                Debug.LogError("Cannot restore enemies without a valid path and enemy prefab.");
                return;
            }

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
                enemy.LoadSaveData(data, wave);
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
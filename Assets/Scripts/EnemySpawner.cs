using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public PathManager pathManager;

    public float spawnInterval = 2f;
    public int enemiesToSpawn = 10;

    private List<Vector3> path;

    void Start()
    {
        // Get the path once when the level starts
        path = pathManager.GetPath();

        StartCoroutine(SpawnWave());
    }

    IEnumerator SpawnWave()
    {
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            SpawnEnemy();

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

        Enemy enemy = newEnemy.GetComponent<Enemy>();

        if (enemy != null)
        {
            enemy.SetPath(path);
        }
    }
}

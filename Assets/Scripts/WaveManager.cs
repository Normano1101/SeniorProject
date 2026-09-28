using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public int currentWave = 1;
    public int enemiesRemaining = 0;
    public EnemySpawner enemySpawner;


    void Start()
    {
        enemySpawner.waveManager = this;
        StartCoroutine(enemySpawner.SpawnWave());
    }

    public void EnemySpawned()
    {
        enemiesRemaining++;
        Debug.Log("Enemy spawned. Remaining: " + 
        enemiesRemaining);
    }

    public void EnemyRemoved()
    {
        enemiesRemaining--;
        Debug.Log("Enemy removed. Remaining: " + 
        enemiesRemaining);
        if (enemiesRemaining == 0)
        {
            Debug.Log("Wave " + currentWave + " complete!");
            currentWave++;
        }
    }

    void Update()
    {
        
    }
}

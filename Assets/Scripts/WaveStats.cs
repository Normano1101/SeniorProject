using UnityEngine;

// Performance numbers for ONE wave. WaveManager resets this when a wave
// starts (or is restored from a save) and DifficultyDirector reads it when
// the wave is complete.
[System.Serializable]
public class WaveStats
{
    public int waveNumber;
    public int enemiesKilled;
    public int enemiesLeaked;

    // Health the base actually lost (damage past 0 HP is not counted).
    public float healthLost;

    // Scaled game time (pausing does not count) when tracking started.
    public float startTime;

    // Seconds from wave start to wave complete. Set by Finish().
    public float clearTime;

    public int TotalEnemies =>
        enemiesKilled + enemiesLeaked;

    public float KillRate =>
        TotalEnemies > 0
            ? (float)enemiesKilled / TotalEnemies
            : 0f;

    public void Reset(int wave)
    {
        waveNumber = wave;
        enemiesKilled = 0;
        enemiesLeaked = 0;
        healthLost = 0f;
        startTime = Time.time;
        clearTime = 0f;
    }

    public void RecordEnemyRemoved(bool wasKilled)
    {
        if (wasKilled)
        {
            enemiesKilled++;
        }
        else
        {
            enemiesLeaked++;
        }
    }

    public void RecordHealthLost(int amount)
    {
        healthLost += Mathf.Max(0, amount);
    }

    public void Finish()
    {
        clearTime = Time.time - startTime;
    }
}

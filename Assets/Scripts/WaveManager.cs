using System.Collections;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public int currentWave = 1;
    public int enemiesRemaining = 0;
    public EnemySpawner enemySpawner;

    [SerializeField] private float intermissionDuration = 10f;
    [SerializeField] private int earlyStartBonus = 20;

    [Header("Adaptive Difficulty")]
    // Optional: found automatically if left empty.
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private DifficultyDirector difficultyDirector = new DifficultyDirector();

    private Coroutine waveCoroutine;
    private bool isSpawning = false;
    private bool isIntermission;
    private float intermissionRemaining;

    // Stats for the wave being played right now.
    private readonly WaveStats currentStats = new WaveStats();

    // Difficulty the current wave was prepared with. After a wave ends the
    // director already holds the NEXT wave's difficulty, so saves use this
    // value to keep currentWave and its enemy count consistent on load.
    private float currentWaveDifficulty = 1f;

    // Last wave that was evaluated, so a wave is never evaluated twice.
    private int lastEvaluatedWave = 0;

    public float IntermissionRemaining => intermissionRemaining;
    public bool IsSpawning => isSpawning;
    public bool IsIntermission => isIntermission;
    public float CurrentWaveDifficulty => currentWaveDifficulty;
    public float NextDifficulty => difficultyDirector.Difficulty;
    public WaveStats CurrentStats => currentStats;

    void Start()
    {
        if (enemySpawner == null)
        {
            Debug.LogError("WaveManager requires an EnemySpawner.");
            enabled = false;
            return;
        }

        if (playerHealth == null)
        {
            playerHealth = FindAnyObjectByType<PlayerHealth>();
        }

        if (playerHealth != null)
        {
            playerHealth.HealthLost += OnPlayerHealthLost;
        }
        else
        {
            Debug.LogWarning("WaveManager: No PlayerHealth found. Health lost will not affect difficulty.");
        }

        enemiesRemaining = 0;
        enemySpawner.waveManager = this;
        StartWave();
    }

    void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthLost -= OnPlayerHealthLost;
        }
    }

    private void OnPlayerHealthLost(int amount)
    {
        currentStats.RecordHealthLost(amount);
    }

    // Used when loading a save, before the saved wave is prepared.
    public void SetDifficulty(float difficulty)
    {
        difficultyDirector.SetDifficulty(difficulty);
        currentWaveDifficulty = difficultyDirector.Difficulty;

        if (difficultyDirector.IsOutsideLimits)
        {
            Debug.LogWarning(
                "[Load] Saved difficulty " + currentWaveDifficulty.ToString("F2") +
                " is outside the current limits (" +
                difficultyDirector.MinimumDifficulty.ToString("F2") + " - " +
                difficultyDirector.MaximumDifficulty.ToString("F2") +
                "). Kept as-is so this wave restores identically; " +
                "it moves inside the limits after this wave."
            );
        }
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

        // The director's difficulty was decided when the previous wave ended.
        currentWaveDifficulty = difficultyDirector.Difficulty;
        enemySpawner.PrepareWave(currentWave, currentWaveDifficulty);
        currentStats.Reset(currentWave);
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
        lastEvaluatedWave = currentWave - 1;

        Debug.Log("Restarting saved wave: " + currentWave);

        StartWave();
    }

    // True while the early-start button would still pay the bonus.
    public bool EarlyStartEligible => isIntermission && intermissionRemaining > 1f;

    public void StartNextWaveEarly()
    {
        Debug.Log("[EarlyStart] Button pressed");
        Debug.Log("[EarlyStart] Intermission: " + isIntermission);

        if (!isIntermission)
        {
            Debug.Log("[EarlyStart] Ignored: not in intermission (bonus already used or wave running)");
            return;
        }

        bool earnsBonus = EarlyStartEligible;
        Debug.Log("[EarlyStart] Time remaining: " + intermissionRemaining.ToString("F1") + "s");
        Debug.Log("[EarlyStart] Eligible: " + earnsBonus);

        // Leaving intermission first means a second click can't claim again.
        isIntermission = false;
        currentWave++;

        if (earnsBonus && CurrencyManager.Instance != null)
        {
            int before = CurrencyManager.Instance.CurrentCurrency;
            CurrencyManager.Instance.AddCurrency(earlyStartBonus);
            Debug.Log("[EarlyStart] Currency before: $" + before);
            Debug.Log("[EarlyStart] Bonus awarded: $" + earlyStartBonus);
            Debug.Log("[EarlyStart] Currency after: $" + CurrencyManager.Instance.CurrentCurrency);
        }
        else if (earnsBonus)
        {
            Debug.LogWarning("[EarlyStart] Eligible but no CurrencyManager in scene; bonus not paid");
        }

        StartWave();
        Debug.Log(
            "[EarlyStart] " + (isSpawning ? "Starting Wave " : "FAILED to start Wave ") +
            currentWave
        );
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
        isIntermission = false;

        // GameAPI calls SetDifficulty() before this, so the saved wave gets
        // the same enemy count it had when it was saved.
        currentWaveDifficulty = difficultyDirector.Difficulty;
        enemySpawner.PrepareWave(currentWave, currentWaveDifficulty);

        // Stats restart from the moment of loading. Kills and damage from
        // before the save are not counted again.
        currentStats.Reset(currentWave);
        lastEvaluatedWave = currentWave - 1;

        // Restore spawned count.
        enemySpawner.enemiesSpawned = Mathf.Clamp(
            savedSpawnedCount,
            0,
            enemySpawner.enemiesToSpawn
        );

        // Restore enemies at saved positions.
        enemySpawner.RestoreEnemies(savedEnemies, currentWave);

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

            // Saved during the intermission: this wave was already
            // evaluated before the save, so don't evaluate it again
            // (it would only see zero kills and zero leaks).
            if (enemiesRemaining == 0)
            {
                lastEvaluatedWave = currentWave;
                Debug.Log(
                    "[Load] Save was made during intermission: wave " + currentWave +
                    " already evaluated, difficulty will not change again"
                );
            }

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

    public void EnemyRemoved(bool wasKilled)
    {
        currentStats.RecordEnemyRemoved(wasKilled);

        enemiesRemaining = Mathf.Max(0, enemiesRemaining - 1);

        Debug.Log("Enemy removed. Remaining: " + enemiesRemaining);

        CheckWaveComplete();
    }

    private void CheckWaveComplete()
    {
        if (!isSpawning && enemiesRemaining == 0 && !isIntermission)
        {
            Debug.Log("Wave " + currentWave + " complete!");
            EvaluateDifficulty();
            isIntermission = true;
            intermissionRemaining = intermissionDuration;
        }
    }

    // AI step: runs once per completed wave and picks the next difficulty.
    private void EvaluateDifficulty()
    {
        if (lastEvaluatedWave >= currentWave)
        {
            return;
        }

        lastEvaluatedWave = currentWave;
        currentStats.Finish();

        int maxHealth = playerHealth != null ? playerHealth.maxHealth : 100;
        float previousDifficulty = currentWaveDifficulty;
        float newDifficulty = difficultyDirector.EvaluateWave(currentStats, maxHealth);
        int nextCount = enemySpawner.CalculateEnemyCount(currentWave + 1, newDifficulty);

        float change = difficultyDirector.LastChange;

        Debug.Log(
            "Wave " + currentWave + " Complete\n" +
            "Kills: " + currentStats.enemiesKilled + "\n" +
            "Leaks: " + currentStats.enemiesLeaked + "\n" +
            "Health Lost: " + currentStats.healthLost + "\n" +
            "Kill Rate: " + (currentStats.KillRate * 100f).ToString("F1") + "%\n" +
            "Clear Time: " + currentStats.clearTime.ToString("F1") + "s\n" +
            "\n" +
            "Raw Performance Score: " + difficultyDirector.LastScore.ToString("F2") + "\n" +
            "Smoothed Performance Score: " + difficultyDirector.SmoothedScore.ToString("F2") + "\n" +
            "\n" +
            "Previous Difficulty: " + previousDifficulty.ToString("F2") + "\n" +
            "Difficulty Change: " + (change >= 0f ? "+" : "") + change.ToString("F2") + "\n" +
            "New Difficulty: " + newDifficulty.ToString("F2") + "\n" +
            "Reason: " + difficultyDirector.LastReason + "\n" +
            "\n" +
            "Next Wave Enemy Count: " + nextCount
        );
    }
}

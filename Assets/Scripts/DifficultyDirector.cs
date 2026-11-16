using UnityEngine;

// Rule-based "AI director" for adaptive difficulty.
//
// After every completed wave it:
//   1. Scores the player's performance from -1 (struggling) to +1 (cruising),
//      using kill rate and base health lost.
//   2. Smooths that score with ASYMMETRIC rates: a better-than-average wave
//      is trusted quickly (positiveSmoothing), a worse-than-average wave
//      slowly (negativeSmoothing). Good play raises difficulty fast,
//      bad play lowers it gently.
//   3. Picks a step from the smoothed score:
//        excellent (>= excellentThreshold)  -> +excellentIncrease (~+0.03)
//        good      (>= goodThreshold)       -> +goodIncrease      (~+0.02)
//        average   (between thresholds)     ->  0
//        poor      (<= -poorThreshold)      -> -half to -full poorDecrease
//                                              (~-0.01 to -0.02)
//   4. Agreement rule: only increase if THIS wave was good, only decrease if
//      THIS wave was poor. Stops a bad wave after a winning streak from still
//      raising difficulty (the average alone would still be high).
//   5. Applies safety caps (maxIncreasePerWave / maxDecreasePerWave) and the
//      overall limits (minimumDifficulty .. maximumDifficulty).
//
// Difficulty is a multiplier on enemy count: 1.0 = the original game.
// This is a plain C# class owned by WaveManager, so no scene setup is needed.
// The values below show up (and can be tuned) on WaveManager in the Inspector.
//
// NOTE: settings whose meaning or default changed were given NEW field names
// (e.g. minimumDifficulty instead of minDifficulty). Unity keeps old values
// saved in scenes under the old names; new names make sure the new defaults
// below are actually used.
[System.Serializable]
public class DifficultyDirector
{
    [Header("Limits")]
    [SerializeField] private float startDifficulty = 1.0f;
    [SerializeField] private float minimumDifficulty = 0.90f;
    [SerializeField] private float maximumDifficulty = 2.50f;

    [Header("Step sizes per wave")]
    [SerializeField] private float excellentIncrease = 0.03f;
    [SerializeField] private float goodIncrease = 0.02f;

    // Full decrease for very poor play. Poor play right at the threshold
    // gets half of this, so with 0.02 the range is -0.01 to -0.02.
    [SerializeField] private float poorDecrease = 0.02f;

    [Header("Safety caps (not the normal step size)")]
    [SerializeField] private float maxIncreasePerWave = 0.25f;
    [SerializeField] private float maxDecreasePerWave = 0.03f;

    [Header("Score thresholds (smoothed score, -1 .. +1)")]
    [SerializeField, Range(0f, 1f)] private float excellentThreshold = 0.70f;
    [SerializeField, Range(0f, 1f)] private float goodThreshold = 0.20f;
    [SerializeField, Range(0f, 1f)] private float poorThreshold = 0.20f;

    [Header("Smoothing (weight of the newest wave)")]
    // Used when the new wave is BETTER than the running average.
    [SerializeField, Range(0.05f, 1f)] private float positiveSmoothing = 0.85f;
    // Used when the new wave is WORSE than the running average.
    [SerializeField, Range(0.05f, 1f)] private float negativeSmoothing = 0.35f;

    [Header("Performance targets (score 0 = on target)")]
    // Killing 90% of a wave counts as "average". 100% = +1, 80% or less = -1.
    [SerializeField, Range(0.5f, 0.99f)] private float targetKillRate = 0.90f;

    // Losing 10% of max health in one wave counts as "average".
    // 0% lost = +1, 20% or more lost = -1.
    [SerializeField, Range(0.01f, 0.5f)] private float targetHealthLostFraction = 0.10f;

    [Header("Score weights (should add up to 1)")]
    [SerializeField] private float killWeight = 0.5f;
    [SerializeField] private float healthWeight = 0.5f;

    private float difficulty = -1f;
    private float smoothedScore;

    public float Difficulty
    {
        get
        {
            // Lazy init so the Inspector value of startDifficulty is used.
            if (difficulty < 0f)
            {
                difficulty = Mathf.Clamp(startDifficulty, minimumDifficulty, maximumDifficulty);
            }

            return difficulty;
        }
    }

    public float MinimumDifficulty => minimumDifficulty;
    public float MaximumDifficulty => maximumDifficulty;
    public bool IsOutsideLimits =>
        Difficulty < minimumDifficulty || Difficulty > maximumDifficulty;

    public float LastScore { get; private set; }
    public float SmoothedScore => smoothedScore;
    public float LastChange { get; private set; }
    public string LastReason { get; private set; } = "";

    // Used when loading a save. Smoothing history is not saved, so it restarts.
    // The saved value is restored EXACTLY, even if it is outside the current
    // limits (e.g. 0.80 from a save made when the minimum was 0.75), so the
    // saved wave is rebuilt with the same enemy count. The next evaluation
    // then moves it back inside the limits.
    public void SetDifficulty(float value)
    {
        difficulty = value > 0f ? value : 1f;
        smoothedScore = 0f;
        LastScore = 0f;
        LastChange = 0f;
        LastReason = "Restored from save";
    }

    // Called once per completed wave. Returns the new difficulty.
    public float EvaluateWave(WaveStats stats, int maxHealth)
    {
        float current = Difficulty;

        // Nothing happened (e.g. no enemies were counted): keep difficulty.
        if (stats == null || stats.TotalEnemies == 0)
        {
            LastScore = 0f;
            LastChange = 0f;
            LastReason = "No enemies counted this wave - difficulty held";
            return current;
        }

        float raw = ScorePerformance(stats, maxHealth);
        LastScore = raw;

        // Asymmetric exponential moving average:
        //   smoothed = old + (raw - old) * rate
        // rate is high when the wave beats the average (react fast to good
        // play) and low when it falls below (react slowly to bad play).
        float rate = raw >= smoothedScore ? positiveSmoothing : negativeSmoothing;
        smoothedScore = Mathf.Lerp(smoothedScore, raw, rate);

        float change;
        string reason;

        if (smoothedScore >= excellentThreshold)
        {
            change = excellentIncrease;
            reason = "Excellent performance";
        }
        else if (smoothedScore >= goodThreshold)
        {
            change = goodIncrease;
            reason = "Good performance";
        }
        else if (smoothedScore <= -poorThreshold)
        {
            // Scale from half to full poorDecrease as the score goes from
            // -poorThreshold down to -1.
            float severity = Mathf.InverseLerp(poorThreshold, 1f, -smoothedScore);
            change = -poorDecrease * Mathf.Lerp(0.5f, 1f, severity);
            reason = "Poor performance";
        }
        else
        {
            change = 0f;
            reason = "Average performance (within thresholds)";
        }

        // Agreement rule: the latest wave must point the same way.
        if (change > 0f && raw < goodThreshold)
        {
            change = 0f;
            reason = "Held: average is high but this wave was not good";
        }
        else if (change < 0f && raw > -poorThreshold)
        {
            change = 0f;
            reason = "Held: average is low but this wave was not poor";
        }

        // Safety caps.
        float capped = Mathf.Clamp(change, -maxDecreasePerWave, maxIncreasePerWave);
        if (!Mathf.Approximately(capped, change))
        {
            reason += " (capped by max step)";
        }

        float next = Mathf.Clamp(current + capped, minimumDifficulty, maximumDifficulty);
        if (!Mathf.Approximately(next, current + capped))
        {
            reason += next >= maximumDifficulty
                ? " (at maximum difficulty)"
                : next <= minimumDifficulty
                    ? " (at minimum difficulty)"
                    : " (moved inside limits)";
        }

        LastChange = next - current;
        LastReason = reason;
        difficulty = next;
        return difficulty;
    }

    // Returns -1 (struggling) .. +1 (dominating). 0 = on target.
    private float ScorePerformance(WaveStats stats, int maxHealth)
    {
        // Kill score: distance from target kill rate, scaled so that
        // a perfect wave = +1 and (target - the same gap) = -1.
        float killGap = 1f - targetKillRate;
        float killScore = Mathf.Clamp(
            (stats.KillRate - targetKillRate) / killGap,
            -1f,
            1f
        );

        // Health score: no damage = +1, target damage = 0, double target = -1.
        float lostFraction = stats.healthLost / Mathf.Max(1, maxHealth);
        float healthScore = Mathf.Clamp(
            1f - lostFraction / targetHealthLostFraction,
            -1f,
            1f
        );

        float totalWeight = Mathf.Max(0.0001f, killWeight + healthWeight);
        return (killScore * killWeight + healthScore * healthWeight) / totalWeight;
    }
}

using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("UI")]
    public Slider healthSlider;

    [Header("Game Over")]
    [SerializeField] private GameManager gameManager;

    // Raised with the amount of health actually lost. Used by WaveManager.
    public event System.Action<int> HealthLost;

    void Start()
    {
        currentHealth = maxHealth;

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }
    public void TakeDamage(int damage)
    {
        if (damage <= 0 || currentHealth <= 0)
        {
            return;
        }

        int healthBefore = currentHealth;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // Report only the health actually lost (overkill damage is ignored)
        // so the difficulty director gets accurate numbers.
        HealthLost?.Invoke(healthBefore - currentHealth);

        UpdateHealthUI();
        Debug.Log("CURRENT HEALTH: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void LoadHealth(int savedHealth)
    {
        currentHealth = Mathf.Clamp(savedHealth, 0, maxHealth);

        UpdateHealthUI();

        Debug.Log("Player health restored: " + currentHealth);
    }

    void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }
    }

    void Die()
    {
        if (gameManager != null)
        {
            gameManager.GameOver();
        }
    }
}
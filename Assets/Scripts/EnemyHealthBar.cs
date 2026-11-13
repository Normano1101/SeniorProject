
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;

    public void SetMaxHealth(int maxHealth)
    {
        if (healthSlider == null)
            return;

        healthSlider.minValue = 0;
        healthSlider.maxValue = maxHealth;
        healthSlider.value = maxHealth;
    }

    public void SetHealth(int currentHealth)
    {
        if (healthSlider == null)
            return;

        healthSlider.value = currentHealth;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

public class Tower : MonoBehaviour
{
    private static readonly List<Tower> activeTowers = new();

    public static IReadOnlyList<Tower> ActiveTowers => activeTowers;

    [SerializeField, Min(1)] private int maxHealth = 100;

    private int currentHealth;
    public float range = 5f;
    public float attackSpeed = 1f;
    public int damage = 10;
    public GameObject projectilePrefab;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    public event Action<Tower, int, int> HealthChanged;

    private float attackTimer = 1f;

    private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = maxHealth;
    }

    private void OnEnable()
    {
        if (!activeTowers.Contains(this))
        {
            activeTowers.Add(this);
        }
    }

    private void OnDisable()
    {
        activeTowers.Remove(this);
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || currentHealth <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - amount);
        HealthChanged?.Invoke(this, currentHealth, maxHealth);

        if (currentHealth == 0)
        {
            Destroy(gameObject);
        }
    }

    public bool TryRepair(int amount, int cost)
    {
        if (amount <= 0 || cost < 0 || currentHealth >= maxHealth)
        {
            return false;
        }

        CurrencyManager currencyManager = CurrencyManager.Instance;
        if (currencyManager == null || !currencyManager.TrySpend(cost))
        {
            return false;
        }

        currentHealth += Mathf.Min(amount, maxHealth - currentHealth);
        HealthChanged?.Invoke(this, currentHealth, maxHealth);
        return true;
    }

    void Update()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        GameObject closestEnemy = null;
        float closestDistance = Mathf.Infinity;

        foreach (GameObject enemy in enemies)
        {
            float distance = Vector2.Distance(
                transform.position,
                enemy.transform.position
            );

            if (distance <= range && distance < closestDistance)
            {
                closestEnemy = enemy;
                closestDistance = distance;
            }
        }

        if (closestEnemy != null)
        {
            attackTimer -= Time.deltaTime;

            if (attackTimer <= 0f)
            {
                // Damage the enemy
                Enemy enemy = closestEnemy.GetComponent<Enemy>();

                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                }

                // Fire visual projectile
                Debug.Log("ATTEMPTING TO FIRE PROJECTILE");

                if (projectilePrefab != null)
                {
                    GameObject projectile = Instantiate(
                        projectilePrefab,
                        transform.position,
                        Quaternion.identity
                    );

                    Debug.Log("PROJECTILE FIRED!");

                    Projectile projectileScript =
                        projectile.GetComponent<Projectile>();

                    if (projectileScript != null)
                    {
                        projectileScript.SetTarget(closestEnemy.transform);
                    }
                }

                attackTimer = 1f / attackSpeed;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
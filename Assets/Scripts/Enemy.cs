
using System;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class EnemySaveData
{
    public float x;
    public float y;
    public int health;
    public int waypoint;
    public string enemy_type;
}

public enum EnemyKind
{
    Basic,
    Runner,
    Heavy
}

public class Enemy : MonoBehaviour
{
    [SerializeField] private int currencyReward = 12;
    public int health = 50;
    public float moveSpeed = 1f;
    public int baseDamage = 10;

    public EnemyKind Kind { get; private set; }

    public WaveManager waveManager;

    private List<Vector3> path;
    private int currentWaypoint = 0;
    private bool hasBeenRemoved;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Configure(EnemyKind kind, int wave)
    {
        Kind = kind;
        int waveBonus = Mathf.Max(0, wave - 1);

        switch (kind)
        {
            case EnemyKind.Runner:
                health = 55 + waveBonus * 12;
                moveSpeed = 1.7f;
                baseDamage = 8;
                currencyReward = 10 + Mathf.Min(waveBonus, 5);
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = new Color(0.55f, 1f, 0.45f);
                }
                break;
            case EnemyKind.Heavy:
                health = 220 + waveBonus * 45;
                moveSpeed = 0.55f;
                baseDamage = 20;
                currencyReward = 24 + waveBonus * 2;
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = new Color(0.75f, 0.65f, 1f);
                }
                break;
            default:
                Kind = EnemyKind.Basic;
                health = 100 + waveBonus * 20;
                moveSpeed = 1f;
                baseDamage = 10;
                currencyReward = 12 + waveBonus;
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.white;
                }
                break;
        }
    }

    public void SetPath(List<Vector3> newPath)
    {
        path = newPath;

        if (path != null && path.Count > 0)
        {
            // Spawn directly on the first path tile
            transform.position = path[0];

            // First target will be the second tile
            currentWaypoint = 1;
        }
    }

    public EnemySaveData GetSaveData()
    {
        return new EnemySaveData
        {
            x = transform.position.x,
            y = transform.position.y,
            health = health,
            waypoint = currentWaypoint,
            enemy_type = Kind.ToString()
        };
    }

    public void LoadSaveData(EnemySaveData data, int wave)
    {
        if (data == null)
        {
            Debug.LogError("Enemy save data is missing.");
            return;
        }

        if (!Enum.TryParse(data.enemy_type, true, out EnemyKind savedKind))
        {
            savedKind = EnemyKind.Basic;
        }

        Configure(savedKind, wave);
        health = Mathf.Max(1, data.health);

        transform.position = new Vector3(
            data.x,
            data.y,
            transform.position.z
        );

        currentWaypoint = Mathf.Clamp(
            data.waypoint,
            0,
            path != null ? path.Count : 0
        );

        hasBeenRemoved = false;
    }

    void Update()
    {
        if (path == null || path.Count == 0)
            return;

        if (currentWaypoint >= path.Count)
            return;

        Vector3 target = path[currentWaypoint];

        transform.position = Vector2.MoveTowards(
            transform.position,
            target,
            moveSpeed * Time.deltaTime
        );

        if (Vector2.Distance(transform.position, target) < 0.1f)
        {
            currentWaypoint++;

            if (currentWaypoint >= path.Count)
            {
                ReachedEnd();
            }
        }
    }

    void ReachedEnd()
    {
        if (hasBeenRemoved)
            return;

        // Find the player's health system
        PlayerHealth playerHealth =
            FindAnyObjectByType<PlayerHealth>();

        // Damage the base when the enemy reaches the end
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(baseDamage);
        }

        RemoveEnemy(wasKilled: false);
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || hasBeenRemoved)
            return;

        health = Mathf.Max(0, health - damage);

        if (health <= 0)
        {
            RemoveEnemy(wasKilled: true);
        }
    }

    void RemoveEnemy(bool wasKilled)
    {
        if (hasBeenRemoved)
            return;

        hasBeenRemoved = true;

        if (wasKilled && CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddCurrency(currencyReward);
        }

        if (waveManager != null)
        {
            waveManager.EnemyRemoved();
        }

        Destroy(gameObject);
    }
}


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
    Heavy,
    TowerAttacker
}

public class Enemy : MonoBehaviour
{
    [SerializeField] private int currencyReward = 12;

    public int health = 50;
    public float moveSpeed = 1f;
    public int baseDamage = 10;

    // Tower attacker settings
    public float towerDetectionRange = 3f;
    public float towerAttackRange = 1.2f;
    public float towerAttackInterval = 1f;
    public int towerDamage = 10;

    [Tooltip("Distance to stop to the left of the tower's center.")]
    public float towerSideDistance = 1.2f;

    public EnemyKind Kind { get; private set; }

    public WaveManager waveManager;

    private List<Vector3> path;
    private int currentWaypoint = 0;
    private bool hasBeenRemoved;
    private SpriteRenderer spriteRenderer;

    private Tower targetTower;
    private float towerAttackTimer;
    private Vector3 towerApproachPosition;
    private bool hasApproachPosition;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Configure(EnemyKind kind, int wave)
    {
        Kind = kind;
        int waveBonus = Mathf.Max(0, wave - 1);

        targetTower = null;
        towerAttackTimer = 0f;
        hasApproachPosition = false;

        switch (kind)
        {
            case EnemyKind.Runner:
                health = 55 + waveBonus * 12;
                moveSpeed = 1.7f;
                baseDamage = 8;
                currencyReward = 10 + Mathf.Min(waveBonus, 5);

                if (spriteRenderer != null)
                    spriteRenderer.color = new Color(0.55f, 1f, 0.45f);
                break;

            case EnemyKind.Heavy:
                health = 220 + waveBonus * 45;
                moveSpeed = 0.55f;
                baseDamage = 20;
                currencyReward = 24 + waveBonus * 2;

                if (spriteRenderer != null)
                    spriteRenderer.color = new Color(0.75f, 0.65f, 1f);
                break;

            case EnemyKind.TowerAttacker:
                health = 120 + waveBonus * 20;
                moveSpeed = 0.8f;
                baseDamage = 10;
                towerDamage = 10 + waveBonus * 2;
                currencyReward = 18 + waveBonus;

                if (spriteRenderer != null)
                    spriteRenderer.color = new Color(1f, 0.35f, 0.25f);
                break;

            default:
                Kind = EnemyKind.Basic;
                health = 100 + waveBonus * 20;
                moveSpeed = 1f;
                baseDamage = 10;
                currencyReward = 12 + waveBonus;

                if (spriteRenderer != null)
                    spriteRenderer.color = Color.white;
                break;
        }
    }

    public void SetPath(List<Vector3> newPath)
    {
        path = newPath;

        if (path != null && path.Count > 0)
        {
            transform.position = path[0];
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
            savedKind = EnemyKind.Basic;

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

    public void LoadSaveData(EnemySaveData data)
    {
        LoadSaveData(data, waveManager != null ? waveManager.currentWave : 1);
    }

    void Update()
    {
        if (hasBeenRemoved || path == null || path.Count == 0)
            return;

        if (Kind == EnemyKind.TowerAttacker)
        {
            UpdateTowerAttack();

            if (targetTower != null)
                return;
        }

        MoveAlongPath();
    }

    void MoveAlongPath()
    {
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
                ReachedEnd();
        }
    }

    void UpdateTowerAttack()
    {
        // Find a tower if there is no current target.
        if (targetTower == null)
        {
            targetTower = FindNearbyTower();

            if (targetTower == null)
            {
                hasApproachPosition = false;
                return;
            }

            // Always approach from the left side of the tower.
            towerApproachPosition =
                targetTower.transform.position
                + Vector3.left * towerSideDistance;

            hasApproachPosition = true;
        }

        if (!hasApproachPosition)
            return;

        float distanceToTower = Vector2.Distance(
            transform.position,
            targetTower.transform.position
        );

        // Attack when close enough to the tower.
        if (distanceToTower <= towerAttackRange)
        {
            towerAttackTimer -= Time.deltaTime;

            if (towerAttackTimer <= 0f)
            {
                targetTower.TakeDamage(towerDamage);
                towerAttackTimer = towerAttackInterval;
            }

            return;
        }

        // Move toward the position on the left.
        transform.position = Vector2.MoveTowards(
            transform.position,
            towerApproachPosition,
            moveSpeed * Time.deltaTime
        );
    }

    Tower FindNearbyTower()
    {
        Tower[] towers = FindObjectsByType<Tower>(
            FindObjectsSortMode.None
        );

        Tower closestTower = null;
        float closestDistance = towerDetectionRange;

        foreach (Tower tower in towers)
        {
            if (tower == null)
                continue;

            float distance = Vector2.Distance(
                transform.position,
                tower.transform.position
            );

            if (distance <= closestDistance)
            {
                closestDistance = distance;
                closestTower = tower;
            }
        }

        return closestTower;
    }

    void ReachedEnd()
    {
        if (hasBeenRemoved)
            return;

        PlayerHealth playerHealth =
            FindAnyObjectByType<PlayerHealth>();

        if (playerHealth != null)
            playerHealth.TakeDamage(baseDamage);

        RemoveEnemy(wasKilled: false);
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || hasBeenRemoved)
            return;

        health = Mathf.Max(0, health - damage);

        if (health <= 0)
            RemoveEnemy(wasKilled: true);
    }

    void RemoveEnemy(bool wasKilled)
    {
        if (hasBeenRemoved)
            return;

        hasBeenRemoved = true;

        if (wasKilled && CurrencyManager.Instance != null)
            CurrencyManager.Instance.AddCurrency(currencyReward);

        if (waveManager != null)
            waveManager.EnemyRemoved();

        Destroy(gameObject);
    }
}

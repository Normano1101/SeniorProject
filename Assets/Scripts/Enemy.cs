using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField, Min(0)] private int currencyReward = 10;
    [SerializeField, Min(0.1f)] private float towerAttackRange = 1.25f;
    [SerializeField, Min(1)] private int towerAttackDamage = 10;
    [SerializeField, Min(0.1f)] private float towerAttackInterval = 1f;

    public int health = 50;
    public float moveSpeed = 1f;
    public WaveManager waveManager;

    private List<Vector3> path;
    private int currentWaypoint = 0;
    private bool hasBeenRemoved;
    private float nextTowerAttackTime;

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

    void Update()
    {
        if (TryAttackTower())
        {
            return;
        }

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
        RemoveEnemy(wasKilled: false);
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || hasBeenRemoved)
        {
            return;
        }

        health = Mathf.Max(0, health - damage);

        if (health <= 0)
        {
            RemoveEnemy(wasKilled: true);
        }
    }

    private bool TryAttackTower()
    {
        Tower target = null;
        float closestDistance = towerAttackRange;

        foreach (Tower tower in Tower.ActiveTowers)
        {
            if (tower == null)
            {
                continue;
            }

            float distance = Vector2.Distance(transform.position, tower.transform.position);
            if (distance <= closestDistance)
            {
                target = tower;
                closestDistance = distance;
            }
        }

        if (target == null)
        {
            return false;
        }

        if (Time.time >= nextTowerAttackTime)
        {
            target.TakeDamage(towerAttackDamage);
            nextTowerAttackTime = Time.time + towerAttackInterval;
        }

        return true;
    }

    private void RemoveEnemy(bool wasKilled)
    {
        if (hasBeenRemoved)
            return;

        hasBeenRemoved = true;

        if (wasKilled && currencyReward > 0 && CurrencyManager.Instance != null)
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
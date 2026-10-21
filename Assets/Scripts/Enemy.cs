
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class EnemySaveData
{
    public float x;
    public float y;
    public int health;
    public int waypoint;
}

public class Enemy : MonoBehaviour
{
    public int health = 50;
    public float moveSpeed = 1f;
    public int baseDamage = 10;

    public WaveManager waveManager;

    private List<Vector3> path;
    private int currentWaypoint = 0;
    private bool hasBeenRemoved;

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
            waypoint = currentWaypoint
        };
    }

    public void LoadSaveData(EnemySaveData data)
    {
        if (data == null)
        {
            Debug.LogError("Enemy save data is missing.");
            return;
        }

        health = data.health;

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
            FindFirstObjectByType<PlayerHealth>();

        // Damage the base when the enemy reaches the end
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(baseDamage);
        }

        RemoveEnemy();
    }

    public void TakeDamage(int damage)
    {
        if (hasBeenRemoved)
            return;

        health -= damage;

        if (health <= 0)
        {
            RemoveEnemy();
        }
    }

    void RemoveEnemy()
    {
        if (hasBeenRemoved)
            return;

        hasBeenRemoved = true;

        if (waveManager != null)
        {
            waveManager.EnemyRemoved();
        }

        Destroy(gameObject);
    }
}

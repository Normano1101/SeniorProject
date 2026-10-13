
using System.Collections.Generic;
using UnityEngine;

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

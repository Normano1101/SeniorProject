using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int health = 50;
    public float moveSpeed = 1f;

    public Transform[] waypoints;

    private int currentWaypoint = 0;

    void Update()
    {
        if (waypoints.Length == 0)
            return;

        Transform target = waypoints[currentWaypoint];

        transform.position = Vector2.MoveTowards(
            transform.position,
            target.position,
            moveSpeed * Time.deltaTime
        );

        if (Vector2.Distance(transform.position, target.position) < 0.1f)
        {
            currentWaypoint++;

            if (currentWaypoint >= waypoints.Length)
            {
                Destroy(gameObject);
                return;
            }
        }
    }

    public void TakeDamage(int damage)
    {
        health -= damage;

        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }
}
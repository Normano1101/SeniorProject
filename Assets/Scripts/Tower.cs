using UnityEngine;

public class Tower : MonoBehaviour
{
    public float range = 5f;
    public float attackSpeed = 1f;
    public int damage = 10;
    public GameObject projectilePrefab;

    private float attackTimer = 1f;

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

using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 3f;

    private Transform target;
    private int damage;
    private float splashRadius;

    public void SetTarget(
        Transform newTarget,
        int projectileDamage,
        float projectileSplashRadius
    )
    {
        target = newTarget;
        damage = projectileDamage;
        splashRadius = projectileSplashRadius;
    }

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = Vector2.MoveTowards(
            transform.position,
            target.position,
            speed * Time.deltaTime
        );

        if (Vector2.Distance(transform.position, target.position) < 0.1f)
        {
            if (splashRadius > 0f)
            {
                foreach (GameObject enemyObject in
                         GameObject.FindGameObjectsWithTag("Enemy"))
                {
                    if (enemyObject != null &&
                        Vector2.Distance(
                            target.position,
                            enemyObject.transform.position
                        ) <= splashRadius)
                    {
                        Enemy enemy = enemyObject.GetComponent<Enemy>();

                        if (enemy != null)
                            enemy.TakeDamage(damage);
                    }
                }
            }
            else
            {
                Enemy enemy = target.GetComponent<Enemy>();

                if (enemy != null)
                    enemy.TakeDamage(damage);
            }

            Destroy(gameObject);
        }
    }
}

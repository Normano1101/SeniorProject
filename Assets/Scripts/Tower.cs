using UnityEngine;

public enum TowerKind
{
    Dart,
    Rapid,
    Cannon
}

[System.Serializable]
public class TowerSaveData
{
    public string tower_type;
    public float position_x;
    public float position_y;
    public int level;
}

public class Tower : MonoBehaviour
{
    [SerializeField] private int cost = 100;

    public string towerType = "BasicTower";
    public int level = 1;
    public float range = 5f;
    public float attackSpeed = 1f;
    public int damage = 10;
    public GameObject projectilePrefab;

    private float splashRadius;
    private float attackTimer = 1f;

    public int Cost => cost;

    public void Configure(TowerKind kind)
    {
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();

        switch (kind)
        {
            case TowerKind.Rapid:
                towerType = "RapidTower";
                cost = 160;
                range = 4.5f;
                attackSpeed = 2.5f;
                damage = 8;
                splashRadius = 0f;
                if (sprite != null)
                {
                    sprite.color = new Color(0.45f, 0.85f, 1f);
                }
                break;
            case TowerKind.Cannon:
                towerType = "CannonTower";
                cost = 220;
                range = 3.5f;
                attackSpeed = 0.65f;
                damage = 32;
                splashRadius = 0.8f;
                if (sprite != null)
                {
                    sprite.color = new Color(1f, 0.65f, 0.35f);
                }
                break;
            default:
                towerType = "BasicTower";
                cost = 100;
                range = 5f;
                attackSpeed = 1f;
                damage = 15;
                splashRadius = 0f;
                if (sprite != null)
                {
                    sprite.color = Color.white;
                }
                break;
        }
    }

    public static int GetCost(TowerKind kind)
    {
        switch (kind)
        {
            case TowerKind.Rapid:
                return 160;
            case TowerKind.Cannon:
                return 220;
            default:
                return 100;
        }
    }

    public TowerSaveData GetSaveData()
    {
        return new TowerSaveData
        {
            tower_type = towerType,
            position_x = transform.position.x,
            position_y = transform.position.y,
            level = level
        };
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

        if (closestEnemy == null)
        {
            return;
        }

        attackTimer -= Time.deltaTime;
        if (attackTimer > 0f)
        {
            return;
        }

        Enemy targetEnemy = closestEnemy.GetComponent<Enemy>();
        if (targetEnemy != null)
        {
            if (splashRadius > 0f)
            {
                DamageEnemiesAround(closestEnemy.transform.position);
            }
            else
            {
                targetEnemy.TakeDamage(damage);
            }
        }

        if (projectilePrefab != null)
        {
            GameObject projectile = Instantiate(
                projectilePrefab,
                transform.position,
                Quaternion.identity
            );

            Projectile projectileScript = projectile.GetComponent<Projectile>();
            if (projectileScript != null)
            {
                projectileScript.SetTarget(closestEnemy.transform);
            }
        }

        attackTimer = 1f / Mathf.Max(0.1f, attackSpeed);
    }

    private void DamageEnemiesAround(Vector3 center)
    {
        foreach (GameObject target in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            if (target != null &&
                Vector2.Distance(center, target.transform.position) <= splashRadius)
            {
                Enemy enemy = target.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}

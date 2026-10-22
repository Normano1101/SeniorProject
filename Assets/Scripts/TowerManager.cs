using System.Collections.Generic;
using UnityEngine;

public class TowerManager : MonoBehaviour
{
    public GameObject basicTowerPrefab;
    public PathManager pathManager;

    private readonly List<GameObject> placedTowers = new List<GameObject>();

    public Sprite TowerSprite
    {
        get
        {
            if (basicTowerPrefab == null)
            {
                return null;
            }

            SpriteRenderer spriteRenderer =
                basicTowerPrefab.GetComponent<SpriteRenderer>();
            return spriteRenderer != null ? spriteRenderer.sprite : null;
        }
    }

    private void Awake()
    {
        if (pathManager == null)
        {
            pathManager = FindAnyObjectByType<PathManager>();
        }
    }

    public void RegisterTower(GameObject tower)
    {
        if (tower != null && !placedTowers.Contains(tower))
        {
            placedTowers.Add(tower);
        }
    }

    public bool CanPlaceAt(Vector3 position, out string reason)
    {
        if (pathManager == null || pathManager.pathTilemap == null)
        {
            reason = "The map path is not configured.";
            return false;
        }

        if (pathManager.IsPathPosition(position))
        {
            reason = "Towers cannot be placed on the path.";
            return false;
        }

        foreach (GameObject towerObject in placedTowers)
        {
            if (towerObject != null &&
                Vector2.Distance(position, towerObject.transform.position) < 0.65f)
            {
                reason = "There is already a tower in that spot.";
                return false;
            }
        }

        reason = string.Empty;
        return true;
    }

    public bool TryPlaceTower(TowerKind kind, Vector3 position, out string reason)
    {
        if (!CanPlaceAt(position, out reason))
        {
            return false;
        }

        if (basicTowerPrefab == null)
        {
            reason = "The tower prefab is not assigned.";
            Debug.LogError(reason);
            return false;
        }

        CurrencyManager currency = CurrencyManager.Instance;
        if (currency == null)
        {
            reason = "The currency manager is missing.";
            Debug.LogError(reason);
            return false;
        }

        int cost = Tower.GetCost(kind);
        if (!currency.CanAfford(cost))
        {
            reason = "Not enough currency for that tower.";
            return false;
        }

        GameObject towerObject = Instantiate(
            basicTowerPrefab,
            position,
            Quaternion.identity
        );
        Tower tower = towerObject.GetComponent<Tower>();
        if (tower == null)
        {
            Destroy(towerObject);
            reason = "The tower prefab has no Tower component.";
            Debug.LogError(reason);
            return false;
        }

        tower.Configure(kind);
        RegisterTower(towerObject);

        if (!currency.TrySpend(cost))
        {
            placedTowers.Remove(towerObject);
            Destroy(towerObject);
            reason = "Currency changed before the tower could be purchased.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    public TowerSaveData[] GetTowersSaveData()
    {
        List<TowerSaveData> towers = new List<TowerSaveData>();

        foreach (GameObject towerObject in placedTowers)
        {
            if (towerObject == null)
            {
                continue;
            }

            Tower tower = towerObject.GetComponent<Tower>();
            if (tower != null)
            {
                towers.Add(tower.GetSaveData());
            }
        }

        return towers.ToArray();
    }

    public void RestoreTowers(TowerSaveData[] savedTowers)
    {
        if (savedTowers != null && basicTowerPrefab == null)
        {
            Debug.LogError("Cannot restore towers because the tower prefab is not assigned.");
            return;
        }

        foreach (GameObject tower in placedTowers)
        {
            if (tower != null)
            {
                Destroy(tower);
            }
        }

        placedTowers.Clear();

        if (savedTowers == null)
        {
            return;
        }

        foreach (TowerSaveData data in savedTowers)
        {
            if (data == null)
            {
                Debug.LogWarning("Skipping an empty tower save entry.");
                continue;
            }

            if (!TryParseTowerKind(data.tower_type, out TowerKind kind))
            {
                Debug.LogWarning("Unknown tower type: " + data.tower_type);
                continue;
            }

            GameObject newTower = Instantiate(
                basicTowerPrefab,
                new Vector3(data.position_x, data.position_y, 0f),
                Quaternion.identity
            );

            Tower tower = newTower.GetComponent<Tower>();
            if (tower == null)
            {
                Debug.LogError("The tower prefab has no Tower component.");
                Destroy(newTower);
                continue;
            }

            tower.Configure(kind);
            tower.level = Mathf.Max(1, data.level);
            RegisterTower(newTower);
        }

        Debug.Log("Restored " + placedTowers.Count + " towers.");
    }

    private static bool TryParseTowerKind(string savedType, out TowerKind kind)
    {
        switch (savedType)
        {
            case "BasicTower":
            case "Dart":
                kind = TowerKind.Dart;
                return true;
            case "RapidTower":
            case "Rapid":
                kind = TowerKind.Rapid;
                return true;
            case "CannonTower":
            case "Cannon":
                kind = TowerKind.Cannon;
                return true;
            default:
                kind = default;
                return false;
        }
    }
}

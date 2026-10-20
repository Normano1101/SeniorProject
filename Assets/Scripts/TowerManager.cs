using System.Collections.Generic;
using UnityEngine;

public class TowerManager : MonoBehaviour
{
    public GameObject basicTowerPrefab;

    private readonly List<GameObject> placedTowers =
        new List<GameObject>();

    public void RegisterTower(GameObject tower)
    {
        if (tower != null && !placedTowers.Contains(tower))
        {
            placedTowers.Add(tower);
        }
    }

    public TowerSaveData[] GetTowersSaveData()
    {
        List<TowerSaveData> towers = new List<TowerSaveData>();

        foreach (GameObject towerObject in placedTowers)
        {
            if (towerObject == null)
                continue;

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
        // Remove currently placed towers
        foreach (GameObject tower in placedTowers)
        {
            if (tower != null)
            {
                Destroy(tower);
            }
        }

        placedTowers.Clear();

        if (savedTowers == null)
            return;

        foreach (TowerSaveData data in savedTowers)
        {
            if (data.tower_type != "BasicTower")
            {
                Debug.LogWarning(
                    "Unknown tower type: " + data.tower_type
                );
                continue;
            }

            GameObject newTower = Instantiate(
                basicTowerPrefab,
                new Vector3(data.position_x, data.position_y, 0f),
                Quaternion.identity
            );

            Tower towerScript = newTower.GetComponent<Tower>();

            if (towerScript != null)
            {
                towerScript.towerType = data.tower_type;
                towerScript.level = data.level;
            }

            RegisterTower(newTower);
        }

        Debug.Log("Restored " + placedTowers.Count + " towers.");
    }
}
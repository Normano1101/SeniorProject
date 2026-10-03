using UnityEngine;
using UnityEngine.EventSystems;

public class TowerDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public GameObject towerPrefab;

    private GameObject draggedTower;

    public void OnBeginDrag(PointerEventData eventData)
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(eventData.position);
        mousePosition.z = 0f;

        draggedTower = Instantiate(towerPrefab, mousePosition, Quaternion.identity);

        Tower tower = draggedTower.GetComponent<Tower>();

        if (tower != null)
        {
            tower.enabled = false;
        }

        Debug.Log("Started dragging tower!");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggedTower == null)
            return;

        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(eventData.position);
        mousePosition.z = 0f;

        draggedTower.transform.position = mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (draggedTower == null)
            return;

        Tower tower = draggedTower.GetComponent<Tower>();

        if (tower != null)
        {
            tower.enabled = true;
        }

        Debug.Log("Placed tower!");

        draggedTower = null;
    }
}
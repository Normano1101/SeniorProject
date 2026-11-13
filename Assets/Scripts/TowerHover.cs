
using UnityEngine;
using UnityEngine.EventSystems;

public class TowerHover : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    public TowerTooltip tooltip;

    [TextArea(3, 8)]
    public string description;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltip != null)
            tooltip.ShowTooltip(description);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltip != null)
            tooltip.HideTooltip();
    }

    private void OnDisable()
    {
        if (tooltip != null)
            tooltip.HideTooltip();
    }
}


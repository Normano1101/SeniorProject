
using UnityEngine;
using TMPro;

public class TowerTooltip : MonoBehaviour
{
    public GameObject tooltipPanel;
    public TMP_Text tooltipText;

    public void ShowTooltip(string description)
    {
        if (tooltipPanel == null || tooltipText == null)
            return;

        tooltipText.text = description;
        tooltipPanel.SetActive(true);
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }
}


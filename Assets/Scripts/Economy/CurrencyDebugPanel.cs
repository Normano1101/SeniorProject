using UnityEngine;

public class CurrencyDebugPanel : MonoBehaviour
{
    [SerializeField] private int addAmount = 10;
    [SerializeField, Min(1)] private int repairAmount = 25;
    [SerializeField, Min(0)] private int repairCost = 25;

    private void OnGUI()
    {
        if (CurrencyManager.Instance == null)
        {
            return;
        }

        const int panelWidth = 240;
        const int margin = 16;
        int panelHeight = 152 + Tower.ActiveTowers.Count * 20;

        GUI.Box(new Rect(margin, margin, panelWidth, panelHeight), "Currency Test");
        GUI.Label(new Rect(margin + 16, margin + 32, panelWidth - 32, 24),
            $"Current: {CurrencyManager.Instance.CurrentCurrency}");

        int labelY = margin + 56;
        foreach (Tower tower in Tower.ActiveTowers)
        {
            GUI.Label(new Rect(margin + 16, labelY, panelWidth - 32, 20),
                $"{tower.name}: {tower.CurrentHealth}/{tower.MaxHealth} HP");
            labelY += 20;
        }

        if (GUI.Button(new Rect(margin + 16, labelY + 4, panelWidth - 32, 28),
                $"Add {addAmount}"))
        {
            CurrencyManager.Instance.AddCurrency(addAmount);
        }

        Tower damagedTower = null;
        foreach (Tower tower in Tower.ActiveTowers)
        {
            if (tower != null && tower.CurrentHealth < tower.MaxHealth)
            {
                damagedTower = tower;
                break;
            }
        }

        GUI.enabled = damagedTower != null;
        if (GUI.Button(new Rect(margin + 16, labelY + 40, panelWidth - 32, 28),
                $"Repair {repairAmount} HP ({repairCost})"))
        {
            damagedTower.TryRepair(repairAmount, repairCost);
        }
        GUI.enabled = true;
    }
}

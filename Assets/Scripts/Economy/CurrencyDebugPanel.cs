using UnityEngine;

public class CurrencyDebugPanel : MonoBehaviour
{
    [SerializeField] private int addAmount = 10;
    [SerializeField] private int spendAmount = 25;

    private void OnGUI()
    {
        if (CurrencyManager.Instance == null)
        {
            return;
        }

        const int panelWidth = 240;
        const int panelHeight = 150;
        const int margin = 16;

        GUI.Box(new Rect(margin, margin, panelWidth, panelHeight), "Currency Test");
        GUI.Label(new Rect(margin + 16, margin + 32, panelWidth - 32, 24),
            $"Current: {CurrencyManager.Instance.CurrentCurrency}");

        if (GUI.Button(new Rect(margin + 16, margin + 64, panelWidth - 32, 28),
                $"Add {addAmount}"))
        {
            CurrencyManager.Instance.AddCurrency(addAmount);
        }

        if (GUI.Button(new Rect(margin + 16, margin + 100, panelWidth - 32, 28),
                $"Spend {spendAmount}"))
        {
            CurrencyManager.Instance.TrySpend(spendAmount);
        }
    }
}

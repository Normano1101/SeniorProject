using System;
using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [SerializeField] private int startingCurrency = 100;

    public int CurrentCurrency { get; private set; }

    public event Action<int> CurrencyChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CurrentCurrency = Mathf.Max(0, startingCurrency);
        CurrencyChanged?.Invoke(CurrentCurrency);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool CanAfford(int amount)
    {
        return amount >= 0 && CurrentCurrency >= amount;
    }

    public bool TrySpend(int amount)
    {
        if (!CanAfford(amount))
        {
            return false;
        }

        CurrentCurrency -= amount;
        CurrencyChanged?.Invoke(CurrentCurrency);
        return true;
    }

    public void AddCurrency(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        CurrentCurrency += amount;
        CurrencyChanged?.Invoke(CurrentCurrency);
    }

    public void SetCurrency(int amount)
    {
        CurrentCurrency = Mathf.Max(0, amount);
        CurrencyChanged?.Invoke(CurrentCurrency);
    }

    public void ResetCurrency()
    {
        SetCurrency(startingCurrency);
    }
}

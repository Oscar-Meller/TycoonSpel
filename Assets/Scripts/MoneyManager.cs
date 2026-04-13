using System;
using UnityEngine;
using TMPro;

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance;

    // Notifies listeners when the player's money value changes.
    public static Action<int> OnMoneyChanged;

    public int money = 0;        // Spelarens pengar
    public int storedMoney = 0;  // Pending money

    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI pendingText; // CollectorScreen

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        UpdateUI();
    }

    // När drop når collector
    public void AddStoredMoney(int amount)
    {
        storedMoney += amount;
        UpdateUI();
    }

    // När spelaren går på pad
    public void CollectMoney()
    {
        if (storedMoney <= 0) return;

        money += storedMoney;
        storedMoney = 0;

        UpdateUI();
    }

    // Försök att spendera pengar. Returnerar true om köp lyckades.
    public bool TrySpend(int amount)
    {
        if (amount <= 0)
            return true;

        if (money >= amount)
        {
            money -= amount;
            UpdateUI();
            return true;
        }

        return false;
    }

    void UpdateUI()
    {
        if (moneyText != null)
            moneyText.text = "Cash: " + money;

        if (pendingText != null)
            pendingText.text = "Cash: " + storedMoney;

        OnMoneyChanged?.Invoke(money);
    }
}
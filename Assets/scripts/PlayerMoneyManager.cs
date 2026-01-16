using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;

public class PlayerMoneyManager : MonoBehaviour
{
    [Header("Money Settings")]
    [SerializeField] private float startingBalance = 100f;

    [Header("UI Display")]
    [SerializeField] private TMP_Text moneyDisplayText;
    [SerializeField] private string moneyPrefix = "$";
    [SerializeField] private string moneySuffix = "";
    [SerializeField] private bool showDecimals = true;

    private float currentBalance;
    private List<Transaction> transactionHistory = new List<Transaction>();

    // Events for UI updates
    public event Action<float> OnMoneyChanged;
    public event Action<Transaction> OnTransactionAdded;

    [System.Serializable]
    public class Transaction
    {
        public string description;
        public float amount;
        public float balanceAfter;
        public DateTime timestamp;

        public Transaction(string desc, float amt, float balance)
        {
            description = desc;
            amount = amt;
            balanceAfter = balance;
            timestamp = DateTime.Now;
        }

        public override string ToString()
        {
            string sign = amount >= 0 ? "+" : "";
            return $"{description}: {sign}{amount:F2} (Balance: {balanceAfter:F2})";
        }
    }

    private void Awake()
    {
        currentBalance = startingBalance;
        LogTransaction("Initial balance", startingBalance);
        UpdateMoneyDisplay();
    }

    private void LogTransaction(string description, float amount)
    {
        Transaction trans = new Transaction(description, amount, currentBalance);
        transactionHistory.Add(trans);
        OnTransactionAdded?.Invoke(trans);
    }

    private void UpdateMoneyDisplay()
    {
        if (moneyDisplayText != null)
        {
            string formattedAmount = showDecimals ?
                currentBalance.ToString("F2") :
                Mathf.Floor(currentBalance).ToString("F0");

            moneyDisplayText.text = $"{moneyPrefix}{formattedAmount}{moneySuffix}";
        }
    }

    /// <summary>
    /// Add money to the player's balance
    /// </summary>
    public bool AddMoney(float amount, string description = "Money added")
    {
        if (amount < 0)
        {
            Debug.LogWarning("Cannot add negative amount");
            return false;
        }

        currentBalance += amount;
        LogTransaction(description, amount);
        OnMoneyChanged?.Invoke(currentBalance);
        UpdateMoneyDisplay();

        Debug.Log($"Added ${amount:F2}. New balance: ${currentBalance:F2}");
        return true;
    }

    /// <summary>
    /// Subtract money from the player's balance
    /// </summary>
    public bool SubtractMoney(float amount, string description = "Money spent")
    {
        if (amount < 0)
        {
            Debug.LogWarning("Cannot subtract negative amount");
            return false;
        }

        if (amount > currentBalance)
        {
            Debug.LogWarning($"Insufficient funds. Required: ${amount:F2}, Available: ${currentBalance:F2}");
            return false;
        }

        currentBalance -= amount;
        LogTransaction(description, -amount);
        OnMoneyChanged?.Invoke(currentBalance);
        UpdateMoneyDisplay();

        Debug.Log($"Spent ${amount:F2}. New balance: ${currentBalance:F2}");
        return true;
    }

    /// <summary>
    /// Check if player can afford a purchase
    /// </summary>
    public bool CanAfford(float amount)
    {
        return currentBalance >= amount;
    }

    /// <summary>
    /// Get current balance
    /// </summary>
    public float GetBalance()
    {
        return currentBalance;
    }

    /// <summary>
    /// Set balance to a specific amount
    /// </summary>
    public void SetBalance(float amount, string description = "Balance set")
    {
        if (amount < 0)
        {
            Debug.LogWarning("Balance cannot be negative");
            return;
        }

        float difference = amount - currentBalance;
        currentBalance = amount;
        LogTransaction(description, difference);
        OnMoneyChanged?.Invoke(currentBalance);
        UpdateMoneyDisplay();
    }

    /// <summary>
    /// Get transaction history
    /// </summary>
    public List<Transaction> GetTransactionHistory(int limit = 0)
    {
        if (limit > 0 && limit < transactionHistory.Count)
        {
            int startIndex = transactionHistory.Count - limit;
            return transactionHistory.GetRange(startIndex, limit);
        }
        return new List<Transaction>(transactionHistory);
    }

    /// <summary>
    /// Print transaction history to console
    /// </summary>
    public void PrintHistory(int limit = 10)
    {
        List<Transaction> recent = GetTransactionHistory(limit);
        Debug.Log("=== Transaction History ===");
        foreach (Transaction trans in recent)
        {
            Debug.Log(trans.ToString());
        }
        Debug.Log($"Current Balance: ${currentBalance:F2}");
    }

    /// <summary>
    /// Reset money to starting balance
    /// </summary>
    public void ResetMoney()
    {
        currentBalance = startingBalance;
        transactionHistory.Clear();
        LogTransaction("Reset to starting balance", startingBalance);
        OnMoneyChanged?.Invoke(currentBalance);
        UpdateMoneyDisplay();
    }

    /// <summary>
    /// Manually refresh the display (useful if you change the text reference at runtime)
    /// </summary>
    public void RefreshDisplay()
    {
        UpdateMoneyDisplay();
    }

    // Example usage methods - remove or modify as needed
    private void Update()
    {
        // Example: Press keys to test
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            AddMoney(50f, "Quest reward");
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SubtractMoney(25f, "Bought potion");
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            PrintHistory();
        }
    }
}
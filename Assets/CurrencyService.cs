using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CurrencyService : MonoBehaviour
{
    public static CurrencyService Instance { get; private set; }

    [Header("Stan")]
    [SerializeField] private int _balance = 0;

    [Header("Wyświetlanie")]
    [SerializeField] private TMP_Text[] _balanceTexts;
    [SerializeField] private string _format = "{0}";

    public event Action<int> OnBalanceChanged;
    public event Action<int> OnEarned;
    public event Action<int> OnSpent;

    public int Balance => _balance;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        RefreshTexts();
    }

    public void Add(int amount)
    {
        if (amount <= 0) return;

        _balance += amount;
        RefreshTexts();
        OnEarned?.Invoke(amount);
        OnBalanceChanged?.Invoke(_balance);
    }

    public bool CanAfford(int cost) => cost >= 0 && _balance >= cost;

    public bool TrySpend(int cost)
    {
        if (!CanAfford(cost)) return false;

        _balance -= cost;
        RefreshTexts();
        OnSpent?.Invoke(cost);
        OnBalanceChanged?.Invoke(_balance);
        return true;
    }

    public void SetBalance(int value)
    {
        _balance = Mathf.Max(0, value);
        RefreshTexts();
        OnBalanceChanged?.Invoke(_balance);
    }

    private void RefreshTexts()
    {
        if (_balanceTexts == null) return;

        string s = string.Format(_format, _balance);
        foreach (var t in _balanceTexts)
            if (t) t.text = s;
    }

    public void RegisterText(TMP_Text text)
    {
        if (!text) return;

        var list = new List<TMP_Text>(_balanceTexts ?? Array.Empty<TMP_Text>());
        if (!list.Contains(text)) list.Add(text);
        _balanceTexts = list.ToArray();

        RefreshTexts();
    }
}
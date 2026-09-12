using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerProfileService : MonoBehaviour
{
    public static PlayerProfileService Instance { get; private set; }

    [Header("Stan")]
    [SerializeField] private string _playerName = "";
    [SerializeField] private string _fallbackName = "Gracz";

    [Header("Wyświetlanie")]
    [SerializeField] private TMP_Text[] _nameTexts;
    [SerializeField] private string _format = "{0}";

    public event Action<string> OnNameChanged;

    public string PlayerName => string.IsNullOrWhiteSpace(_playerName) ? _fallbackName : _playerName;
    public bool HasName => !string.IsNullOrWhiteSpace(_playerName);

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        RefreshTexts();
    }

    public void SetName(string value)
    {
        _playerName = (value ?? "").Trim();
        RefreshTexts();
        OnNameChanged?.Invoke(PlayerName);
    }

    private void RefreshTexts()
    {
        if (_nameTexts == null) return;

        string s = string.Format(_format, PlayerName);
        foreach (var t in _nameTexts)
            if (t) t.text = s;
    }

    public void RegisterText(TMP_Text text)
    {
        if (!text) return;

        var list = new List<TMP_Text>(_nameTexts ?? Array.Empty<TMP_Text>());
        if (!list.Contains(text)) list.Add(text);
        _nameTexts = list.ToArray();

        RefreshTexts();
    }
}
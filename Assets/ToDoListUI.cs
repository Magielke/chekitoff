using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TodoListUI : MonoBehaviour
{
    [Header("Prefab i kontener")]
    [SerializeField] private TodoItemUI _itemPrefab;      // prefab z Project, nie obiekt ze sceny
    [SerializeField] private RectTransform _content;      // Content ze sceny

    [Header("Przyciski")]
    [SerializeField] private RectTransform _addButtonRow; // ButtonAdd - cały wiersz do przesuwania
    [SerializeField] private Button _addButton;           // Button w środku ButtonAdd
    [SerializeField] private Button _clearDoneButton;
    [SerializeField] private Button _clearAllButton;

    [Header("Limity")]
    [SerializeField] private int _maxItems = 9;

    private readonly List<TodoItemUI> _items = new List<TodoItemUI>();

    private void Awake()
    {
        if (_content == null || !_content.gameObject.scene.IsValid())
        {
            Debug.LogError("TodoListUI: _content musi wskazywać obiekt ze sceny, nie asset.", this);
            enabled = false;
            return;
        }

        if (_itemPrefab != null && _itemPrefab.gameObject.scene.IsValid())
            Debug.LogWarning("TodoListUI: _itemPrefab wskazuje obiekt ze sceny. Zrób z niego prefab i usuń ze sceny.", this);

        if (_addButton)       _addButton.onClick.AddListener(() => AddItem("", true));
        if (_clearDoneButton) _clearDoneButton.onClick.AddListener(ClearDone);
        if (_clearAllButton)  _clearAllButton.onClick.AddListener(ClearAll);

        MoveAddButtonToBottom();
    }

    public TodoItemUI AddItem(string label, bool startEditing)
    {
        if (_items.Count >= _maxItems) return null;

        var item = Instantiate(_itemPrefab, _content);
        item.OnDeleteRequested += RemoveItem;

        _items.Add(item);
        item.Setup(label, false, startEditing);

        MoveAddButtonToBottom();
        return item;
    }

    private void MoveAddButtonToBottom()
    {
        if (_addButtonRow && _addButtonRow.parent == _content)
            _addButtonRow.SetAsLastSibling();
    }

    private void RemoveItem(TodoItemUI item)
    {
        if (item == null) return;

        item.OnDeleteRequested -= RemoveItem;
        _items.Remove(item);
        Destroy(item.gameObject);
    }

    private void ClearDone()
    {
        for (int i = _items.Count - 1; i >= 0; i--)
            if (_items[i].Done) RemoveItem(_items[i]);
    }

    private void ClearAll()
    {
        for (int i = _items.Count - 1; i >= 0; i--)
            RemoveItem(_items[i]);
    }

    public int CountTotal => _items.Count;

    public int CountDone
    {
        get
        {
            int n = 0;
            foreach (var i in _items) if (i.Done) n++;
            return n;
        }
    }
}
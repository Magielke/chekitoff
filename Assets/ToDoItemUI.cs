using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TodoItemUI : MonoBehaviour
{
    /// <summary>True gdy dowolny element jest w trybie edycji - blokuje input gry.</summary>
    public static bool AnyEditing { get; private set; }

    [Header("Checkbox")]
    [SerializeField] private Button _checkboxButton;
    [SerializeField] private GameObject _checkedBox;      // BUTTON_Checkbox_checked
    [SerializeField] private GameObject _uncheckedBox;    // BUTTON_Checkbox

    [Header("Tekst")]
    [SerializeField] private TMP_Text _textNormal;        // TEXT_unchecked
    [SerializeField] private TMP_Text _textChecked;       // TEXT_checked
    [SerializeField] private TMP_InputField _editField;
    [SerializeField] private bool _strikethroughTag = true;

    [Header("Pozostałe")]
    [SerializeField] private Button _deleteButton;
    [SerializeField] private Button _textButton;          // klikalny obszar tekstu

    public event Action<TodoItemUI> OnDeleteRequested;
    public event Action<TodoItemUI> OnEditFinished;

    private string _label = "";
    private bool _done;
    private bool _editing;

    public string Label => _label;
    public bool Done => _done;

    private void Awake()
    {
        if (_checkboxButton) _checkboxButton.onClick.AddListener(ToggleDone);
        if (_deleteButton)   _deleteButton.onClick.AddListener(() => OnDeleteRequested?.Invoke(this));
        if (_textButton)     _textButton.onClick.AddListener(BeginEdit);

        if (_editField)
        {
            _editField.onSubmit.AddListener(_ => CommitEdit());
            _editField.onDeselect.AddListener(_ => CommitEdit());
        }
    }

    private void OnDisable()
    {
        if (_editing)
        {
            _editing = false;
            AnyEditing = false;
        }
    }

    public void Setup(string label, bool done, bool startEditing)
    {
        _label = label;
        _done = done;
        RefreshVisuals();

        if (startEditing) BeginEdit();
    }

    // ---------- stan ----------

    private void ToggleDone()
    {
        if (_editing) return;
        _done = !_done;
        RefreshVisuals();
    }

    public void SetDone(bool done)
    {
        _done = done;
        RefreshVisuals();
    }

    // ---------- edycja ----------

    public void BeginEdit()
    {
        if (_editing) return;
        _editing = true;
        AnyEditing = true;

        if (_textNormal)  _textNormal.gameObject.SetActive(false);
        if (_textChecked) _textChecked.gameObject.SetActive(false);

        if (_editField)
        {
            _editField.gameObject.SetActive(true);
            _editField.text = _label;
            _editField.ActivateInputField();
            _editField.Select();
        }
    }

    private void CommitEdit()
    {
        if (!_editing) return;
        _editing = false;
        AnyEditing = false;

        string v = _editField ? _editField.text.Trim() : _label;

        if (string.IsNullOrEmpty(v))
        {
            OnDeleteRequested?.Invoke(this);
            return;
        }

        _label = v;
        RefreshVisuals();
        OnEditFinished?.Invoke(this);
    }

    private void CancelEdit()
    {
        if (!_editing) return;
        _editing = false;
        AnyEditing = false;

        if (string.IsNullOrEmpty(_label)) OnDeleteRequested?.Invoke(this);
        else RefreshVisuals();
    }

    private void Update()
    {
        if (_editing && Input.GetKeyDown(KeyCode.Escape)) CancelEdit();
    }

    // ---------- wygląd ----------

    private void RefreshVisuals()
    {
        if (_checkedBox)   _checkedBox.SetActive(_done);
        if (_uncheckedBox) _uncheckedBox.SetActive(!_done);

        if (_textNormal)
        {
            _textNormal.gameObject.SetActive(!_done);
            _textNormal.text = _label;
        }

        if (_textChecked)
        {
            _textChecked.gameObject.SetActive(_done);
            _textChecked.text = _strikethroughTag ? $"<s>{Escape(_label)}</s>" : _label;
        }

        if (_editField) _editField.gameObject.SetActive(false);
    }

    private static string Escape(string s) => s.Replace("<", "<noparse><</noparse>");
}
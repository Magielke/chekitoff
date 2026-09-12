using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Cinemachine;

public class NamePromptPopup : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    [SerializeField] private PlayerProfileService _profile;
    [SerializeField] private PanelReveal _panel;

    [Header("Pola")]
    [SerializeField] private TMP_InputField _nameInput;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private TMP_Text _warningText;

    [Header("Walidacja")]
    [SerializeField] private int _minLength = 1;
    [SerializeField] private int _maxLength = 16;
    [SerializeField] private string _warningEmpty = "Wpisz imię";
    [SerializeField] private string _warningTooLong = "Za długie imię";

    [Header("Blokada gry")]
    [Tooltip("Skrypty ruchu gracza.")]
    [SerializeField] private Behaviour[] _movementScripts;
    [Tooltip("CinemachineInputAxisController z kamer - te same co w DeskWorkstation.")]
    [SerializeField] private Behaviour[] _cameraControls;
    [Tooltip("CinemachineBrain z Main Camera - zamraża kamerę całkowicie.")]
    [SerializeField] private CinemachineBrain _brain;

    private bool _open;

    private void Awake()
    {
        if (_confirmButton) _confirmButton.onClick.AddListener(Confirm);

        if (_nameInput)
        {
            _nameInput.characterLimit = _maxLength;
            _nameInput.onSubmit.AddListener(_ => Confirm());
        }

        if (_warningText) _warningText.text = "";
    }

    private void Start()
    {
        IsOpen = false;
        if (_panel) _panel.Hide();

        if (_profile && _profile.HasName) return;
        Open();
    }

    private void OnDisable()
    {
        if (_open) { _open = false; IsOpen = false; }
    }

    private void Open()
    {
        _open = true;
        IsOpen = true;

        SetGameInput(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (_panel) _panel.Show();

        if (_nameInput)
        {
            _nameInput.text = "";
            _nameInput.ActivateInputField();
            _nameInput.Select();
        }
    }

    private void Confirm()
    {
        if (!_open) return;

        string v = _nameInput ? _nameInput.text.Trim() : "";

        if (v.Length < _minLength)
        {
            if (_warningText) _warningText.text = _warningEmpty;
            if (_nameInput) _nameInput.ActivateInputField();
            return;
        }

        if (v.Length > _maxLength)
        {
            if (_warningText) _warningText.text = _warningTooLong;
            return;
        }

        if (_profile) _profile.SetName(v);
        Close();
    }

    private void Close()
    {
        _open = false;
        IsOpen = false;

        if (_panel) _panel.Hide();
        if (_warningText) _warningText.text = "";

        SetGameInput(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    private void LateUpdate()
    {
        if (!_open) return;

        if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
    private void SetGameInput(bool enabled)
    {
        if (_brain) _brain.enabled = enabled;

        if (_movementScripts != null)
            foreach (var b in _movementScripts) if (b) b.enabled = enabled;

        if (_cameraControls != null)
            foreach (var b in _cameraControls) if (b) b.enabled = enabled;
    }
}
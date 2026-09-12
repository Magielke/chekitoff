using UnityEngine;

public class InteractionPrompt : MonoBehaviour
{
    [Header("Widok")]
    [SerializeField] private SpriteRenderer _sprite;
    [SerializeField] private bool _billboard = true;
    [SerializeField] private bool _lockVertical = true;

    [Header("Unoszenie")]
    [SerializeField] private float _bobAmplitude = 0.08f;
    [SerializeField] private float _bobSpeed = 1.5f;

    [Header("Znikanie")]
    [SerializeField] private DeskWorkstation _desk;
    [SerializeField] private float _fadeDuration = 0.25f;

    private Camera _cam;
    private float _bobTime;
    private float _appliedOffset;     // ile już dodaliśmy do pozycji
    private float _alpha = 1f;
    private bool _consumed;

    public bool IsConsumed => _consumed;

    private void Awake()
    {
        if (!_sprite) _sprite = GetComponentInChildren<SpriteRenderer>();

        _alpha = 1f;
        ApplyAlpha();
    }

    private void OnDisable()
    {
        ResetOffset();      // oddaj to, co dodałeś
    }

    private void LateUpdate()
    {
        if (_cam == null) _cam = Camera.main;

        if (!_consumed && _desk && (_desk.IsSeated || _desk.IsBusy))
            _consumed = true;

        if (_consumed)
        {
            if (_alpha > 0f)
            {
                _alpha = Mathf.MoveTowards(_alpha, 0f, Time.deltaTime / Mathf.Max(0.01f, _fadeDuration));
                ApplyAlpha();
            }
            else
            {
                ResetOffset();
                enabled = false;
            }
            return;
        }

        UpdateBob();
        UpdateBillboard();
    }

    /// <summary>Dodaje tylko przyrost względem poprzedniej klatki - nigdy nie nadpisuje pozycji.</summary>
    private void UpdateBob()
    {
        if (_bobAmplitude <= 0f) return;

        _bobTime += Time.deltaTime;
        float wanted = Mathf.Sin(_bobTime * _bobSpeed) * _bobAmplitude;
        float delta = wanted - _appliedOffset;

        if (Mathf.Abs(delta) > 0.00001f)
        {
            transform.position += Vector3.up * delta;
            _appliedOffset = wanted;
        }
    }

    /// <summary>Cofa całe dotychczasowe przesunięcie, zostawiając pozycję wyjściową.</summary>
    private void ResetOffset()
    {
        if (Mathf.Abs(_appliedOffset) < 0.00001f) return;

        transform.position -= Vector3.up * _appliedOffset;
        _appliedOffset = 0f;
    }

    private void UpdateBillboard()
    {
        if (!_billboard || _cam == null || !_sprite) return;

        Vector3 dir = _sprite.transform.position - _cam.transform.position;
        if (_lockVertical) dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        _sprite.transform.rotation = Quaternion.LookRotation(dir);
    }

    private void ApplyAlpha()
    {
        if (!_sprite) return;

        Color c = _sprite.color;
        c.a = _alpha;
        _sprite.color = c;
        _sprite.enabled = _alpha > 0.001f;
    }

    public void ResetPrompt()
    {
        _consumed = false;
        _alpha = 1f;
        _bobTime = 0f;
        enabled = true;
        ApplyAlpha();
    }
}
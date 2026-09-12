using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class DeskWorkstation : MonoBehaviour
{
    [Header("Gracz")]
    [SerializeField] private Animator _playerAnimator;
    [SerializeField] private string _sitBoolParam = "IsSitting";
    [SerializeField] private Transform _playerRoot;
    [SerializeField] private Transform _seatAnchor;
    [SerializeField] private bool _snapToSeat = true;
    [SerializeField] private CharacterController _playerController;

    [Header("Sterowanie wyłączane na czas siedzenia")]
    [SerializeField] private Behaviour[] _movementScripts;
    [SerializeField] private Behaviour[] _cameraControls;

    [Header("Kamery")]
    [SerializeField] private CinemachineCamera _gameplayCamera;
    [SerializeField] private CinemachineCamera _deskCamera;
    [SerializeField] private int _activePriority = 20;
    [SerializeField] private int _inactivePriority = 0;

    [Header("Czasy animacji (s)")]
    [SerializeField] private float _sitDuration = 1.0f;
    [SerializeField] private float _standDuration = 1.0f;
    [SerializeField] private float _snapDuration = 0.25f;

    [Header("Timer i UI")]
    [SerializeField] private timer_service _timer;
    [SerializeField] private PanelReveal _timerPanel;
    [SerializeField] private FocusRewardPopup _rewardPopup;
    [Tooltip("Czy wstanie od biurka przyznaje nagrodę za przepracowany czas.")]
    [SerializeField] private bool _rewardOnStandUp = true;

    private bool _seated;
    private bool _busy;
    private bool _movedToSeat;
    private Vector3 _returnPos;
    private Quaternion _returnRot;
    private bool _hadRootMotion;

    public bool IsSeated => _seated;
    public bool IsBusy => _busy;

    private void Start()
    {
        if (_timerPanel) _timerPanel.Hide();
    }

    public void Interact()
    {
        if (_busy) return;
        if (_seated) StandUp();
        else SitDown();
    }

    public void SitDown()
    {
        if (_busy || _seated) return;
        StartCoroutine(SitRoutine());
    }

    public void StandUp()
    {
        if (_busy || !_seated) return;
        StartCoroutine(StandRoutine());
    }

    private IEnumerator SitRoutine()
    {
        _busy = true;

        SetControls(false);
        SetCursor(false);
        SwitchCamera(true);

        yield return null;

        _movedToSeat = false;
        if (_snapToSeat && _seatAnchor && _playerRoot)
        {
            _returnPos = _playerRoot.position;
            _returnRot = _playerRoot.rotation;
            _movedToSeat = true;
            yield return MoveTo(_playerRoot, _seatAnchor.position, _seatAnchor.rotation,
                                _snapDuration, false);
        }

        if (_playerAnimator)
        {
            _hadRootMotion = _playerAnimator.applyRootMotion;
            _playerAnimator.applyRootMotion = false;
        }

        SetAnimatorSitting(true);
        yield return new WaitForSeconds(_sitDuration);

        _seated = true;
        _busy = false;

        SetCursor(true);
        if (_timerPanel) _timerPanel.Show();
    }

    private IEnumerator StandRoutine()
    {
        _busy = true;

        if (_timerPanel) _timerPanel.Hide();

        if (_timer)
        {
            if (!_rewardOnStandUp && _rewardPopup) _rewardPopup.SuppressNext();
            _timer.Stop();
        }

        SetCursor(false);
        SetAnimatorSitting(false);
        yield return new WaitForSeconds(_standDuration);

        if (_movedToSeat && _playerRoot)
        {
            yield return MoveTo(_playerRoot, _returnPos, _returnRot, _snapDuration, false);

            if (_playerController)
            {
                _playerController.enabled = true;
                yield return null;
                Vector3 p = _playerRoot.position;
                p.y = _returnPos.y;
                _playerRoot.position = p;
            }

            _movedToSeat = false;
        }

        if (_playerController && !_playerController.enabled)
            _playerController.enabled = true;

        if (_playerAnimator) _playerAnimator.applyRootMotion = _hadRootMotion;

        SwitchCamera(false);
        SetControls(true);

        _seated = false;
        _busy = false;

        SetCursor(false);
    }

    private void SetControls(bool enabled)
    {
        if (_movementScripts != null)
            foreach (var b in _movementScripts) if (b) b.enabled = enabled;

        if (_cameraControls != null)
            foreach (var b in _cameraControls) if (b) b.enabled = enabled;
    }

    private void SwitchCamera(bool desk)
    {
        if (_gameplayCamera) _gameplayCamera.Priority = desk ? _inactivePriority : _activePriority;
        if (_deskCamera)     _deskCamera.Priority     = desk ? _activePriority   : _inactivePriority;
    }

    private void SetCursor(bool free)
    {
        Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = free;
    }

    private void SetAnimatorSitting(bool sitting)
    {
        if (_playerAnimator && !string.IsNullOrEmpty(_sitBoolParam))
            _playerAnimator.SetBool(_sitBoolParam, sitting);
    }

    private IEnumerator MoveTo(Transform t, Vector3 pos, Quaternion rot,
                               float duration, bool restoreController)
    {
        bool had = _playerController && _playerController.enabled;
        if (had) _playerController.enabled = false;

        Vector3 p0 = t.position;
        Quaternion r0 = t.rotation;

        for (float e = 0f; e < duration; e += Time.deltaTime)
        {
            float k = e / duration;
            t.position = Vector3.Lerp(p0, pos, k);
            t.rotation = Quaternion.Slerp(r0, rot, k);
            yield return null;
        }

        t.position = pos;
        t.rotation = rot;

        if (had && restoreController) _playerController.enabled = true;
    }
}
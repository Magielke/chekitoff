using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class PanelReveal : MonoBehaviour
{
    public enum Mode { Fade, SlideFromBottom, SlideFromTop, SlideFromLeft, SlideFromRight }

    [SerializeField] private Mode _mode = Mode.SlideFromBottom;
    [SerializeField] private float _duration = 0.35f;
    [SerializeField] private float _slideDistance = 300f;
    [SerializeField] private bool _hiddenOnStart = true;

    private CanvasGroup _group;
    private RectTransform _rect;
    private Vector2 _shownPos;
    private Coroutine _routine;

    private void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        _rect = GetComponent<RectTransform>();
        _shownPos = _rect.anchoredPosition;

        if (_hiddenOnStart) ApplyState(0f);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        Restart(FadeRoutine(1f));
    }

    public void Hide()
    {
        if (!gameObject.activeInHierarchy) { ApplyState(0f); return; }
        Restart(FadeRoutine(0f));
    }

    private void Restart(IEnumerator r)
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(r);
    }

    private IEnumerator FadeRoutine(float target)
    {
        float from = _group.alpha;
        for (float e = 0f; e < _duration; e += Time.deltaTime)
        {
            ApplyState(Mathf.SmoothStep(from, target, e / _duration));
            yield return null;
        }
        ApplyState(target);
        _routine = null;
    }

    private void ApplyState(float t)
    {
        _group.alpha = t;
        _group.interactable = t > 0.99f;
        _group.blocksRaycasts = t > 0.99f;

        if (_mode == Mode.Fade) return;

        Vector2 offset = _mode switch
        {
            Mode.SlideFromBottom => Vector2.down * _slideDistance,
            Mode.SlideFromTop    => Vector2.up * _slideDistance,
            Mode.SlideFromLeft   => Vector2.left * _slideDistance,
            _                    => Vector2.right * _slideDistance
        };

        _rect.anchoredPosition = Vector2.Lerp(_shownPos + offset, _shownPos, t);
    }
}
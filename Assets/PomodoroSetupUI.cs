using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class PomodoroSetupUI : MonoBehaviour
{
    [SerializeField] private DeskWorkstation _desk;
    [SerializeField] private GameObject _panel;
    
    [SerializeField] private TMP_InputField _workInput;
    [SerializeField] private TMP_InputField _breakInput;
    [SerializeField] private Toggle _loopToggle;
    
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _cancelButton;

    [SerializeField] private float _fallbackWork = 25f;
    [SerializeField] private float _fallbackBreak = 5f;

    private void Awake()
    {
        if(_workInput) _workInput.text = _fallbackWork.ToString(CultureInfo.InvariantCulture);
        if(_breakInput) _breakInput.text = _fallbackBreak.ToString(CultureInfo.InvariantCulture);
        if(_confirmButton) _confirmButton.onClick.AddListener(Confirm);
        if(_cancelButton) _cancelButton.onClick.AddListener(()=>_desk.StandUp());
        if(_panel) _panel.SetActive(false);
    }
    
    public void Show() 
    {
        if(_panel) _panel.SetActive(true);
    }

    public void Hide()
    {
        if(_panel) _panel.SetActive(false);
    }

    private void Confirm()
    {
        float work = ParseOr(_workInput? _workInput.text:null, _fallbackWork);
        float brk = ParseOr(_breakInput? _breakInput.text:null, _fallbackBreak);
        bool loop = _loopToggle || _loopToggle.isOn;
        Hide();
        _desk.BeginTimer(work,brk,loop);
    }

    private static float ParseOr(string s, float fallback)
    {
        if (string.IsNullOrWhiteSpace(s)) return fallback;
        s = s.Replace(",", ".");
        return float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float result) && result >0f ? result : fallback;
    }
    


}
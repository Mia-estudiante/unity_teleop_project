using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;   // Image 사용에 필수
using TMPro;            // TextMeshProUGUI 사용에 필수

public class SpeechManager : MonoBehaviour
{
    [Header("UI 참조 — 인스펙터에서 반드시 연결")]
    [SerializeField] private TextMeshProUGUI resultText;   // 음성 결과가 표시되는 곳
    [SerializeField] private TextMeshProUGUI micLabel;     // MicButton 라벨
    [SerializeField] private Image           micIcon;      // MicButton 아이콘
    [SerializeField] private TextMeshProUGUI statusText;   // 상태 표시 텍스트
    [SerializeField] private Image           statusDot;    // 상태 표시 점

    [Header("설정")]
    [SerializeField] private string placeholder = "여기에 음성 인식 결과가 표시됩니다...";
    [SerializeField] private Color  idleColor   = Color.white;
    [SerializeField] private Color  activeColor = new Color(0.30f, 0.69f, 0.31f); // #4CAF50

#if UNITY_VISIONOS && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void Speech_Start();
    [DllImport("__Internal")] private static extern void Speech_Stop();
#else
    // 에디터에서는 네이티브 호출 대신 로그만 — UI 흐름 테스트용
    private static void Speech_Start() => Debug.Log("[Editor] Speech_Start");
    private static void Speech_Stop()  => Debug.Log("[Editor] Speech_Stop");
#endif

    private bool _isListening;

    private void Start()
    {
        if (resultText) resultText.text = placeholder;
        UpdateVisual();
    }

    // MicButton의 OnClick에 연결
    // public void Toggle()
    // {
    //     if (_isListening) { Speech_Stop();  _isListening = false; }
    //     else              { Speech_Start(); _isListening = true;  }
    //     UpdateVisual();
    // }

    // ClearButton의 OnClick에 연결
    // public void ClearText()
    // {
    //     if (resultText) resultText.text = placeholder;
    // }

    public void StartListening()
    {
        if (_isListening) return;
        Speech_Start();
        _isListening = true;
        UpdateVisual();
    }

    public void StopListening()
    {
        if (!_isListening) return;
        Speech_Stop();
        _isListening = false;
        UpdateVisual();
    }

    // ── Swift(SpeechBridge)에서 UnitySendMessage로 호출되는 콜백 ──
    public void OnPartialResult(string text)
    {
        Debug.Log($"[SpeechManager] OnPartialResult called: '{text}'");
        if (resultText) resultText.text = text;
        else Debug.LogWarning("[SpeechManager] resultText is NULL!");
    }

    public void OnFinalResult(string text)
    {
        if (resultText) resultText.text = text;
        _isListening = false;
        UpdateVisual();
    }

    public void OnError(string err)
    {
        Debug.LogError($"[Speech] {err}");
        _isListening = false;
        UpdateVisual();
    }

    // 버튼/상태 표시 시각 갱신
    private void UpdateVisual()
    {
        if (micLabel)   micLabel.text   = _isListening ? "중지" : "말하기";
        if (micIcon)    micIcon.color   = _isListening ? activeColor : idleColor;
        if (statusText) statusText.text = _isListening ? "음성 인식 중..." : "음성 인식 대기 중";
        if (statusDot)  statusDot.color = _isListening ? activeColor : new Color(0.5f, 0.5f, 0.5f);
    }
}
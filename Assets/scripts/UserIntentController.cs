using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UserIntentController : MonoBehaviour
{
    public enum Mode { Idle, Voice, Pinch }

    [Header("Buttons")]
    [SerializeField] private Image voiceButtonBg;   // VoiceButton의 Image
    [SerializeField] private Image pinchButtonBg;   // PinchButton의 Image

    [Header("Result UI")]
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private string placeholder = "Loading to result..";

    [Header("Speech")]
    [SerializeField] private SpeechManager speechManager;

    [Header("Colors")]
    [SerializeField] private Color idleColor   = new Color32(0x2D, 0x2D, 0x2D, 0xFF);
    [SerializeField] private Color activeColor = new Color(0.85f, 0.25f, 0.25f); // 빨강
    
    [SerializeField] private QuadPinchDetector quadDetector;
    [SerializeField] private QuadVideoStream quadStream;   // 추가

    private Vector2 _lastPixelVec;   // 추가


    private Mode _mode = Mode.Idle;
    private string _lastPixel = "";

    private void Start() => ResetUI();

    // VoiceButton OnClick에 연결
    public void OnVoiceButtonClick()
    {
        Debug.Log("[UserIntent] Voice clicked!");   // ← 추가
        if (_mode == Mode.Pinch) return;   // Pinch 진행 중이면 무시

        if (_mode == Mode.Idle)
        {
            // 첫 번째: 인식 시작
            _mode = Mode.Voice;
            voiceButtonBg.color = activeColor;
            resultText.text = "";
            speechManager.StartListening();
        }
        else
        {
            // 두 번째: 인식 중단 + 송신
            speechManager.StopListening();

            string text = resultText != null ? resultText.text.Trim() : "";
            if (!string.IsNullOrEmpty(text) && quadStream != null)
            {
                quadStream.SendTextInit(text);
            }
            ResetUI();
        }
    }

    // PinchButton OnClick에 연결
    public void OnPinchButtonClick()
    {
        Debug.Log("[UserIntent] Pinch clicked!");   // ← 추가
        if (_mode == Mode.Voice) return;

        if (_mode == Mode.Idle)
        {
            _mode = Mode.Pinch;
            pinchButtonBg.color = activeColor;
            resultText.text = "";
            EnablePinchOnVideo(true);
        }
        else
        {
            // 두 번째 누름 → 전송 + 종료
            if (!string.IsNullOrEmpty(_lastPixel) && quadStream != null)
            {
                 Debug.Log($"[UserIntent] _lastPixelVec: {_lastPixelVec}");   // ← 추가
                quadStream.SendClickInit(_lastPixelVec);
            }
            EnablePinchOnVideo(false);
            ResetUI();   // 버튼 색 원복, resultText placeholder 복귀
        }
    }

    // VideoQuad에서 픽셀이 선택됐을 때 외부에서 호출
    public void OnPixelSelected(Vector2 pixel)
    {
        if (_mode != Mode.Pinch) return;

        int px = Mathf.RoundToInt(pixel.x);
        int py = Mathf.RoundToInt(pixel.y);

        _lastPixelVec = new Vector2(px, py);    // 정수값으로 저장
        _lastPixel = $"({px}, {py})";           // 표시도 같은 정수
        resultText.text = _lastPixel;
    }

    // ─── 외부 시스템과 연결되는 자리 (현재는 stub) ─────────
    private void EnablePinchOnVideo(bool enabled)
    {
        // TODO: VideoQuad의 핀치 입력 컴포넌트 켜고/끄기
        Debug.Log($"[Pinch] enabled={enabled}");
        if (quadDetector) quadDetector.SetMode(enabled);
    }

    private void SendFrameAndPayload(string kind, string payload)
    {
        // TODO: WebSocketClient로 현재 비디오 프레임 + payload 송신
        Debug.Log($"[Send] kind={kind}, payload='{payload}' (+ frame)");
    }
    // ────────────────────────────────────────────────────

    private void ResetUI()
    {
        _mode = Mode.Idle;
        _lastPixel = "";
        if (voiceButtonBg) voiceButtonBg.color = idleColor;
        if (pinchButtonBg) pinchButtonBg.color = idleColor;
        if (resultText)    resultText.text   = placeholder;
    }
}
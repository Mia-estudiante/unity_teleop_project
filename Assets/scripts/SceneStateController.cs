using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

public class SceneStateController : MonoBehaviour
{
    ROSConnection ros;
    private string triggerTopicName = "/avp/trigger";
    public static SceneStateController Instance { get; private set; }

    [Header("Toggle targets")]
    [SerializeField] private GameObject teleopRoot;  // RobotRoot + Quad + Sphere 모두 포함됨

    [Header("Start button visual")]
    [SerializeField] private Image startButtonBg;
    [SerializeField] private Color idleColor   = new Color(0.30f, 0.69f, 0.31f);
    [SerializeField] private Color activeColor = new Color(0.83f, 0.18f, 0.18f);

    [Header("Sphere triggers (양손 sphere)")]
    [SerializeField] private SphereTrigger[] palmTriggers;

    [Header("Streaming (선택)")]
    [SerializeField] private QuadVideoStream quadStream;

    public bool IsTeleopVisible { get; private set; }  // visuals 표시 중
    public bool IsTeleopRunning { get; private set; }  // 양손 모두 잡힘 → mujoco RUN

    private bool _prevAllTriggered;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }
    }

    private void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<BoolMsg>(triggerTopicName);
        ApplyVisibility(false);   // 초기: 메뉴만
    }

    private void Update()
    {
        if (!IsTeleopVisible) return;
        if (palmTriggers == null || palmTriggers.Length == 0) return;

        // 모든 sphere가 잡힌 상태인지
        bool allTriggered = true;
        foreach (var t in palmTriggers)
            // Debug.Log($"palmTriggers t: {t}");
            if (t == null || !t.isTriggered) { allTriggered = false; break; }
        
        BoolMsg msg = new BoolMsg(allTriggered);   
        ros.Publish(triggerTopicName, msg);
        Debug.Log($"[SCENE TRIGGER] {msg}");

        // 상태 전이 (Edge 감지)
        if (allTriggered && !_prevAllTriggered)
        {
            // Preparing/Paused → Active
            IsTeleopRunning = true;
            OnTeleopRun();
        }
        else if (!allTriggered && _prevAllTriggered)
        {
            // Active → Paused (palm-up reset 또는 손 빼냄)
            IsTeleopRunning = false;
            OnTeleopPause();
        }
        _prevAllTriggered = allTriggered;
    }

    // Start 버튼 OnClick에 연결
    public void ToggleTeleop()
    {
        ApplyVisibility(!IsTeleopVisible);
    }

    private void ApplyVisibility(bool show)
    {
        IsTeleopVisible = show;
        if (teleopRoot != null) teleopRoot.SetActive(show);

        if (startButtonBg != null) startButtonBg.color = show ? activeColor : idleColor;

        if (!show)
        {
            if (IsTeleopRunning)
            {
                IsTeleopRunning = false;
                OnTeleopPause();         // 강제 스트리밍 정지
            }
            foreach (var t in palmTriggers)
                if (t != null) t.ResetTrigger();
            _prevAllTriggered = false;
        }
    }

    // ─── 텔레옵 RUN/PAUSE 훅 (mujoco + streaming 연결 위치) ───
    private void OnTeleopRun()
    {
        Debug.Log("[Teleop] RUN");
        quadStream?.ResumeReceiving();   // 스트리밍 재개
        // TODO: mujoco 시작
    }

    private void OnTeleopPause()
    {
        Debug.Log("[Teleop] PAUSE");
        quadStream?.PauseReceiving();    // 스트리밍 정지 (마지막 프레임에 freeze)
        // TODO: mujoco 정지
    }
}
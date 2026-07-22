using UnityEngine;

public class SphereTrigger : MonoBehaviour
{
    public Renderer rend;

    public Color defaultColor;
    public Color triggerColor = Color.green;

    public bool isTriggered = false;

    private bool isInside = false;
    private float enterTime;
    private float exitTime;

    public float enterThreshold = 3.0f;
    public float exitDelay = 2.0f;

    public Transform handTarget; // 🔥 따라갈 hand (R_Palm 넣기)
    private Vector3 initialPosition;

    private float palmUpStartTime;
    private bool isPalmUp = false;

    public float palmUpThreshold = 0.8f;
    public float palmUpHoldTime = 3.0f;
    private bool _initialized = false;

    // 기존 Start 대신 Awake로 변경!
    void Awake()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            defaultColor = rend.material.color;
        }
        
        // 씬이 켜지자마자 부모가 꺼지기 전에 '진짜 월드 좌표'를 먼저 기억합니다.
        initialPosition = transform.position;   
        Debug.Log($"[SphereTrigger Awake] {gameObject.name} initialPosition: {initialPosition}");
        _initialized = true;
    }

    void OnEnable()
    {
        // Start 이후 재활성화(re-press Start) 시에만 scene 위치로 복귀
        if (!_initialized) return;
        transform.position = initialPosition;
        isTriggered = false;
        isInside    = false;
        isPalmUp    = false;
        palmUpStartTime = 0f;
        enterTime   = 0f;
        exitTime    = 0f;
        if (rend != null) rend.material.color = defaultColor;
    }

    void Update()
    {
        // 🔥 palm 방향 체크
        if (handTarget == null) return;
        else
        {
            float dot = Vector3.Dot(handTarget.up, Vector3.down);

            if (dot > palmUpThreshold)
            {
                if (!isPalmUp)
                {
                    isPalmUp = true;
                    palmUpStartTime = Time.time;
                }
            }
            else
            {
                isPalmUp = false;
            }
        }

        // 👉 Trigger 로직
        if (isInside)
        {
            if (!isTriggered && Time.time - enterTime >= enterThreshold)
            {
                isTriggered = true;
                rend.material.color = triggerColor;
            }
        }
        else
        {
            if (isTriggered && Time.time - exitTime >= exitDelay)
            {
                isTriggered = false;
                rend.material.color = defaultColor;
            }
        }

        // 🔥 reset 조건
        if (isTriggered && isPalmUp)
        {
            if (Time.time - palmUpStartTime >= palmUpHoldTime)
            {
                ResetSphere();
            }
        }
        // 🔥 핵심: triggered 상태일 때만 따라감
        if (isTriggered && handTarget != null)
        {
            transform.position = handTarget.position;
            transform.rotation = handTarget.rotation;
        }
    }

    // 외부에서 초기 위치를 설정할 때 호출
    public void SetHomePosition(Vector3 worldPos)
    {
        transform.position = worldPos;
        initialPosition    = worldPos;
        isTriggered = false;
        isInside    = false;
        isPalmUp    = false;
        enterTime   = 0f;
        exitTime    = 0f;
        if (rend != null) rend.material.color = defaultColor;
    }

    void ResetSphere()
    {
        isTriggered = false;
        isInside = false;
        isPalmUp = false;
        // transform.position = initialPosition;   // ← 주석 풀기 (선택)
        rend.material.color = defaultColor;
        Debug.Log("Reset by Palm Up Hold");
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Hand"))
        {
            isInside = true;
            enterTime = Time.time;
            Debug.Log($"Enter {other.tag}");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Hand"))
        {
            isInside = false;
            exitTime = Time.time;
            Debug.Log($"Exit {other.tag}");
        }
    }

    // SphereTrigger.cs 안에 추가
    public void ResetTrigger()
    {
        isTriggered = false;
        isInside = false;
        isPalmUp = false;
        palmUpStartTime = 0f;
        enterTime = 0f;
        exitTime = 0f;
        transform.position = initialPosition;   // ← 동일
        if (rend != null) rend.material.color = defaultColor;
    }
}
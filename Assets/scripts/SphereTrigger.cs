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

    void Start()
    {
        rend = GetComponent<Renderer>();
        defaultColor = rend.material.color;
        initialPosition = transform.position;
    }

    void Update()
    {
        // 🔥 palm 방향 체크
        if (handTarget != null)
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

    void ResetSphere()
    {
        isTriggered = false;
        isInside = false;
        isPalmUp = false;

        // transform.position = initialPosition;
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
}
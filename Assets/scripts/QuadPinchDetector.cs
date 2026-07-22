using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Unity.PolySpatial.InputDevices;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class QuadPinchDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Collider quadCollider;          // InteractiveQuad의 Collider
    [SerializeField] private Transform pinchMarker;          // 자식 PinchMarker
    [SerializeField] private UserIntentController userIntent;

    [Header("Frame size")]
    [SerializeField] private int frameWidth  = 640;
    [SerializeField] private int frameHeight = 480;

    [Header("Marker")]
    [SerializeField] private float surfaceOffset = 0.005f;
    [SerializeField] private float hitTolerance  = 0.05f;    // quad 표면 근처 허용 오차 (m)

    private bool _modeActive;

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    public void SetMode(bool active)
    {
        _modeActive = active;
        if (pinchMarker) pinchMarker.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!_modeActive) return;

        // 활성 touch 순회
        foreach (var touch in Touch.activeTouches)
        {
            var spatial = EnhancedSpatialPointerSupport.GetPointerState(touch);

            // 핀치 시작 시점만 잡음 (Began phase)
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                Vector3 worldPoint = spatial.interactionPosition;
                if (IsOnQuad(worldPoint))
                {
                    PlaceMarkerAt(worldPoint);
                    SendPixel(worldPoint);
                }
            }
        }
    }

    private bool IsOnQuad(Vector3 worldPoint)
    {
        // 월드 좌표 → quad의 로컬 좌표
        Vector3 local = transform.InverseTransformPoint(worldPoint);

        // Unity Quad mesh 로컬 범위: x,y ∈ [-0.5, 0.5], z ≈ 0
        if (Mathf.Abs(local.x) > 0.5f) return false;
        if (Mathf.Abs(local.y) > 0.5f) return false;
        if (Mathf.Abs(local.z) > hitTolerance) return false;
        return true;
    }

    private void PlaceMarkerAt(Vector3 worldPoint)
    {
        if (pinchMarker == null) return;
        Vector3 local = transform.InverseTransformPoint(worldPoint);
        pinchMarker.localPosition = new Vector3(local.x, local.y, -surfaceOffset);
        pinchMarker.localRotation = Quaternion.identity;
        pinchMarker.gameObject.SetActive(true);
    }

    private void SendPixel(Vector3 worldPoint)
    {
        Vector3 local = transform.InverseTransformPoint(worldPoint);
        Vector2 uv = new Vector2(local.x + 0.5f, local.y + 0.5f);
        uv.x = Mathf.Clamp01(uv.x);
        uv.y = Mathf.Clamp01(uv.y);

        Vector2 pixel = new Vector2(uv.x * frameWidth, (1f - uv.y) * frameHeight);
        userIntent?.OnPixelSelected(pixel);
    }
}
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Collider))]
public class QuadInteractionTarget : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private XRSimpleInteractable interactable;   // 이 quad에 붙은 컴포넌트
    [SerializeField] private Transform pinchMarker;               // 자식 빨간 원
    [SerializeField] private UserIntentController userIntent;

    [Header("Frame size (픽셀 좌표 변환용)")]
    [SerializeField] private int frameWidth  = 640;
    [SerializeField] private int frameHeight = 480;

    [Header("Marker 위치")]
    [SerializeField] private float surfaceOffset = 0.005f;        // quad 표면에서 살짝 앞

    private bool _modeActive;

    private void Awake()
    {
        if (interactable != null)
            interactable.selectEntered.AddListener(OnPinch);
        HideMarker();
    }

    private void OnDestroy()
    {
        if (interactable != null)
            interactable.selectEntered.RemoveListener(OnPinch);
    }

    // UserIntentController에서 호출 (Pinch 모드 진입/종료)
    public void SetMode(bool active)
    {
        _modeActive = active;
        HideMarker();   // 모드 시작/종료할 때마다 마커 초기화
    }

    private void OnPinch(SelectEnterEventArgs args)
    {
        Debug.Log("[Quad] OnPinch fired!");   // ← 추가
        if (!_modeActive) return;

        // 핀치 hit 위치 (월드 좌표)
        Vector3 worldPoint = args.interactorObject
            .GetAttachTransform(interactable).position;
        Debug.Log($"[Quad] mode active, world point={worldPoint}" );

        // 마커를 quad 로컬 좌표에서 표면 살짝 앞쪽으로 배치
        Vector3 local = transform.InverseTransformPoint(worldPoint);
        if (pinchMarker != null)
        {
            pinchMarker.localPosition = new Vector3(local.x, local.y, -surfaceOffset);
            pinchMarker.localRotation = Quaternion.identity;
            pinchMarker.gameObject.SetActive(true);
        }

        // 픽셀 좌표 계산 (기본 Unity Quad mesh 가정: local x,y ∈ [-0.5, 0.5])
        Vector2 uv = new Vector2(local.x + 0.5f, local.y + 0.5f);
        uv.x = Mathf.Clamp01(uv.x);
        uv.y = Mathf.Clamp01(uv.y);

        Vector2 pixel = new Vector2(uv.x * frameWidth, (1f - uv.y) * frameHeight);
        // (1 - uv.y) → 이미지 좌표는 Y가 위에서 아래로 가므로 뒤집음

        // ResultText에 픽셀 표시 + 마지막 픽셀 저장
        userIntent?.OnPixelSelected(pixel);
    }

    private void HideMarker()
    {
        if (pinchMarker != null) pinchMarker.gameObject.SetActive(false);
    }
}
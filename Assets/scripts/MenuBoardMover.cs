using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Unity.PolySpatial.InputDevices;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using Finger = UnityEngine.InputSystem.EnhancedTouch.Finger;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class MenuBoardMover : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform menuBoard;
    [SerializeField] private Collider grabCollider;
    [SerializeField] private Transform xrCamera; // XR Camera

    [Header("Hit detection")]
    [SerializeField] private float hitTolerance = 0.05f;

    [Header("Rotation")]
    [SerializeField] private float rotationSmooth = 8f;

    private bool _isGrabbed;
    private Finger _grabFinger;
    private Vector3 _grabOffset;

    private void OnEnable()  { EnhancedTouchSupport.Enable(); }
    private void OnDisable() { EnhancedTouchSupport.Disable(); }

    private void Update()
    {
        if (_isGrabbed) UpdateGrab();
        else CheckGrabStart();
    }

    private void CheckGrabStart()
    {
        foreach (var touch in Touch.activeTouches)
        {
            if (touch.phase != TouchPhase.Began) continue;

            var spatial = EnhancedSpatialPointerSupport.GetPointerState(touch);
            Vector3 pos = spatial.interactionPosition;

            if (IsOnGrabBar(pos))
            {
                _isGrabbed = true;
                _grabFinger = touch.finger;

                _grabOffset = menuBoard.position - pos;
                return;
            }
        }
    }

    private void UpdateGrab()
    {
        bool stillActive = false;

        foreach (var touch in Touch.activeTouches)
        {
            if (touch.finger != _grabFinger) continue;

            stillActive = true;

            var spatial = EnhancedSpatialPointerSupport.GetPointerState(touch);
            Vector3 pos = spatial.interactionPosition;

            if (touch.phase == TouchPhase.Ended ||
                touch.phase == TouchPhase.Canceled)
            {
                EndGrab();
                return;
            }

            // =========================
            // Position
            // =========================
            menuBoard.position = pos + _grabOffset;

            // =========================
            // Rotation
            // =========================

            // 메뉴판 -> 카메라 방향
            Vector3 lookDir = xrCamera.position - menuBoard.position;

            // y축만 사용해서 자연스럽게
            lookDir.y = 0f;

            if (lookDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot =
                    Quaternion.LookRotation(-lookDir);

                menuBoard.rotation = Quaternion.Slerp(
                    menuBoard.rotation,
                    targetRot,
                    Time.deltaTime * rotationSmooth
                );
            }

            return;
        }

        if (!stillActive)
            EndGrab();
    }

    private void EndGrab()
    {
        _isGrabbed = false;
        _grabFinger = null;
    }

    private bool IsOnGrabBar(Vector3 worldPoint)
    {
        if (grabCollider == null) return false;

        Vector3 closest = grabCollider.ClosestPoint(worldPoint);

        return Vector3.Distance(closest, worldPoint)
               <= hitTolerance;
    }
}
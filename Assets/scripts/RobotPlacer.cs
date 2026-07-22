using UnityEngine;

public class RobotPlacer : MonoBehaviour
{
    [SerializeField] private Transform robotRoot;

    public void PlaceAtUser(Transform head)
    {
        if (robotRoot == null || head == null) return;

        // 사용자 yaw (좌우 회전)만 추출
        Vector3 fwd = head.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward;
        fwd.Normalize();

        // 위치: 사용자 X·Z 그대로, Y는 바닥(0)
        Vector3 pos = new Vector3(head.position.x, 0f, head.position.z);

        robotRoot.position = pos;
        robotRoot.rotation = Quaternion.LookRotation(fwd, Vector3.up);
    }
}
using System.Collections;
using UnityEngine;

public class MenuBoardPlacer : MonoBehaviour
{
    [SerializeField] private Transform head;          // XR Origin의 Main Camera
    [SerializeField] private float distance = 0.7f;   // 머리 앞 거리 (m)
    [SerializeField] private float heightOffset = -0.15f;  // 머리 기준 높이 보정
    [SerializeField] private float startDelay = 0.5f; // 트래킹 안정화 대기

    private void Start()
    {
        StartCoroutine(PlaceRoutine());
    }

    private IEnumerator PlaceRoutine()
    {
        yield return new WaitForSeconds(startDelay);
        if (head == null) yield break;

        // 머리 forward의 수평 성분(yaw)만 사용
        Vector3 fwd = head.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
        fwd.Normalize();

        // 위치: 머리 앞 distance, 높이 살짝 아래
        Vector3 pos = head.position + fwd * distance;
        pos.y += heightOffset;
        transform.position = pos;

        // 회전: 메뉴 canvas 면이 사용자를 향하도록
        // (canvas content는 +Z 방향에서 보이는 게 일반적이라 +Z를 사용자 반대쪽으로)
        Vector3 awayFromUser = transform.position - head.position;
        awayFromUser.y = 0f;
        if (awayFromUser.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(awayFromUser.normalized, Vector3.up);
        }

        // 만약 빌드해보고 메뉴가 거꾸로 보이면 아래 줄로 교체:
        // transform.rotation = Quaternion.LookRotation(-awayFromUser.normalized, Vector3.up);
    }
}
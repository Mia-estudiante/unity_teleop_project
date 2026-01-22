using UnityEngine;
using Unity.XR.CoreUtils;

public class LockCameraPosition : MonoBehaviour
{
    public XROrigin xrOrigin;

    void LateUpdate()
    {
        Vector3 camLocal = xrOrigin.CameraInOriginSpacePos;
        xrOrigin.transform.position -= new Vector3(camLocal.x, 0, camLocal.z);
    }
}

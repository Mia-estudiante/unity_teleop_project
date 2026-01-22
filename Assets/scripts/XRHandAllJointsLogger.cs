using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry; // PoseMsg, PointMsg, QuaternionMsg
using UnityEngine.XR.Management;
using UnityEngine.XR.Hands;

public class XRHandAllJointsLogger : MonoBehaviour
{
    public XRHandSubsystem handSubsystem;

    ROSConnection ros;
    public string topicName1 = "/lwrist_pose";
    public string topicName2 = "/rwrist_pose";
    public Transform wristTransform; // Unity 내 손목 위치

    public float publishRateHz = 20f;
    float timeElapsed;

    void Start()
    {
        handSubsystem = XRGeneralSettings.Instance.Manager.activeLoader
            .GetLoadedSubsystem<XRHandSubsystem>();
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<PoseMsg>(topicName1);
        ros.RegisterPublisher<PoseMsg>(topicName2);
    }
    void Update()
    {
        // XRHandSubsystem이 할당되지 않았다면 자동으로 할당
        // if (handSubsystem == null)
        // {
        //     handSubsystem = XRGeneralSettings.Instance.Manager.activeLoader
        //     .GetLoadedSubsystem<XRHandSubsystem>();
        //     // var subsystems = new System.Collections.Generic.List<XRHandSubsystem>();
        //     // XRGeneralSettings.Instance.Manager.GetInstances(subsystems);
        //     // if (subsystems.Count > 0)
        //     //     handSubsystem = subsystems[0];
        //     // else
        //     //     return;
        // }

        // 왼손/오른손 모두 반복
        timeElapsed += Time.deltaTime;
        if (timeElapsed >= 1f / publishRateHz)
        {
            LogHandJoints(handSubsystem.leftHand, topicName1);
            LogHandJoints(handSubsystem.rightHand, topicName2);
            timeElapsed = 0f;
        }
    }

    void LogHandJoints(XRHand hand, string topicName)
    {
        // 손이 현재 트래킹되고 있는지 확인
        if (!hand.isTracked)
            return;

        XRHandJoint joint = hand.GetJoint(XRHandJointID.Wrist);
        if (joint.TryGetPose(out Pose pose))
        {
            // pose.position, pose.rotation 사용
            Vector3 pos = pose.position;
            Quaternion rot = pose.rotation;

            PoseMsg poseMsg = new PoseMsg(
                new PointMsg(pos.x, pos.y, pos.z),
                new QuaternionMsg(rot.x, rot.y, rot.z, rot.w)
            );

            ros.Publish(topicName, poseMsg);
            Debug.Log($"{topicName} Wrist Pose - Position: {pos}, Rotation: {rot.eulerAngles}");
        }
        // for (int i = 0; i < (int)XRHandJointID.EndMarker; i++)
        // {
        //     XRHandJointID jointID = (XRHandJointID)i;
        //     XRHandJoint joint = hand.GetJoint(jointID);
        //     if (joint.TryGetPose(out Pose pose))
        //     {
        //         Debug.Log($"{handName} Hand - {jointID}: Position={pose.position}, Rotation={pose.rotation.eulerAngles}");
        //     }
        // }

        // 모든 XRHandJointID 순회
        // 모든 XRHandJointID 순회 시 Invalid(-1) 제외
        // foreach (XRHandJointID jointID in System.Enum.GetValues(typeof(XRHandJointID)))
        // {
        //     // 유효한 관절 ID만 처리 (0 <= (int)jointID < 26)
        //     if ((int)jointID < 0 || (int)jointID >= (int)XRHandJointID.EndMarker)
        //         continue;

        //     XRHandJoint joint = hand.GetJoint(jointID);
        //     if (joint.TryGetPose(out Pose pose))
        //     {
        //         Debug.Log($"{handName} Hand - {jointID}: Position={pose.position}, Rotation={pose.rotation.eulerAngles}");
        //     }
        // // foreach (XRHandJointID jointID in System.Enum.GetValues(typeof(XRHandJointID)))
        // // {
        // //     XRHandJoint joint = hand.GetJoint(jointID);
        // //     if (joint.TryGetPose(out Pose pose))
        // //     {
        // //         Debug.Log($"{handName} Hand - {jointID}: Position={pose.position}, Rotation={pose.rotation.eulerAngles}");
        // //     }
        // }
    }
}

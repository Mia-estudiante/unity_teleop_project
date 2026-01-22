using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry; // PoseMsg, PointMsg, QuaternionMsg
// using Unity.Robotics.Core;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine.XR.Management;
using UnityEngine.XR.Hands;

public class HeadWristPosePublisher : MonoBehaviour
{
    ROSConnection ros;
    public XRHandSubsystem handSubsystem;
    public string leftWristTopicName = "/lwrist_pose";
    public string rightWristTopicName = "/rwrist_pose";
    public string headTopicName = "/head_pose";

    public Transform origin;
    public Transform LWristTransform; // Unity 내 왼 손목 위치
    public Transform RWristTransform; // Unity 내 오른 손목 위치
    public Transform cameraTransform;

    public float publishRateHz = 20f;
    float timeElapsed;

    public bool initFlag = true;
    public Vector3 referencePos;
    public Quaternion referenceOri;

    void Start()
    {
        handSubsystem = XRGeneralSettings.Instance.Manager.activeLoader
            .GetLoadedSubsystem<XRHandSubsystem>();
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<PoseMsg>(leftWristTopicName);
        ros.RegisterPublisher<PoseMsg>(rightWristTopicName);
        ros.RegisterPublisher<PoseMsg>(headTopicName);
    }

    void Update()
    {
        timeElapsed += Time.deltaTime;
        if (timeElapsed >= 1f / publishRateHz)
        {
            PublishWristPose(handSubsystem.leftHand, leftWristTopicName);
            PublishWristPose(handSubsystem.rightHand, rightWristTopicName);
            PublishHeadPose(headTopicName);
            timeElapsed = 0f;
        }
    }

    void PublishWristPose(XRHand hand, string topicName)
    {
        if (!hand.isTracked)
            return;

        XRHandJoint joint = hand.GetJoint(XRHandJointID.Wrist);

        if (joint.TryGetPose(out Pose wristJointPose))
        {
            Vector3 pos = wristJointPose.position;
            Quaternion rot = wristJointPose.rotation;
            pos.y += 1.5f;
            pos.z += 0.2f;

            // Unity->ROS
            Vector3Msg rosPosition = pos.To<FLU>();
            QuaternionMsg rosRotation = rot.To<FLU>();

            PoseMsg poseMsg = new PoseMsg(
                new PointMsg(rosPosition.x, rosPosition.y, rosPosition.z),
                new QuaternionMsg(rosRotation.x, rosRotation.y, rosRotation.z, rosRotation.w)
            );

            // PoseMsg poseMsg = new PoseMsg(
            //     new PointMsg(pos.x, pos.y, pos.z),
            //     new QuaternionMsg(rot.x, rot.y, rot.z, rot.w)
            // );

            ros.Publish(topicName, poseMsg);
            // Debug.Log($"{topicName} Pose - Position: {pos}, Rotation: {rot}");
        }
    }

    void PublishHeadPose(string topicName)
    {
        if (initFlag)
            referencePos = cameraTransform.position;
            initFlag = false;

        referencePos = cameraTransform.position;
        referenceOri = cameraTransform.rotation;

        // Unity->ROS
        Vector3Msg rosPosition = referencePos.To<FLU>();
        QuaternionMsg rosRotation = referenceOri.To<FLU>();

        PoseMsg poseMsg = new PoseMsg(
            new PointMsg(rosPosition.x, rosPosition.y, rosPosition.z),
            new QuaternionMsg(rosRotation.x, rosRotation.y, rosRotation.z, rosRotation.w)
        );

        // PoseMsg poseMsg = new PoseMsg(
        //     new PointMsg(pos.x, pos.y, pos.z),
        //     new QuaternionMsg(rot.x, rot.y, rot.z, rot.w)
        // );

        ros.Publish(topicName, poseMsg);
        // Debug.Log($"{topicName} Pose - Position: {referencePos}, Rotation: {referenceOri}");
    }
}

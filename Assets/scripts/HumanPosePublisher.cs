using UnityEngine;
using System.Collections.Generic;

using UnityEngine.XR.Hands;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Geometry;

public class HumanPosePublisher : MonoBehaviour
{
    ROSConnection ros;
    XRHandSubsystem m_HandSubsystem;
    public Transform cameraTransform;

    private string headTopicName = "/avp/head";
    private string leftWristTopicName = "/avp/lwrist";
    private string rightWristTopicName = "/avp/rwrist";
    private string leftFingersTopicName = "/avp/lfingers";
    private string rightFingersTopicName = "/avp/rfingers";
    private XRHandJointID[] tips = new XRHandJointID[]{
        XRHandJointID.ThumbTip, XRHandJointID.IndexTip,
        XRHandJointID.MiddleTip, XRHandJointID.RingTip, XRHandJointID.LittleTip
    };

    // === Diagnostics ===
    // private int headPublishCount = 0;
    // private int headSkipCount = 0;
    // private float lastDiagTime = 0f;

    void Start()
    {
        var handSubsystems = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(handSubsystems);
        for (var i = 0; i < handSubsystems.Count; ++i)
        {
            if (handSubsystems[i].running)
            {
                m_HandSubsystem = handSubsystems[i];
                break;
            }
        }

        // :star: Auto-assign cameraTransform if null
        // if (cameraTransform == null)
        // {
        //     if (Camera.main != null)
        //     {
        //         cameraTransform = Camera.main.transform;
        //         Debug.Log($"[HumanPosePublisher2] Auto-assigned cameraTransform to Camera.main: {cameraTransform.name}");
        //     }
        //     else
        //     {
        //         Debug.LogError("[HumanPosePublisher2] cameraTransform is null AND Camera.main not found! Head will not be published.");
        //     }
        // }
        // else
        // {
        //     Debug.Log($"[HumanPosePublisher2] cameraTransform assigned in Inspector: {cameraTransform.name}");
        // }

        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<PoseMsg>(headTopicName);
        ros.RegisterPublisher<PoseMsg>(leftWristTopicName);
        ros.RegisterPublisher<PoseMsg>(rightWristTopicName);
        ros.RegisterPublisher<PoseArrayMsg>(leftFingersTopicName);
        ros.RegisterPublisher<PoseArrayMsg>(rightFingersTopicName);
    }

    void Update()
    {
        PublishHeadPose(headTopicName);

        if (m_HandSubsystem != null)
        {
            PublishWristPose(m_HandSubsystem.leftHand,  leftWristTopicName);
            PublishWristPose(m_HandSubsystem.rightHand, rightWristTopicName);
            PublishFingersPose(m_HandSubsystem.leftHand,  leftFingersTopicName);
            PublishFingersPose(m_HandSubsystem.rightHand, rightFingersTopicName);
        }

        // === Diagnostics every 2 seconds ===
        // if (Time.time - lastDiagTime > 2f)
        // {
        //     float total = headPublishCount + headSkipCount;
        //     float ratio = total > 0 ? headSkipCount / total * 100f : 0f;
        //     Debug.Log($"[HumanPosePublisher2] head: {headPublishCount} pub, {headSkipCount} skip ({ratio:F1}% skipped) over last 2s");
        //     headPublishCount = 0;
        //     headSkipCount = 0;
        //     lastDiagTime = Time.time;
        // }
    }

    void PublishHeadPose(string topicName)
    {
        // :star: Guard against null camera
        if (cameraTransform == null)
        {
            // headSkipCount++;
            return;
        }

        try
        {
            Vector3 pos = cameraTransform.position;
            Quaternion rot = cameraTransform.rotation;

            Vector3Msg rosPos = pos.To<FLU>();
            QuaternionMsg rosRot = rot.To<FLU>();

            PoseMsg poseMsg = new PoseMsg(
                new PointMsg(rosPos.x, rosPos.y, rosPos.z),
                new QuaternionMsg(rosRot.x, rosRot.y, rosRot.z, rosRot.w)
            );
            ros.Publish(topicName, poseMsg);
            // headPublishCount++;
        }
        catch (System.Exception e)
        {
            // headSkipCount++;
            // Log only first occurrence to avoid spam
            // if (headSkipCount == 1)
            Debug.LogError($"[HumanPosePublisher] PublishHeadPose error: {e.Message}");
        }
    }

    void PublishWristPose(XRHand hand, string topicName)
    {
        if (!hand.isTracked) return;

        XRHandJoint joint = hand.GetJoint(XRHandJointID.Wrist);
        if (joint.TryGetPose(out Pose wristJointPose))
        {
            Vector3Msg rosPos = wristJointPose.position.To<FLU>();
            QuaternionMsg rosRot = wristJointPose.rotation.To<FLU>();
            PoseMsg poseMsg = new PoseMsg(
                new PointMsg(rosPos.x, rosPos.y, rosPos.z),
                new QuaternionMsg(rosRot.x, rosRot.y, rosRot.z, rosRot.w)
            );
            ros.Publish(topicName, poseMsg);
        }
    }

    void PublishFingersPose(XRHand hand, string topicName)
    {
        if (!hand.isTracked) return;

        PoseArrayMsg poseArray = new PoseArrayMsg();
        poseArray.poses = new PoseMsg[tips.Length];
        for (int i = 0; i < tips.Length; i++)
        {
            XRHandJoint joint = hand.GetJoint(tips[i]);
            if (joint.TryGetPose(out Pose jointPose))
            {
                Vector3Msg rosPos = jointPose.position.To<FLU>();
                QuaternionMsg rosRot = jointPose.rotation.To<FLU>();
                poseArray.poses[i] = new PoseMsg(
                    new PointMsg(rosPos.x, rosPos.y, rosPos.z),
                    new QuaternionMsg(rosRot.x, rosRot.y, rosRot.z, rosRot.w)
                );
            }
            else
            {
                // joint pose not available - fill with identity to keep array length consistent
                poseArray.poses[i] = new PoseMsg(
                    new PointMsg(0, 0, 0),
                    new QuaternionMsg(0, 0, 0, 1)
                );
            }
        }
        ros.Publish(topicName, poseArray);
    }
}
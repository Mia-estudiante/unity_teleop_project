using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using System.Collections;
using RosMessageTypes.Geometry; // PoseMsg, PointMsg, QuaternionMsg
using SensorUnity = RosMessageTypes.Sensor.JointStateMsg;
using UnityEngine.InputSystem;
using RosMessageTypes.Std;
using UnityEngine.XR.Management;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine.XR.Hands;
using System;
using Unity.VisualScripting.FullSerializer;


public class HandPosePublisher : MonoBehaviour
{
    ROSConnection ros;
    public XRHandSubsystem handSubsystem;

    // 업데이트 간격 제한
    private float updateInterval = 0.05f; // 50ms
    private float lastUpdateTime = 0f;

    // ROS PUBLISHER
    float timeElapsed;
    public float publishRateHz = 20f;

    void Start()
    {
        handSubsystem = XRGeneralSettings.Instance.Manager.activeLoader
            .GetLoadedSubsystem<XRHandSubsystem>();

        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<PoseArrayMsg>("/rci_hand_manager/left_landmarks");
        ros.RegisterPublisher<PoseArrayMsg>("/rci_hand_manager/right_landmarks");
    }

    // DEBUG
    // void Update()
    // {
    //     timeElapsed += Time.deltaTime;

    //     if (timeElapsed >= 1f / publishRateHz)
    //     {
    //         Float64Msg msg = new Float64Msg();
    //         msg.data = 10.0f;
    //         ros.Publish("/TestFloat", msg);

    //         timeElapsed = 0f;
    //     }
    // }

    void Update()
    {
        timeElapsed += Time.deltaTime;

        if (timeElapsed >= 1f / publishRateHz)
        {
            PublishHandPose(handSubsystem.leftHand, "/rci_hand_manager/left_landmarks");
            PublishHandPose(handSubsystem.rightHand, "/rci_hand_manager/right_landmarks");
            timeElapsed = 0f;
        }
    }

    /*
    참고
    - joint 개수는 총 29개가 나왔으나, enum value 기준으로는 총 28개(wrist 가 2번 찍힘)
    */
    void PublishHandPose(XRHand hand, string topicName)
    {
        if (!hand.isTracked)
            return;

        XRHandJointID[] allJointIDs = (XRHandJointID[])Enum.GetValues(typeof(XRHandJointID));
        // PoseMsg[] poseArrayMsg = new PoseMsg[25];
        PoseArrayMsg poseArray = new PoseArrayMsg();
        poseArray.header = new HeaderMsg();
        poseArray.header.frame_id = "";
        poseArray.poses = new PoseMsg[27]; // Wrist 1개 더 있음

        Debug.Log($"{topicName} allJointIDs.Length : {allJointIDs.Length}");
        int iter = 0;
        for (int i = 0; i < allJointIDs.Length; i++) // i: 0 ~ 28
        {
            XRHandJointID jointID = allJointIDs[i];

            int enum_val = (int)jointID;

            if (enum_val >= 1 && enum_val < 27) // enum_val: 1 ~ 26 + 1 한 번 더
            {
                XRHandJoint joint = hand.GetJoint(jointID);
                if (joint.TryGetPose(out Pose jointPose))
                {
                    // wristJointPose = wristJointPose.GetTransformedBy(xrOriginPose);
                    Vector3 pos = jointPose.position;
                    Quaternion rot = jointPose.rotation;
                    poseArray.header.frame_id += $"{i} ";

                    // Unity->ROS
                    Vector3Msg rosPosition = pos.To<FLU>();
                    QuaternionMsg rosRotation = rot.To<FLU>();

                    poseArray.poses[iter] = new PoseMsg(
                        new PointMsg(rosPosition.x, rosPosition.y, rosPosition.z),
                        new QuaternionMsg(rosRotation.x, rosRotation.y, rosRotation.z, rosRotation.w)
                    );
                    Debug.Log($"{topicName} Index: {i}, Joint ID: {jointID}, Enum Value: {(int)jointID}");
                }
                iter++;
            }
        }
        Debug.Log($"{poseArray.poses.Length}");
        ros.Publish(topicName, poseArray);
    }
}

/* Enum Val
0
1 Wrist
2 Palm
3 ThumbMetacarpal
4 ThumbProximal
5 ThumbDistal
6 ThumbTip
7 IndexMetacarpal
8 IndexProximal
9 IndexIntermediate
10 IndexDistal
11 IndexTip
12 MiddleMetacarpal
13 MiddleProximal
14 MiddleIntermediate
15 MiddleDistal
16 MiddleTip
17 RingMetacarpal
18 RingProximal
19 RingIntermediate
20 RingDistal
21 RingTip
22 LittleMetacarpal
23 LittleProximal
24 LittleIntermediate
25 LittleDistal
26 LittleTip
27
*/
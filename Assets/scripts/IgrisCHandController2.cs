using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using SensorUnity = RosMessageTypes.Sensor.JointStateMsg;
using RosMessageTypes.Sensor;
using UnityEngine.InputSystem;
using RosMessageTypes.Std;
using System;
using System.Linq;
using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
public class IgrisCHandController2 : MonoBehaviour
{
    ROSConnection ros;
    // private string ctlTopicName = "/mujoco/controller";
    private string handJointTopicName = "/unity/hand_joint_states";

    // 업데이트 간격 제한
    // private float updateInterval = 0.05f; // 50ms
    // private float lastUpdateTime = 0f;


    // ROS PUBLISHER
    // public string JointStateTopicName = "/mujoco/hand_joint_states";
    // float timeElapsed;
    // public float publishRateHz = 20f;
    // public ArticulationBody[] jointArticulations;

    public ArticulationBody[] leftHandJoints;
    public ArticulationBody[] rightHandJoints;
    public SphereManager manager;

    // private int[] leftTargetIndex = new int[] {0,2,4,6,9,8};
    // private int[] rightTargetIndex = new int[] {0,2,4,6,9,8};

    private SensorUnity latestMsg;
    private double[] lfingers_qpos = new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
    private double[] rfingers_qpos = new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
    private string[] lfingers_names = new string[11];
    private string[] rfingers_names = new string[11];
    private string[] hand_joint_names = new string[22];

    Dictionary<string, ArticulationBody> leftMap = new Dictionary<string, ArticulationBody>();
    Dictionary<string, ArticulationBody> rightMap = new Dictionary<string, ArticulationBody>();

    string ExtractKey(string name)
    {
        // 예: Left_Link_Thumb_Proximal
        var parts = name.Split('_');

        // 뒤 2개 붙이기
        return parts[parts.Length - 2] + "_" + parts[parts.Length - 1];
    }

    // articulate joint name
    void BuildJointMap()
    {
        foreach (var joint in leftHandJoints)
        {
            string key = ExtractKey(joint.name); // Thumb_Proximal 같은 것
            leftMap[key] = joint;
            // Debug.Log($"Left map: {key} -> {joint.name}");
        }

        foreach (var joint in rightHandJoints)
        {
            string key = ExtractKey(joint.name);
            rightMap[key] = joint;
            // Debug.Log($"Right map: {key} -> {joint.name}");
        }
    }

    string ExtractKeyFromROS(string name)
    {
        // 예: Left_1_Joint_Thumb_Proximal
        var parts = name.Split('_');

        return parts[parts.Length - 2] + "_" + parts[parts.Length - 1];
    }

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        // if (jointArticulations == null || jointArticulations.Length == 0)
        // {
        //     jointArticulations = GetComponentsInChildren<ArticulationBody>();
        // }
        ros.Subscribe<SensorUnity>(handJointTopicName, JointStateCallback);
        // ros.RegisterPublisher<Float64MultiArrayMsg>(handJointTopicName);
        BuildJointMap();
    }

    void FixedUpdate()
    {
        if (latestMsg == null) return;

        ApplyHand(latestMsg);
    }

    // little_middle, ring_middle, middle_middle, index_middle, thumb_middle, thumb_proximal
    void JointStateCallback(SensorUnity msg)
    {
        // 🔥 trigger 안되면 로봇 멈춤
        if (manager == null || !manager.bothTriggered)
            return;

        latestMsg = msg;
    
        // for (int i = 0; i < msg.name.Length; i++)
        // {
        //     string rosName = msg.name[i];
        //     double pos = msg.position[i];

        //     string key = ExtractKeyFromROS(rosName);

        //     if (rosName.StartsWith("Left_"))
        //     {
        //         if (leftMap.ContainsKey(key))
        //         {
        //             ApplyJoint(leftMap[key], pos, rosName);
        //         }
        //     }
        //     else if (rosName.StartsWith("Right_"))
        //     {
        //         if (rightMap.ContainsKey(key))
        //         {
        //             ApplyJoint(rightMap[key], pos, rosName);
        //         }
        //     }
        // }
    }

    void ApplyHand(SensorUnity msg)
    {
        for (int i = 0; i < msg.name.Length; i++)
        {
            string rosName = msg.name[i];
            double pos = msg.position[i];

            string key = ExtractKeyFromROS(rosName);

            if (rosName.StartsWith("Left_"))
            {
                if (leftMap.ContainsKey(key))
                    ApplyJoint(leftMap[key], pos, rosName);
            }
            else if (rosName.StartsWith("Right_"))
            {
                if (rightMap.ContainsKey(key))
                    ApplyJoint(rightMap[key], pos, rosName);
            }
        }
    }

    void ApplyJoint(ArticulationBody joint, double rad, string rosName)
    {
        var drive = joint.xDrive;
        drive.target = (float)(Mathf.Rad2Deg * rad);
        drive.stiffness = 3000f;
        drive.damping = 200f;
        drive.forceLimit = float.MaxValue;
        joint.xDrive = drive;
        Debug.Log($"joint name: {joint.name} -> {rosName}");
    }
}
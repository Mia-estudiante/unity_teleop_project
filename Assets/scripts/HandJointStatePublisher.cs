using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using SensorUnity = RosMessageTypes.Sensor.JointStateMsg;
using UnityEngine.InputSystem;
using RosMessageTypes.Std;
using System;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
// using SensorUnity = RosMessageTypes.Sensor.JointStateMsg;

public class HandJointStatePublisher : MonoBehaviour
{
    ROSConnection ros;

    // 업데이트 간격 제한
    private float updateInterval = 0.05f; // 50ms
    private float lastUpdateTime = 0f;


    // ROS PUBLISHER
    public string JointStateTopicName = "/rci_h12_manager/hand_joint_states";
    float timeElapsed;
    public float publishRateHz = 20f;
    public ArticulationBody[] jointArticulations;

    // Set Joint Order with Mimic 
    // from Thumb to  Pinky
    // Left 12 : Left hand
    // Right 12 : Right hand
    // Total Length : 24
    public int[] hand_joint_idx = new int[] { 45, 46, 47, 48, 43, 44, 41, 42, 39, 40, 37, 38, 22, 23, 24, 25, 20, 21, 18, 19, 16, 17, 14, 15 };
    int[] actuator_idx = new int[] { 10, 8, 6, 4, 1, 0, 22, 20, 18, 16, 13, 12 };
    double[] qpos = new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };


    /*
    45: L_thumb_proximal_base
    46: L_thumb_proximal
    47: L_thumb_intermediate
    48: L_thumb_distal
    43: L_index_proximal
    44: L_index_intermediate
    41: L_middle_proximal
    42: L_middle_intermediate
    39: L_ring_proximal
    40: L_ring_intermediate
    37: L_pinky_proximal
    38: L_pinky_intermediate

    22: R_thumb_proximal_base
    23: R_thumb_proximal
    24: R_thumb_intermediate
    25: R_thumb_distal
    20: R_index_proximal
    21: R_index_intermediate
    18: R_middle_proximal
    19: R_middle_intermediate
    16: R_ring_proximal
    17: R_ring_intermediate
    14: R_pinky_proximal
    15: R_pinky_intermediate
    */

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<SensorUnity>(JointStateTopicName);
        if (jointArticulations == null || jointArticulations.Length == 0)
        {
            jointArticulations = GetComponentsInChildren<ArticulationBody>();
        }
        // ros.Subscribe<SensorUnity>("/rci_h12_manager/l_hand_joint_pose", JointStateCallback_left);
        // ros.Subscribe<SensorUnity>("/rci_h12_manager/r_hand_joint_pose", JointStateCallback_right);
        ros.Subscribe<SensorUnity>("/rci_h12_manager/hand_joint_pose", JointStateCallback);

    }

    // TEST 250525
    void Update()
    {
        // for (int i = 0; i < jointArticulations.Length; i++)
        // {
        //     Debug.Log($"i{i}, name: {jointArticulations[i].name}");
        // }

        int iteration = 0;

        // Total Length : 24

        // qpos[0] = 0.0;
        // qpos[1] = 1.57;
        // qpos[4] = 1.57;
        // qpos[6] = 1.57;
        // qpos[8] = 0.0;
        // qpos[10] = 1.3;

        // qpos[12] = 1.12;
        // qpos[13] = 0.86;
        // qpos[16] = 1.18;
        // qpos[18] = 0.83;
        // qpos[20] = 0.0;
        // qpos[22] = 0.54;

        qpos[2] = qpos[1] * 1.6;
        qpos[3] = qpos[1] * 2.4;
        qpos[5] = qpos[4] * 1.0;
        qpos[7] = qpos[6] * 1.0;
        qpos[9] = qpos[8] * 1.0;
        qpos[11] = qpos[10] * 1.0;

        qpos[14] = qpos[13] * 1.6;
        qpos[15] = qpos[13] * 2.4;
        qpos[17] = qpos[16] * 1.0;
        qpos[19] = qpos[18] * 1.0;
        qpos[21] = qpos[20] * 1.0;
        qpos[23] = qpos[22] * 1.0;

        foreach (int idx in hand_joint_idx)
        {

            // int idx = joint_states_order[i];  // 내가 지정한 인덱스
            ArticulationBody joint = jointArticulations[idx];
            var drive = joint.xDrive;
            drive.target = (float)(Mathf.Rad2Deg * qpos[iteration]);
            // 라디안 -> 각도
            drive.stiffness = 600f; // 목표값에 얼마나 강하게 끌어당길지
            drive.damping = 30f;    // 진동이나 떨림을 얼마나 줄일지
            drive.forceLimit = 200f; // 최대 적용 가능한 힘      
            joint.xDrive = drive;
            iteration++;
        }
    }

    // void JointStateCallback_left(SensorUnity msg)
    // {
    //     // msg.position

    //     if (Time.time - lastUpdateTime < updateInterval)
    //     {
    //         return;
    //     }
    //     lastUpdateTime = Time.time;

    //     for (int i = 0; i < 6; i++)
    //     {
    //         int idx = actuator_idx[i];
    //         qpos[idx] = msg.position[i];
    //     }
    // }


    // void JointStateCallback_right(SensorUnity msg)
    // {
    //     // msg.position

    //     if (Time.time - lastUpdateTime < updateInterval)
    //     {
    //         return;
    //     }
    //     lastUpdateTime = Time.time;

    //     for (int i = 0; i < 6; i++)
    //     {
    //         int idx = actuator_idx[i + 6];
    //         qpos[idx] = msg.position[i];
    //     }
    // }


    void JointStateCallback(SensorUnity msg)
    {
        // msg.position

        if (Time.time - lastUpdateTime < updateInterval)
        {
            return;
        }
        lastUpdateTime = Time.time;

        for (int i = 0; i < 6+6; i++) // Left Actuator 6, Right Actuator 6
        {
            int idx = actuator_idx[i];
            qpos[idx] = msg.position[i];
        }
    }

    // void HandPoseCallback(Float64MultiArrayMsg msg)
    // {
    //     // 12개를 콜백받음
    //     // for i in range 12:
    //     //  idx = actuator_idx[i];
    //     //  qpos[idx] = callback[i];

    //     int iteration = 0;
    //     if (Time.time - lastUpdateTime < updateInterval)
    //     {
    //         return;
    //     }
    //     lastUpdateTime = Time.time;

    //     foreach (int idx in hand_joint_idx)
    //     {
    //         // int idx = joint_states_order[i];  // 내가 지정한 인덱스
    //         ArticulationBody joint = jointArticulations[idx];
    //         var drive = joint.xDrive;
    //         drive.target = (float)(Mathf.Rad2Deg * msg.data[iteration]); // 라디안 -> 각도
    //         drive.stiffness = 300f; // 목표값에 얼마나 강하게 끌어당길지
    //         drive.damping = 30f;    // 진동이나 떨림을 얼마나 줄일지
    //         drive.forceLimit = 200f; // 최대 적용 가능한 힘      
    //         joint.xDrive = drive;
    //         iteration += 1;
    //     }
    // }
}
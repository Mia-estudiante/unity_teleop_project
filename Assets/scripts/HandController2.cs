using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using SensorUnity = RosMessageTypes.Sensor.JointStateMsg;
using UnityEngine.InputSystem;
using RosMessageTypes.Std;
using System;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

public class HandController2 : MonoBehaviour
{
    ROSConnection ros;
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
    private int[] leftTargetIndex = new int[] {0,2,4,6,9,8};
    private int[] rightTargetIndex = new int[] {0,2,4,6,9,8};

    private double[] lfingers_qpos = new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
    private double[] rfingers_qpos = new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        // if (jointArticulations == null || jointArticulations.Length == 0)
        // {
        //     jointArticulations = GetComponentsInChildren<ArticulationBody>();
        // }
        ros.Subscribe<Float64MultiArrayMsg>("/unity/controller", JointStateCallback);
        ros.RegisterPublisher<Float64MultiArrayMsg>("/unity/hand_joint_states");
    }

    void Update()
    {
       for (int i = 0; i < leftHandJoints.Length; i++)
        {
            ArticulationBody body = leftHandJoints[i];
            var drive = body.xDrive;
            drive.target = (float)(Mathf.Rad2Deg * lfingers_qpos[i]); // 라디안 -> 각도
            drive.stiffness = 300; // 목표값에 얼마나 강하게 끌어당길지
            // drive.stiffness = 200f; // 목표값에 얼마나 강하게 끌어당길지
            drive.damping = 30f;    // 진동이나 떨림을 얼마나 줄일지
            drive.forceLimit = 200f; // 최대 적용 가능한 힘
            // drive.forceLimit = 100f; // 최대 적용 가능한 힘
            body.xDrive = drive;
        }
        for (int i = 0; i < rightHandJoints.Length; i++)
        {
            ArticulationBody body = rightHandJoints[i];
            var drive = body.xDrive;
            drive.target = (float)(Mathf.Rad2Deg * rfingers_qpos[i]); // 라디안 -> 각도
            drive.stiffness = 300; // 목표값에 얼마나 강하게 끌어당길지
            // drive.stiffness = 200f; // 목표값에 얼마나 강하게 끌어당길지
            drive.damping = 30f;    // 진동이나 떨림을 얼마나 줄일지
            drive.forceLimit = 200f; // 최대 적용 가능한 힘
            // drive.forceLimit = 100f; // 최대 적용 가능한 힘
            body.xDrive = drive;
        }
        // PublishHandJointStates(handJointTopicName);
    }

    // pinky, ring, middle, index, thumb_pitch, thumb_yaw
    void JointStateCallback(Float64MultiArrayMsg msg)
    {    
        for (int i = 0; i < 6; i++)
        {
            int targetIdx = leftTargetIndex[i];
            lfingers_qpos[targetIdx] = msg.data[7 + i];
        }
        for (int i = 0; i < 6; i++)
        {
            int targetIdx = rightTargetIndex[i];
            rfingers_qpos[targetIdx] = msg.data[20 + i];
        }

        // pinky -> intermediate
        lfingers_qpos[1] = lfingers_qpos[0] * 1.0f;
        // ring -> intermediate
        lfingers_qpos[3] = lfingers_qpos[2] * 1.0f;
        // middle -> intermediate
        lfingers_qpos[5] = lfingers_qpos[4] * 1.0f;
        // index -> intermediate
        lfingers_qpos[7] = lfingers_qpos[6] * 1.0f;
        // thumb -> intermediate, distal
        lfingers_qpos[10] = lfingers_qpos[9]* 1.6f;
        lfingers_qpos[11] = lfingers_qpos[9]* 2.4f;

        // pinky -> intermediate
        rfingers_qpos[1] = rfingers_qpos[0] * 1.0f;
        // ring -> intermediate
        rfingers_qpos[3] = rfingers_qpos[2] * 1.0f;
        // middle -> intermediate
        rfingers_qpos[5] = rfingers_qpos[4] * 1.0f;
        // index -> intermediate
        rfingers_qpos[7] = rfingers_qpos[6] * 1.0f;
        // thumb -> intermediate, distal
        rfingers_qpos[10] = rfingers_qpos[9]* 1.6f;
        rfingers_qpos[11] = rfingers_qpos[9]* 2.4f;
    }

    void ApplyQposToHand(ArticulationBody[] joints, double[] qpos, int startIndex)
    {
        for (int i = startIndex; i < startIndex + 7; i++)
        {
            var drive = joints[i - startIndex].xDrive;
            drive.target = (float)(Mathf.Rad2Deg * qpos[i]); // 라디안 -> 각도
            drive.stiffness = 300; // 목표값에 얼마나 강하게 끌어당길지
            // drive.stiffness = 200f; // 목표값에 얼마나 강하게 끌어당길지
            drive.damping = 30f;    // 진동이나 떨림을 얼마나 줄일지
            drive.forceLimit = 200f; // 최대 적용 가능한 힘
            // drive.forceLimit = 100f; // 최대 적용 가능한 힘
            joints[i - startIndex].xDrive = drive;
        }
    }
}
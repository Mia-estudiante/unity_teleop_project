using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using SensorUnity = RosMessageTypes.Sensor.JointStateMsg;
using UnityEngine.InputSystem;
using RosMessageTypes.Std;
using System;

public class ArmController : MonoBehaviour
{
    ROSConnection ros;
    // public string jointStateTopic = "/mujoco_ctrl";
    public ArticulationBody[] leftArmJoints;
    public ArticulationBody[] rightArmJoints;

    // 업데이트 간격 제한
    private float updateInterval = 0.05f; // 50ms
    private float lastUpdateTime = 0f;

    bool joint_ok_ = false;
    double[] qpos = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<Float64MultiArrayMsg>("/rci_h12_manager/unity_ctrl", JointStateCallback);
    }

    void Update()
    {
        if (!joint_ok_)
        {
            ApplyQposToArm(leftArmJoints, qpos, 0);
            ApplyQposToArm(rightArmJoints, qpos, 7);
        }
    }

    void JointStateCallback(Float64MultiArrayMsg msg)
    {   
        if (Time.time - lastUpdateTime < updateInterval)
        {
            return;
        }
        lastUpdateTime = Time.time;
        ApplyQposToArm(leftArmJoints, msg.data, 0);
        ApplyQposToArm(rightArmJoints, msg.data, 7);
        joint_ok_ = true;
    }

    void ApplyQposToArm(ArticulationBody[] joints, double[] qpos, int startIndex)
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
            // Debug.Log($"CHECK APPLY{qpos[i]}");
            // Debug.Log($"Index : {i}, drive.target : {drive.target}");
            joints[i - startIndex].xDrive = drive;
        }
    }
}
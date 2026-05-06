using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using SensorUnity = RosMessageTypes.Sensor.JointStateMsg;
using UnityEngine.InputSystem;
using RosMessageTypes.Std;
using System;

public class ArmController2 : MonoBehaviour
{
    ROSConnection ros;
    public ArticulationBody[] leftArmJoints;
    public ArticulationBody[] rightArmJoints;

    // 업데이트 간격 제한
    private float updateInterval = 0.05f; // 50ms
    private float lastUpdateTime = 0f;
    // bool joint_ok_ = false;
    double[] qpos = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };


    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<Float64MultiArrayMsg>("/mujoco/controller", JointStateCallback);
        // ApplyQposToArm(leftArmJoints, qpos, 0);
        // ApplyQposToArm(rightArmJoints, qpos, 7);
    }

    //     void Update()
    // {
    //     if (!joint_ok_)
    //     {
    //         ApplyQposToArm(leftArmJoints, qpos, 0);
    //         ApplyQposToArm(rightArmJoints, qpos, 7);
    //     }
    // }

    void JointStateCallback(Float64MultiArrayMsg msg)
    {   
        if (Time.time - lastUpdateTime < updateInterval)
        {
            return;
        }
        lastUpdateTime = Time.time;
        ApplyQposToArm(leftArmJoints, msg.data, 0);
        ApplyQposToArm(rightArmJoints, msg.data, 7);
        // joint_ok_ = true;
    }

    // void ApplyQposToArm(ArticulationBody[] joints, double[] qpos, int startIndex)
    // {
    //     for (int i = startIndex; i < startIndex + 7; i++)
    //     {
    //         var drive = joints[i - startIndex].xDrive;
    //         drive.target = (float)(Mathf.Rad2Deg * qpos[i]); // 라디안 -> 각도
    //         drive.stiffness = 300; // 목표값에 얼마나 강하게 끌어당길지
    //         // drive.stiffness = 200f; // 목표값에 얼마나 강하게 끌어당길지
    //         drive.damping = 100f;    // 진동이나 떨림을 얼마나 줄일지
    //         drive.forceLimit = 200f; // 최대 적용 가능한 힘
    //         // drive.forceLimit = 100f; // 최대 적용 가능한 힘
    //         Debug.Log($"CHECK APPLY{qpos[i]}");
    //         Debug.Log($"Index : {i}, drive.target : {drive.target}");
    //         joints[i - startIndex].xDrive = drive;
    //     }
    // }

    void ApplyQposToArm(ArticulationBody[] joints, double[] qpos, int startIndex)
    {
        // 안전 장치: 데이터 길이나 배열 크기 확인
        if (qpos.Length < startIndex + 7 || joints.Length < 7)
        {
            Debug.LogWarning($"데이터 부족! qpos: {qpos.Length}, joints: {joints.Length}, start: {startIndex}");
            return;
        }

        for (int i = 0; i < 7; i++)
        {
            int dataIndex = startIndex + i;
            if (joints[i] == null) continue;

            var drive = joints[i].xDrive;
            
            // 라디안을 도(Degree)로 변환하여 할당
            float targetDegree = (float)(qpos[dataIndex] * Mathf.Rad2Deg);
            drive.target = targetDegree;
            
            // 하드코딩보다는 인스펙터 설정을 따르거나 필요시 여기서 조정
            drive.stiffness = 300f;
            drive.damping = 100f;
            drive.forceLimit = 200f;

            joints[i].xDrive = drive;
            
            // 오른쪽 팔(startIndex 7)에 대해서만 로그 찍어보기
            if (startIndex == 7) {
                Debug.Log($"Right Arm Joint {i} (DataIdx {dataIndex}) -> Target: {targetDegree}");
            }
        }
    }
}
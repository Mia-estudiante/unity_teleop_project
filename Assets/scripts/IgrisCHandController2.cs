using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std; // Float64MultiArrayMsg를 사용하기 위해 추가
using System;
using System.Collections.Generic;

public class IgrisCHandController2 : MonoBehaviour
{
    ROSConnection ros;
    private string handJointTopicName = "/mujoco/hand_controller";
    private float updateInterval = 0.01f;
    private float lastUpdateTime = 0f;

    public ArticulationBody[] leftHandJoints;
    public ArticulationBody[] rightHandJoints;
    public SphereManager manager;

    // ★ 중요: 기존 JointStateMsg 대신 Float64MultiArrayMsg로 변경
    private Float64MultiArrayMsg latestMsg;

    Dictionary<string, ArticulationBody> leftMap = new Dictionary<string, ArticulationBody>();
    Dictionary<string, ArticulationBody> rightMap = new Dictionary<string, ArticulationBody>();

    // 파이썬 DexRetargeting에서 들어오는 6개 데이터의 관절 매핑 순서 정의
    // ※ 만약 실행했을 때 손가락이 다르게 움직인다면, 이 배열의 순서를 파이썬 target_joint_names 순서와 일치하도록 변경하세요.
    private string[] jointOrder = new string[]
    {
        "Little_Middle",
        "Ring_Middle",
        "Middle_Middle",
        "Index_Middle",
        "Thumb_Middle",
        "Thumb_Proximal"
    };

    string ExtractKey(string name)
    {
        var parts = name.Split('_');
        return parts[parts.Length - 2] + "_" + parts[parts.Length - 1];
    }

    void BuildJointMap()
    {
        foreach (var joint in leftHandJoints)
        {
            string key = ExtractKey(joint.name); 
            leftMap[key] = joint;
        }

        foreach (var joint in rightHandJoints)
        {
            string key = ExtractKey(joint.name);
            rightMap[key] = joint;
        }
    }

    void InitializeJointDrives(ArticulationBody[] joints)
    {
        if (joints == null) return;

        foreach (var joint in joints)
        {
            if (joint == null) continue;

            var drive = joint.xDrive;

            drive.target =
                joint.jointPosition[0] * Mathf.Rad2Deg;

            drive.stiffness = 200f;
            drive.damping = 250f;
            drive.forceLimit = 80f;

            joint.xDrive = drive;
        }
    }

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        
        // ★ 구독(Subscribe)하는 메시지 타입을 Float64MultiArrayMsg로 변경
        ros.Subscribe<Float64MultiArrayMsg>(handJointTopicName, JointStateCallback);
        
        BuildJointMap();
        InitializeJointDrives(leftHandJoints);
        InitializeJointDrives(rightHandJoints);
    }

    void FixedUpdate()
    {
        if (latestMsg == null) return;

        ApplyHand(latestMsg);
    }

    // ★ 콜백 함수 매개변수 타입을 Float64MultiArrayMsg로 변경
    void JointStateCallback(Float64MultiArrayMsg msg)
    {
        if (manager == null || !manager.bothTriggered)
            return;

        if (Time.time - lastUpdateTime < updateInterval)
            return;

        lastUpdateTime = Time.time;

        latestMsg = msg;
    }

    // ★ 인덱스 기반으로 데이터를 해석하여 손가락에 적용하는 함수
    void ApplyHand(Float64MultiArrayMsg msg)
    {
        // 데이터가 총 12개(왼손 6개 + 오른손 6개) 이상 들어왔는지 검증
        if (msg.data.Length < 12) return;

        // 1. 왼손 제어 (msg.data[0] ~ msg.data[5])
        for (int i = 0; i < 6; i++)
        {
            string key = jointOrder[i];
            double rad = msg.data[i];

            if (leftMap.ContainsKey(key))
            {
                ApplyJoint(leftMap[key], rad);
            }
        }

        // 2. 오른손 제어 (msg.data[6] ~ msg.data[11])
        for (int i = 0; i < 6; i++)
        {
            string key = jointOrder[i];
            double rad = msg.data[i + 6]; // 오른손 데이터는 인덱스 6번부터 시작

            if (rightMap.ContainsKey(key))
            {
                ApplyJoint(rightMap[key], rad);
            }
        }
    }

    void ApplyJoint(ArticulationBody joint, double rad)
    {
        var drive = joint.xDrive;

        // 현재 target
        float current = drive.target;

        float target = (float)(Mathf.Rad2Deg * rad);

        // limit clamp
        // target = Mathf.Clamp(target, 0f, 70f);
        drive.target = Mathf.MoveTowards(
            current,
            target,
            800f * Time.fixedDeltaTime
        );
        // drive.target =
        //     Mathf.Lerp(current, target, 0.08f);

        drive.stiffness = 200f;
        drive.damping = 250f;
        drive.forceLimit = 150;
        joint.xDrive = drive;
    }

    // void ApplyJoint(ArticulationBody joint, double rad)
    // {
    //     var drive = joint.xDrive;
    //     drive.target = (float)(Mathf.Rad2Deg * rad);
        
    //     // drive.stiffness = 500f;
    //     drive.stiffness = 1000f;
    //     drive.damping = 400f;
    //     drive.forceLimit = 150f;
    //     // drive.damping = 200f;
    //     // drive.forceLimit = float.MaxValue;
    //     joint.xDrive = drive;
    // }
}
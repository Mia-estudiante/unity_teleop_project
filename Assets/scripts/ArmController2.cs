using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using SensorUnity = RosMessageTypes.Sensor.JointStateMsg;
using Float64MultiArray = RosMessageTypes.Std.Float64MultiArrayMsg;
// using Unity.Robotics.ROSTCPConnector.MessageGeneration;
// using RosMessageTypes.Std;
using System.Collections.Generic;
using UnityEngine.AI;

public class ArmController2 : MonoBehaviour
{
    ROSConnection ros;
    public string jointStateTopic = "/unity/joint_states";
    public string ctrlTopic = "/unity_ctrl";
    // public ArticulationBody[] leftArmJoints;
    // public ArticulationBody[] rightArmJoints;
    // 업데이트 간격 제한
    private float updateInterval = 0.05f; // 50ms
    private float lastUpdateTime = 0f;
    private int num_joints = 14;
    // public List<float> jointCommands = new List<float>(new float[14]);

    public float[] qpos = new float[51];    // 상태값
    public float[] qvel = new float[51];    // 속도값
    public float[] ctrl = new float[14];    // 제어 명령
    // public List<float> lrPos = new List<float>(new float[14]);
    // public List<float> lrVel = new List<float>(new float[14]);

    public Transform[] leftArmjointTransforms;
    public Transform[] rightArmjointTransforms;

    public float publishRateHz = 100f;  // 100Hz
    private float timeElapsed = 0f;

    private bool initSimulation = true;
    private bool controlFlag = false;
    
    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        // ros.Subscribe<SensorUnity>(jointStateTopic, JointStateCallback);
        ros.Subscribe<Float64MultiArray>(ctrlTopic, CtrlStateCallback);
        ros.RegisterPublisher<SensorUnity>(jointStateTopic);
    }

    void Update()
    {
        timeElapsed += Time.deltaTime;

        if (timeElapsed >= 1.0f / publishRateHz)
        {
            TimerCallback();  // 주기적으로 실행할 함수
            timeElapsed = 0f;
        }
    }

    void TimerCallback()
    {
        // PublishJointStates()
        SensorUnity jointState = new SensorUnity();

        // 조인트의 위치와 속도 계산 (예시로 Z축 회전값을 사용)
        List<double> positions = new List<double>();
        List<double> velocities = new List<double>(); // 속도는 여기선 0

        foreach (Transform joint in leftArmjointTransforms)
        {
            float angle = joint.localEulerAngles.z;
            if (angle > 180f) angle -= 360f; // Unity는 0~360, ROS는 -180~180이 일반적
            positions.Add(Mathf.Deg2Rad * angle);  // 라디안 변환
            velocities.Add(0.0);  // 속도 계산하려면 이전 상태 저장 필요
        }
        foreach (Transform joint in rightArmjointTransforms)
        {
            float angle = joint.localEulerAngles.z;
            if (angle > 180f) angle -= 360f; // Unity는 0~360, ROS는 -180~180이 일반적
            positions.Add(Mathf.Deg2Rad * angle);  // 라디안 변환
            velocities.Add(0.0);  // 속도 계산하려면 이전 상태 저장 필요
        }

        jointState.position = positions.ToArray();
        jointState.velocity = velocities.ToArray();


        ros.Publish(jointStateTopic, jointState);

        if (initSimulation)
        {
            for (int i = 0; i < qpos.Length; i++) qpos[i] = 0f;
            // → 여기에 IK 계산이나 초기 포즈 설정 가능
            initSimulation = false;
        }

        if (controlFlag)
        {
            for (int i = 0; i < 7; i++) qpos[13 + i] = ctrl[i];      // 왼팔
            for (int i = 0; i < 7; i++) qpos[32 + i] = ctrl[7 + i];  // 오른팔

            // 여기에 포워드 키네마틱스 또는 IK 적용
            // UpdateArmPoseFromQpos();
        }
    }

    // void JointStateCallback(ArticulationBody[] joints, double[] qpos)
    // {
    //     for (int i = 0; i < Mathf.Min(joints.Length, qpos.Length); i++)
    //     {
    //         var drive = joints[i].xDrive;
    //         drive.target = Mathf.Rad2Deg * (float)qpos[i];
    //         joints[i].xDrive = drive;
    //     }
    // }

    void CtrlStateCallback(Float64MultiArray msg)
    {
        controlFlag = true;
        for (int i = 0; i < num_joints; i++)
        {
            ctrl[i] = (float)msg.data[i];
        }
    }        
}
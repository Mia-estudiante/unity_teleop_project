using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using SensorUnity = RosMessageTypes.Sensor.JointStateMsg;
using RosMessageTypes.Std;

public class IgrisCArmController2 : MonoBehaviour
{
    ROSConnection ros;

    public ArticulationBody[] leftArmJoints;
    public ArticulationBody[] rightArmJoints;

    private double[] latestQpos;
    private float updateInterval = 0.005f;
    private float lastUpdateTime = 0f;

    private string controllerTopicName = "/mujoco/controller";
    public string jointStateTopicName = "/unity/joint_states";

    public SphereManager manager;

    // ---- Drive 게인 (튜닝 포인트) -------------------------------
    // 이전 20000/8000/MaxValue는 polynomial explosion 유발.
    // 일반 7-DOF arm은 3000~5000 stiffness, 300~500 damping 정도가 출발점.
    [Header("Drive Gains")]
    public float armStiffness = 5000f;
    public float armDamping   = 3000f;
    public float armForceLimit = 50000f;   // MaxValue는 절대 X

    [Header("Target Smoothing")]
    [Range(0f, 1f)]
    public float targetLerp = 1.0f;  // 0=정지, 1=즉시 따라감
    // -------------------------------------------------------------
    

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<Float64MultiArrayMsg>(controllerTopicName, JointStateCallback);
        ros.RegisterPublisher<SensorUnity>(jointStateTopicName);

        // ★ 핵심: 모든 joint를 "현재 자세 hold"로 초기화.
        //   이거 없으면 첫 callback 때 drive가 target=0으로 갑자기 켜져서 폭주.
        InitializeJointDrives(leftArmJoints);
        InitializeJointDrives(rightArmJoints);
    }

    void InitializeJointDrives(ArticulationBody[] joints)
    {
        if (joints == null) return;
        foreach (var joint in joints)
        {
            if (joint == null) continue;
            var drive = joint.xDrive;
            // 현재 joint position을 target으로 (rad → deg). 점프 방지의 핵심.
            drive.target = joint.jointPosition[0] * Mathf.Rad2Deg;
            drive.targetVelocity = 0f;
            drive.stiffness = armStiffness;
            drive.damping = armDamping;
            drive.forceLimit = armForceLimit;
            joint.xDrive = drive;
        }
    }

    void FixedUpdate()
    {
        if (latestQpos == null)
        {
            // 메시지 들어오기 전이라도 joint state 발행은 계속 — Unity → ROS feedback 유지
            PublishJointState();
            return;
        }

        ApplyQposToArm(leftArmJoints, latestQpos, 0);
        ApplyQposToArm(rightArmJoints, latestQpos, 7);
        PublishJointState();
    }

    void PublishJointState()
    {
        int i = 0;
        SensorUnity jointMsg = new SensorUnity();
        jointMsg.position = new double[14];
        jointMsg.velocity = new double[14];
        jointMsg.name = new string[14];

        foreach (var joint in leftArmJoints)
        {
            jointMsg.position[i] = joint.jointPosition[0];
            jointMsg.velocity[i] = joint.jointVelocity[0];
            jointMsg.name[i++] = joint.name;
        }
        foreach (var joint in rightArmJoints)
        {
            jointMsg.position[i] = joint.jointPosition[0];
            jointMsg.velocity[i] = joint.jointVelocity[0];
            jointMsg.name[i++] = joint.name;
        }
        ros.Publish(jointStateTopicName, jointMsg);
    }

    void JointStateCallback(Float64MultiArrayMsg msg) {
        if (manager == null || !manager.bothTriggered) return;
        if (Time.time - lastUpdateTime < updateInterval) return;
        
        double tRecv = Time.realtimeSinceStartupAsDouble * 1000;
        lastUpdateTime = Time.time;
        latestQpos = msg.data;
        
        LatencyLogger.Instance?.Log("unity_ctrl_recv", tRecv);  // 도착 시각만
    }

  void ApplyQposToArm(ArticulationBody[] joints, double[] qpos, int startIndex)
    {
        for (int i = 0; i < 7; i++)
        {
            var joint = joints[i];
            if (joint == null) continue;

            var drive = joint.xDrive;

            float current = drive.target;
            // 무조코의 라디안 각도를 도(Degree) 단위로 변환
            float target = (float)(Mathf.Rad2Deg * qpos[startIndex + i]);

            // ★ [핵심 수정] 4번째 관절(i == 3)이 엘보우(팔꿈치) 관절입니다.
            if (i == 3)
            {
                // 방향을 뒤집고(-) 90도 오프셋(-90)을 적용합니다.
                target = target + 90f;
            }

            // 기존  Lerp 유지 (만약 반응이 느리다면 인스펙터에서 targetLerp를 1에 가깝게 올리세요)
            drive.target = Mathf.Lerp(current, target, targetLerp);

            // 게인 값 적용
            drive.stiffness = armStiffness;
            drive.damping = armDamping;
            drive.forceLimit = armForceLimit;

            joint.xDrive = drive;
        }
    }
}
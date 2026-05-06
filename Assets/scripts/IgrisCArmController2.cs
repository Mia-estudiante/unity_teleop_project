using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using SensorUnity = RosMessageTypes.Sensor.JointStateMsg;
using RosMessageTypes.Std;

public class IgrisCArmController2 : MonoBehaviour
{
    ROSConnection ros;

    public ArticulationBody[] leftArmJoints;
    public ArticulationBody[] rightArmJoints;

    private double[] latestQpos; // 목표값
    private float updateInterval = 0.005f;   // ⭐ 200Hz
    private float lastUpdateTime = 0f;

    private string controllerTopicName = "/mujoco/controller";
    public string jointStateTopicName = "/unity/joint_states";

    public SphereManager manager;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<Float64MultiArrayMsg>(controllerTopicName, JointStateCallback);
        ros.RegisterPublisher<SensorUnity>(jointStateTopicName);
    }

    /* ===============================
       ⭐ 반드시 FixedUpdate 사용
       =============================== */
    void FixedUpdate()
    {
        if (latestQpos == null) return;

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

    /* ===============================
       ROS callback
       =============================== */
    void JointStateCallback(Float64MultiArrayMsg msg)
    {
        // 🔥 trigger 안되면 로봇 멈춤
        if (manager == null || !manager.bothTriggered)
            return;

        if (Time.time - lastUpdateTime < updateInterval)
            return;

        lastUpdateTime = Time.time;
        latestQpos = msg.data;

        ApplyQposToArm(leftArmJoints, msg.data, 0);
        ApplyQposToArm(rightArmJoints, msg.data, 7);
    }

    /* ===============================
       ⭐ 강한 servo 세팅
       =============================== */
    void ApplyQposToArm(ArticulationBody[] joints, double[] qpos, int startIndex)
    {
        for (int i = 0; i < 7; i++)
        {
            var joint = joints[i];
            var drive = joint.xDrive;
            
            float current = joint.xDrive.target;
            float target = (float)(Mathf.Rad2Deg * qpos[startIndex + i]);
            drive.target = Mathf.Lerp(current, target, 0.2f);

            // drive.target = (float)(Mathf.Rad2Deg * qpos[startIndex + i]);

            // 🔥 핵심 튜닝값
            drive.stiffness = 20000f;
            drive.damping = 8000f;
            drive.forceLimit = float.MaxValue;

            joint.xDrive = drive;
        }
    }
}

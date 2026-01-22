using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using SensorUnity = RosMessageTypes.Sensor.JointStateMsg;
using UnityEngine.InputSystem;
using RosMessageTypes.Std;

public class JointStatePublisher : MonoBehaviour
{
    ROSConnection ros;

    // 업데이트 간격 제한
    private float updateInterval = 0.05f; // 50ms
    private float lastUpdateTime = 0f;

    // ROS PUBLISHER
    public string JointStateTopicName = "/rci_h12_manager/joint_states";
    float timeElapsed;
    public float publishRateHz = 20f;
    public ArticulationBody[] jointArticulations;

    // Set Joint Order 
    // Left 7 : Left arm
    // Right 7 : Right arm
    // Total Length : 14
    public int[] arm_joint_states_order = new int[] { 26, 27, 28, 29, 30, 31, 32, 3, 4, 5, 6, 7, 8, 9 }; // shoulder_pitch_link ~ wrist_yaw_link
    
    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<SensorUnity>(JointStateTopicName);
        if (jointArticulations == null || jointArticulations.Length == 0)
        {
            jointArticulations = GetComponentsInChildren<ArticulationBody>();
        }
    }

    void Update()
    {
        float pos, vel;
        SensorUnity h12_joint_states = new SensorUnity();
        h12_joint_states.position = new double[14];
        h12_joint_states.velocity = new double[14];

        // Debug.Log($"arm_joint_states_order.Length: {arm_joint_states_order.Length}");
        // for (int i = 0; i < jointArticulations.Length; i++)
        // {
        //     Debug.Log($"i{i}, name: {jointArticulations[i].name}");
        // }

        for (int i = 0; i < arm_joint_states_order.Length; i++)
        {
            int idx = arm_joint_states_order[i];  // arm joint 에 해당하는 인덱스
            ArticulationBody joint = jointArticulations[idx];
            pos = joint.jointPosition[0];  // 안전한 접근
            vel = joint.jointVelocity[0];  // Angular or linear depending on joint type            

            h12_joint_states.position[i] = pos;
            h12_joint_states.velocity[i] = vel;
            // Debug.Log($"i{i}, name {jointArticulations[idx].name}");
        }

        timeElapsed += Time.deltaTime;
        if (timeElapsed >= 1f / publishRateHz)
        {
            ros.Publish(JointStateTopicName, h12_joint_states);
        }
    }
}
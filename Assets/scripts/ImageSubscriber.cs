using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor; // 반드시 추가

public class ImageSubscriber : MonoBehaviour
{
    ROSConnection ros;
    public RawImage rawImage;
    private Texture2D texture;

    // Publish the cube's position and rotation every N seconds
    // public float publishMessageFrequency = 0.5f;

    // Used to determine how much time has elapsed since the last message was published
    // private float timeElapsed;
    // bool imgCheck = false;

    void Start()
    {
        // start the ROS connection
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<ImageMsg>("/mujoco/camera", CameraCallback);
    }

    void CameraCallback(ImageMsg msg)
    {   
        // 최초 수신 시 Texture 생성
        if (texture == null)
        {
            texture = new Texture2D(
                (int)msg.width,
                (int)msg.height,
                TextureFormat.RGB24,
                false
            );
            rawImage.texture = texture;
        }

        // ROS Image → Texture
        texture.LoadRawTextureData(msg.data);
        texture.Apply();
    }
}
using UnityEngine;
using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry; // PoseMsg, PointMsg, QuaternionMsg
// using Unity.Robotics.Core;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine.XR.Management;
using UnityEngine.XR.Hands;
public class HandsExample : MonoBehaviour
{
    XRHandSubsystem m_HandSubsystem;
    XRHand leftHand;
    XRHand rightHand;

    void Start()
    {
        var handSubsystems = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(handSubsystems);

        for (var i = 0; i < handSubsystems.Count; ++i)
        {
            var handSubsystem = handSubsystems[i];
            if (handSubsystem.running)
            {
                m_HandSubsystem = handSubsystem;
                break;
            }
        }

        // if (m_HandSubsystem != null)
        //     m_HandSubsystem.updatedHands += OnUpdatedHands;
    }
    void Update()
    {
        leftHand = m_HandSubsystem.leftHand;
        // rightHand = subsystem.righttHand;
        if (!leftHand.isTracked)
            return;
        XRHandJoint joint = leftHand.GetJoint(XRHandJointID.Wrist);
        Debug.Log($"joint: {joint}");
    }

    // void OnUpdatedHands(XRHandSubsystem subsystem)
    // {
    //     leftHand = subsystem.leftHand;
    //     // rightHand = subsystem.righttHand;
    //     if (!leftHand.isTracked)
    //         return;
    //     XRHandJoint joint = leftHand.GetJoint(XRHandJointID.Wrist);
    //     Debug.Log($"joint: {joint}");


    // }
    // void OnUpdatedHands(XRHandSubsystem subsystem,
    //     XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags,
    //     XRHandSubsystem.UpdateType updateType)
    // {
    //     switch (updateType)
    //     {
    //         case XRHandSubsystem.UpdateType.Dynamic:
    //             // Update game logic that uses hand data
    //             break;
    //         case XRHandSubsystem.UpdateType.BeforeRender:
    //             // Update visual objects that use hand data
    //             break;
    //     }
    // }
}

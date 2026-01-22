using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.Profiling;
using NUnit.Framework.Interfaces;
using UnityEngine.AI;

public class HandPose : MonoBehaviour
{
    // public XROrigin xrOrigin;
    public Transform cameraTransform;
    // public Transform leftHandTransform;
    // public Transform rightHandTransform;
    public XRHandSubsystem handSubsystem;
    // public XRHand hand; // leftHand 또는 rightHand
    // public XRHandJoint joint;
    public ArticulationBody[] jointArticulations;
    public ArticulationBody[] HandJointActuator;
    int[] jointIndex = { 46, 47, 44, 42, 40, 38, 23, 24, 21, 19, 17, 15 }; // left, right

    void Start()
    {
        // handSubsystem = XRGeneralSettings.Instance.Manager.activeLoader
        //     .GetLoadedSubsystem<XRHandSubsystem>();
        if (jointArticulations == null || jointArticulations.Length == 0)
        {
            jointArticulations = GetComponentsInChildren<ArticulationBody>();
        }
        // ros = ROSConnection.GetOrCreateInstance();
        // ros.RegisterPublisher<PoseArrayMsg>(leftHandTopicName);
        // ros.RegisterPublisher<PoseArrayMsg>(rightHandTopicName);
    }

    // void Update()
    // {
    //     // if (handSubsystem == null) return;
    //     // float pos, vel;
    //     float[] qpos = new float[12];
    //     for (int i = 0; i < 12; i++)
    //     {
    //         qpos[i] = 0.0f;
    //     }
    //     foreach (int idx in jointIndex) {
    //         ArticulationBody joint = jointArticulations[idx];

            
    //     }

    //     for (int i = 0; i < jointArticulations.Length; i++)
    //     {
    //         int idx = jointIndex[i];  // 내가 지정한 인덱스
    //         ArticulationBody joint = jointArticulations[idx];
    //     }
        
        
    // }

    // void ApplyQposToArm(ArticulationBody[] joints, double[] qpos, int startIndex)
    // {
    //     for (int i = startIndex; i < startIndex + 7; i++)
    //     {
    //         var drive = joints[i - startIndex].xDrive;
    //         drive.target = (float)(Mathf.Rad2Deg * qpos[i]); // 라디안 -> 각도
    //         drive.stiffness = 300f; // 목표값에 얼마나 강하게 끌어당길지
    //         drive.damping = 30f;    // 진동이나 떨림을 얼마나 줄일지
    //         drive.forceLimit = 200f; // 최대 적용 가능한 힘

    //         joints[i - startIndex].xDrive = drive;
    //         // Debug.Log($"{joints[i - startIndex].name}: {(float)(Mathf.Rad2Deg * qpos[i - startIndex])})");
    //         //     Debug.Log($"{i}-th joint value {qpos[i - startIndex]})");
    //         // }
    //     }
    // }

}

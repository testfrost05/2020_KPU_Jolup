using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VrFps;
using Valve.VR;

[CustomEditor(typeof(InteractionVolume))]
public class InteractionVolumeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        InteractionVolume IV = (InteractionVolume)target;

        if (GUILayout.Button("Save Hand Offsets"))
        {
            SteamVR_Behaviour_Skeleton[] hands = IV.transform.GetComponentsInChildren<SteamVR_Behaviour_Skeleton>();
            
            foreach(SteamVR_Behaviour_Skeleton hand in hands)
            {
                if(hand.name == "vr_glove_left_model_slim(Clone)")
                {
                    IV.leftPosePositionOffset = hand.transform.localPosition;
                    IV.leftPoseRotationOffset = hand.transform.localRotation.eulerAngles;
                }
                else if (hand.name == "vr_glove_right_model_slim(Clone)")
                {
                    IV.rightPosePositionOffset = hand.transform.localPosition;
                    IV.rightPoseRotationOffset = hand.transform.localRotation.eulerAngles;
                }
            }
        }

        if(!IV.Highlight)
            if (GUILayout.Button("Generate Highlight"))
            {
                IV.CreateHighlight();
            }
    }
}

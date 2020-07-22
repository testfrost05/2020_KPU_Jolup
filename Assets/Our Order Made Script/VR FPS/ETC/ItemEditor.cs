using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VrFps;
using Valve.VR;

[CustomEditor(typeof(Item))]
public class ItemEditor : Editor
{
    protected void SetOffset(Item item, ref Vector3 positionOffset, ref Vector3 rotationOffSet, ref int buttonPressed)
    {
        Transform previousTransform = item.transform.parent;

        if (buttonPressed == 0 || !item.controller)
        {
            ActiveEditorTracker.sharedTracker.isLocked = true;
            GUI.color = Color.green;
            item.controller = new GameObject(string.Format("Controller Prefab", name));
            SteamVR_RenderModel model = item.controller.AddComponent<SteamVR_RenderModel>();
            model.modelOverride = "vr_controller_vive_1_5";

            Transform realOffset = GameObject.Find("Player Rig").transform.GetChild(0).Find("Global Offset Left");

            item.controller.transform.parent = item.transform;
            item.controller.transform.localPosition = Vector3.zero;
            item.controller.transform.localRotation = Quaternion.Inverse(realOffset.localRotation);
            item.controller.transform.parent = null;

            buttonPressed = 1;

            GameObject[] selection = new GameObject[1];
            selection[0] = item.controller;
            Selection.objects = selection;
        }
        else
        {
            GameObject offset = new GameObject(string.Format("Item Offset", name));

            Transform realOffset = GameObject.Find("Player Rig").transform.GetChild(0).Find("Global Offset Left");

            offset.transform.parent = item.controller.transform;
            offset.transform.localPosition = realOffset.localPosition; //new Vector3(0, -0.01f, -0.1f);
            offset.transform.localRotation = realOffset.localRotation; //Quaternion.Euler(60, 0, 0);

            item.transform.parent = offset.transform;

            GUI.color = Color.red;

            GameObject rotationOffset = new GameObject(string.Format("Rotation Offset", name));

            rotationOffset.transform.parent = offset.transform;
            rotationOffset.transform.localPosition = Vector3.zero;
            rotationOffset.transform.localRotation = item.transform.localRotation;

            item.transform.parent = rotationOffset.transform;

            positionOffset = item.transform.localPosition;
            rotationOffSet = Quaternion.Inverse(rotationOffset.transform.localRotation).eulerAngles;

            item.transform.parent = previousTransform;
            DestroyImmediate(item.controller);
            buttonPressed = 0;
            ActiveEditorTracker.sharedTracker.isLocked = false;

            GameObject[] selection = new GameObject[1];
            selection[0] = item.gameObject;
            Selection.objects = selection;
        }
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        Item item = (Item)target;

        string offsetButtonName = item.itemOffsetPressed == 0 ? "Set Controller Offset" : "Save Controller Offset";

        if (GUILayout.Button(offsetButtonName))
        {
            SetOffset(item, ref item.positionOffset, ref item.rotationOffset, ref item.itemOffsetPressed);
        }

        string flippedOffsetButtonName = item.itemFlippedOffsetPressed == 0 ? "Set Flipped Controller Offset" : "Save Flipped Controller Offset";

        if (GUILayout.Button(flippedOffsetButtonName))
        {
            SetOffset(item, ref item.flippedPositionOffset, ref item.flippedRotationOffset, ref item.itemFlippedOffsetPressed);
        }
    }
}



[CustomEditor(typeof(Grenade))]
public class GrenadeEditor : ItemEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }
}

[CustomEditor(typeof(Attachment))]
public class AttachmentEditor : ItemEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }
}



[CustomEditor(typeof(Bullet))]
public class BulletEditor : ItemEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }
}

[CustomEditor(typeof(Magazine))]
public class MagazineEditor : ItemEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }
}


[CustomEditor(typeof(Firearm))]
public class FirearmEditor : ItemEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }
}




[CustomEditor(typeof(BoltAction))]
public class BoltActionEditor : FirearmEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }
}



[CustomEditor(typeof(BreakAction))]
public class BreakActionEditor : FirearmEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }
}

[CustomEditor(typeof(HammerActionPistol))]
public class HammerActionPistolEditor : FirearmEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }
}

[CustomEditor(typeof(Revolver))]
public class RevolverEditor : HammerActionPistolEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }
}

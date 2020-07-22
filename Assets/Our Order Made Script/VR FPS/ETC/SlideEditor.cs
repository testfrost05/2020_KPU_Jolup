using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VrFps;
using Valve.VR;

[CustomEditor(typeof(Slide))]
public class SlideEditor : Editor
{
    //Set different name for SocketSlide

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        Slide slide = (Slide)target;

        string setSlideButtonText = slide is SocketSlide ? "Set Unloaded Position" : "Set Slide Start Position";

        if (GUILayout.Button(setSlideButtonText))
        {
            slide.slideObject = slide is SocketSlide ? slide.GetComponentInChildren<Item>().transform : slide.transform;

            string slidePositionName = slide is SocketSlide ? " Unloaded Position : " : " Slide Start Position : ";

            slide.startPosition = slide.startPosition ? slide.startPosition : new GameObject(string.Format(slidePositionName + slide.name, name)).transform;

            slide.startPosition.parent = slide.transform.parent;
            slide.startPosition.position = slide.slideObject.position;
            slide.startPosition.rotation = slide.slideObject.rotation;
        }

        setSlideButtonText = slide is SocketSlide ? "Set Loaded Position" : "Set Slide End Position";

        if (GUILayout.Button(setSlideButtonText))
        {
            slide.slideObject = slide is SocketSlide ? slide.GetComponentInChildren<Item>().transform : slide.transform;

            string slidePositionName = slide is SocketSlide ? "Loaded Position : " : "Slide End Position : ";

            slide.endPosition = slide.endPosition ? slide.endPosition : new GameObject(string.Format(slidePositionName + slide.name, name)).transform;

            slide.endPosition.parent = slide.transform.parent;
            slide.endPosition.position = slide.slideObject.position;
            slide.endPosition.rotation = slide.slideObject.rotation;
        }

        if (GUILayout.Button("Toggle Slide Position"))
        {
            slide.slideObject = slide as SocketSlide ? slide.GetComponentInChildren<Item>().transform : slide.transform;

            Vector3 tempPosition = slide.slideObject.position == slide.EndPosition ? slide.StartPosition : slide.EndPosition;

            if (tempPosition != Vector3.zero)
                slide.slideObject.position = tempPosition;
            else
                Debug.LogWarning("No slide positions to toggle to.");
        }
    }
}

/*
[CustomEditor(typeof(DoubleSlide))]
public class DoubleSlideEditor : SlideEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }
}
*/

[CustomEditor(typeof(FirearmSlide))]
public class FirearmSlideEditor : SlideEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }
}

[CustomEditor(typeof(FirearmDoubleSlide))]
public class FirearmDoubleSlideEditor : FirearmSlideEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }
}

[CustomEditor(typeof(SocketSlide))]
public class SocketSlideEditor : SlideEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }
}

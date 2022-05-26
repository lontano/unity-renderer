using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapperCamera))]
[CanEditMultipleObjects]
public class MapperCameraEditor : Editor
{
    MapperCamera mapperCamera;


    void OnEnable()
    {
        try
        {
            mapperCamera = (MapperCamera)target;
        }
        catch (System.Exception)
        {

        }
    }


    public override void OnInspectorGUI()
    {
        // Show the custom GUI controls.
        EditorGUILayout.LabelField($"Current state");
        EditorGUILayout.LabelField($"Started = {mapperCamera.Started}");
        EditorGUILayout.LabelField($"Current state={mapperCamera.ParcelState}");

        EditorGUILayout.LabelField($"X position");
        int newX = mapperCamera.currentX;
        newX = EditorGUILayout.IntSlider(newX, mapperCamera.InitialSquare.x, mapperCamera.FinalSquare.x);
        EditorGUILayout.LabelField($"Y position");
        int newY = mapperCamera.currentY;
        newY = EditorGUILayout.IntSlider(newY, mapperCamera.InitialSquare.y, mapperCamera.FinalSquare.y);
        if (newX != mapperCamera.currentX || newY != mapperCamera.currentY)
            mapperCamera.GoToParcel(newX, newY);

        mapperCamera.SideCamera = EditorGUILayout.Toggle("Side camera", mapperCamera.SideCamera);

        if (!mapperCamera.Started)
        {
            if (GUILayout.Button("Start process")) mapperCamera.StartProcess();
        }
        else
        {
            if (GUILayout.Button("Stop process")) mapperCamera.StopProcess();
        }
        if (GUILayout.Button("Take snapshot")) mapperCamera.TakeSnapshot();
        //if (GUILayout.Button("Start")) automatedFlyoverCamera.StartCoroutine();

        // Show default inspector property editor
        DrawDefaultInspector();

    }

    // Custom GUILayout progress bar.
    void ProgressBar(float value, string label)
    {
        // Get a rect for the progress bar using the same margins as a textfield:
        Rect rect = GUILayoutUtility.GetRect(18, 18, "TextField");
        EditorGUI.ProgressBar(rect, value, label);
        EditorGUILayout.Space();
    }
}

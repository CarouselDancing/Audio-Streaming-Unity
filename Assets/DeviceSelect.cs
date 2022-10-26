using System;
using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(WASAPI))]
public class DeviceSelect : Editor
{
    WASAPI mainScript;
    string[] devices;

    private void OnEnable()
    {
        devices = Microphone.devices;
        Array.Sort(devices);
        mainScript = (WASAPI)target;
    }
    public override void OnInspectorGUI()
    {
        mainScript.SelectedDevice = EditorGUILayout.Popup("Output Device", mainScript.SelectedDevice, devices, EditorStyles.popup);
        DrawDefaultInspector();
    }
}

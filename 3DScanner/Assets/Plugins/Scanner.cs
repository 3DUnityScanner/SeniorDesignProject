﻿
using UnityEngine;
using UnityEditor;
using UnityScanner3D.CameraIO;
using System.Collections.Generic;
using System;
using UnityScanner3D.ComputerVision;

public class Scanner : EditorWindow
{
    //Object used to access the camera
    ICamera camera;
    IAlgorithm algorithm = new ColorTrackingAlgorithm();

    //UI Backing Fields
    string cameraName;
    Type cameraType;
    Texture2D colorStream, depthStream;
    bool updateGUI;
    ColorDepthImage cameraImage = null;
    bool isStreaming = false;
    int selectedCameraIndex = 0;
    string statusLabelText = "Idle";
    string streamText = "Start Stream";
    string captureText = "Capture";
    private bool snapEnabled;
    string[] cameraOptions = new string[]
    {
        "Dummy Camera",
        "Intel F200",
        "Kinect",
    };

    //A lookup that maps camera names to classes
    Dictionary<string, Type> cameraNameLookup = new Dictionary<string, Type>
    {
        { "Dummy Camera", typeof(DummyCamera) },
        { "Intel F200", typeof(IntelCamera) },
        { "Kinect", null },
    };

    //Shows the plugin when the user clicks the Window > 3DScanner option
    [MenuItem("Window/3D Scanner")]
    public static void ShowWindow()
    {
        GetWindow(typeof(Scanner));
    }

    void OnGUI()
    {
        GUILayout.BeginVertical();

        //Creates the camera selection drop down menu
        GUILayout.BeginHorizontal();
        selectedCameraIndex = EditorGUILayout.Popup("Choose a Camera", selectedCameraIndex, cameraOptions, EditorStyles.popup);
        GUILayout.EndHorizontal();

        //Buttons and status
        GUILayout.BeginHorizontal();
        bool streamButton = GUILayout.Button(streamText, GUILayout.Width(100));
        bool captureButton = GUILayout.Button(captureText, GUILayout.Width(100));
        GUILayout.FlexibleSpace();
        GUILayout.Box("Status: " + statusLabelText);
        GUILayout.EndHorizontal();

        //Options
        GUILayout.BeginHorizontal();
        snapEnabled = GUILayout.Toggle(snapEnabled, "Snap to Objects");
        GUILayout.EndHorizontal();

        //We interrupt this gross UI stuff to bring you some logic
        if (streamButton)
        {
            //if streaming, stop it
            if (isStreaming)
            {
                isStreaming = false;
                statusLabelText = "Idle";
                streamText = "Start Stream";
                if (camera != null)
                    camera.StopCapture();
            }
            //if not streaming, start it
            else
            {
                isStreaming = true;
                statusLabelText = "Streaming!";
                streamText = "Stop Stream";

                //Create an instance of the camera
                cameraName = cameraOptions[selectedCameraIndex];
                cameraType = cameraNameLookup[cameraName];

                if (cameraType != null)
                {
                    camera = (ICamera)Activator.CreateInstance(cameraType);
                    camera.StartCapture();
                }
            }
        }

        if (captureButton)
        {
            if (camera != null)
            {
                cameraImage = camera.GetImage();
                algorithm.ProcessImage(cameraImage);

                IEnumerable<Shape> poseList = algorithm.GetShapes();

                foreach (Shape p in poseList)
                {
                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.transform.rotation = p.Rotation;
                    cube.transform.position = p.Translation;
                }

                algorithm.ClearShapes();
            }
        }

        //Update ColorDepthImage
        if (camera != null && isStreaming)
        {
            cameraImage = camera.GetImage();

            colorStream = cameraImage.ColorImage;
            depthStream = cameraImage.DepthImage;
        }

        //We now return to our regularly scheduled GUI mess

        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical();
        GUILayout.Label("RGB Stream");
        if (colorStream == null || !isStreaming)
            GUILayout.Box("No RGB Data Detected", GUILayout.MaxWidth(300), GUILayout.MaxHeight(300), GUILayout.MinWidth(50), GUILayout.MinHeight(50));
        else
            GUILayout.Box(colorStream, GUILayout.MaxWidth(300), GUILayout.MaxHeight(300), GUILayout.MinWidth(50), GUILayout.MinHeight(50));
        GUILayout.EndVertical();

        GUILayout.BeginVertical();
        GUILayout.Label("Depth Stream");
        if (depthStream == null || !isStreaming)
            GUILayout.Box("No Depth Data Detected", GUILayout.MaxWidth(300), GUILayout.MaxHeight(300), GUILayout.MinWidth(50), GUILayout.MinHeight(50));
        else
            GUILayout.Box(depthStream, GUILayout.MaxWidth(300), GUILayout.MaxHeight(300), GUILayout.MinWidth(50), GUILayout.MinHeight(50));
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();

        updateGUI = false;
    }

    //Load Necessary resources (point cloud, etc..)
    void Awake()
    {
        //feedTex = new Texture2D(2, 2);
        //feedTex = Resources.Load("scannerBlocks", (typeof(Texture2D))) as Texture2D;
        updateGUI = true;
    }

    private void Update()
    {
        if (isStreaming && camera != null)
        {
            updateGUI = true;
        }

        if (updateGUI)
        {
            EditorUtility.SetDirty(GetWindow(typeof(Scanner)));
        }
    }
}


using UnityEngine;
using UnityEditor;
using UnityScanner3D.CameraIO;
using System.Collections.Generic;
using System;
using UnityScanner3D.ComputerVision;
using System.Diagnostics;

public class Scanner : EditorWindow
{
    //Object used to access the camera
    ICamera camera;
    IAlgorithm algorithm = new ColorTrackingAlgorithm();

    Stopwatch lastCameraUpdate = new Stopwatch();
    int CAPTURELIMIT = 50;

    //UI Backing Fields
    string cameraName;
    string logText = "";
    Type cameraType;
    Texture2D leftStream, rightStream;
    bool updateGUI, showAlgorithm;
    ColorDepthImage cameraImage = null;
    bool isStreaming = false;
    int selectedCameraIndex = 0;
    string statusLabelText = "Idle";
    string streamText = "Start Stream";
    string captureText = "Capture";
    private bool snapEnabled;
    Stack<GameObject> scans = new Stack<GameObject>();
    int scanCount = 0;
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
        bool undoButton = GUILayout.Button("Undo", GUILayout.Width(100));
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
            if (isStreaming && !showAlgorithm)
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
                showAlgorithm = false;
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
                logText = "Running Algorithm: Color Tracking\n\n";

                scanCount++;
                updateCam();
                algorithm.ProcessImage(cameraImage);

                IEnumerable<Shape> poseList = algorithm.GetShapes();
                

                //Set parent for group objects as empty GameObject
                var parent = new GameObject() { name="Scanned Objects "+scanCount };

                Vector3 centerVector = new Vector3();
                bool vFlag = false;
                int i = 0;
                foreach (Shape p in poseList)
                {
                    i++;
                    if (!vFlag)
                    {
                        centerVector = p.Translation;
                        vFlag = true;
                    }
                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.transform.rotation = p.Rotation;
                    cube.transform.position = p.Translation - centerVector;
                    cube.transform.localScale = new Vector3(70, 70, 70);
                    cube.transform.parent = parent.transform;//grouping spawned objects

                    logText += "Object " + i + " :\n";
                    logText += "Position: " + cube.transform.position.ToString() + "\nRotation: " + cube.transform.rotation + "\n\n";
                }

                logText += "Objects Found: " + i + "\n\n";

                scans.Push(parent);//pass to lastScanned for the undo button

                algorithm.ClearShapes();

                showAlgorithm = true;
                statusLabelText = "Showing Algorithm Result";
                streamText = "Start Stream";
            }
        }

        if (undoButton)
        {
            if (scans.Count > 0)
            {
                var lastScanned = scans.Pop();
                DestroyImmediate(lastScanned);
            }
            else
            {
                UnityEngine.Debug.Log("Must have scanned before undoing");
            }
        }

        //Update ColorDepthImage
        if (isStreaming && !showAlgorithm)
            updateCam();

        //We now return to our regularly scheduled GUI mess

        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical();
        GUILayout.Label("RGB Stream");
        if (leftStream == null || !isStreaming)
            GUILayout.Box("", GUILayout.MaxWidth(640 / 2), GUILayout.MaxHeight(480 / 2), GUILayout.MinWidth(640 / 6), GUILayout.MinHeight(480 / 6));
        else
            GUILayout.Box(leftStream, GUILayout.MaxWidth(640 / 2), GUILayout.MaxHeight(480 / 2), GUILayout.MinWidth(640 / 6), GUILayout.MinHeight(480 / 6));
        GUILayout.EndVertical();

        GUILayout.BeginVertical();
        GUILayout.Label("Depth Stream");
        if (rightStream == null || !isStreaming)
            GUILayout.Box("", GUILayout.MaxWidth(640 / 2), GUILayout.MaxHeight(480 / 2), GUILayout.MinWidth(640 / 6), GUILayout.MinHeight(480 / 6));
        else
            GUILayout.Box(rightStream, GUILayout.MaxWidth(640 / 2), GUILayout.MaxHeight(480 / 2), GUILayout.MinWidth(640 / 6), GUILayout.MinHeight(480 / 6));
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();

        GUILayout.TextArea(logText, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

        GUILayout.EndVertical();

        updateGUI = false;
    }

    //Load Necessary resources (point cloud, etc..)
    void Awake()
    {
        updateGUI = true;
    }

    private void updateCam()
    {
        if (camera != null)
        {
            if (lastCameraUpdate.ElapsedMilliseconds > CAPTURELIMIT || cameraImage == null)
            {
                cameraImage = camera.GetImage();

                leftStream = cameraImage.ColorImage;
                rightStream = cameraImage.DepthImage;

                leftStream.Apply();
                rightStream.Apply();

                lastCameraUpdate.Reset();
                lastCameraUpdate.Start();
            }
            else if (!lastCameraUpdate.IsRunning)
                lastCameraUpdate.Start();
        }
    }

    private void Update()
    {
        if (isStreaming && camera != null && !showAlgorithm)
        {
            updateGUI = true;
        }

        if (updateGUI)
        {
            EditorUtility.SetDirty(GetWindow(typeof(Scanner)));
        }
    }
}

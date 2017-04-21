
using UnityEngine;
using UnityEditor;
using UnityScanner3D.CameraIO;
using System.Collections.Generic;
using System;
using UnityScanner3D.ComputerVision;
using System.Diagnostics;

[InitializeOnLoad]
public class Scanner : EditorWindow
{
    private static bool justRecompiled;

    //Object used to access the camera
    ICamera camera;

    IAlgorithm algorithm = new ColorTrackingAlgorithm();

    Stopwatch lastCameraUpdate = new Stopwatch();
    const int CAPTURELIMIT = 100;

    //UI Backing Fields
    string cameraName;
    string logText, statusLabelText, streamText, captureText;
    Type cameraType;
    Texture2D leftStream, rightStream;
    bool updateGUI, showAlgorithm, isStreaming, snapEnabled, showContrast;
    ColorDepthImage cameraImage;
    Stack<GameObject> scans;
    int scanCount;
    int selectedCameraIndex = 0;
    private bool runningAlgorithm;
    private string rightStreamLabel = "Depth Stream";
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

    static Scanner()
    {
        //Clean it up
        justRecompiled = true;
    }

    void OnGUI()
    {
        GUILayout.BeginVertical();

        //Creates the camera selection drop down menu
        GUILayout.BeginHorizontal();
        selectedCameraIndex = EditorGUILayout.Popup("Choose a Camera", selectedCameraIndex, cameraOptions, EditorStyles.popup);
        GUILayout.EndHorizontal();

        //Buttons and status
        bool captureButton = false;
        GUILayout.BeginHorizontal();
        bool streamButton = GUILayout.Button(streamText, GUILayout.Width(100));
        if (GUIKeyDown(KeyCode.Space))
            streamButton = true;
        if (!isStreaming)
            GUI.enabled = false;
        captureButton = GUILayout.Button(captureText, GUILayout.Width(100));
        if (!isStreaming)
            GUI.enabled = true;
        GUILayout.FlexibleSpace();
        bool undoButton = GUILayout.Button("Undo Last", GUILayout.Width(100));
        bool clearButton = GUILayout.Button("Clear All", GUILayout.Width(100));//Implement this
        GUILayout.EndHorizontal();



        //Options
        GUILayout.BeginHorizontal();
        snapEnabled = GUILayout.Toggle(snapEnabled, "Snap to Objects");
        snapEnabled = GUILayout.Toggle(snapEnabled, "Woah Look at");
        snapEnabled = GUILayout.Toggle(snapEnabled, "All These Options");
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        //We interrupt this gross UI stuff to bring you some logic
        if (streamButton)
        {

            //if streaming, stop it
            if (isStreaming)
            {
                showAlgorithm = false;
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

        if (captureButton && camera != null)
                runningAlgorithm = true;

        if (undoButton)
        {
            if (scans.Count > 0)
            {
                var lastScanned = scans.Pop();
                DestroyImmediate(lastScanned);
                scanCount--;
            }
            else
            {
                UnityEngine.Debug.Log("Must have scanned before undoing");
            }
        }

        //Update ColorDepthImage
        if (isStreaming)
            updateCam();

        drawBottom();

        updateGUI = false;
    }

    private void drawBottom()
    {
        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical();
        GUILayout.Label("RGB Stream");
        if (leftStream == null || !isStreaming)
            GUILayout.Box("", GUILayout.MaxWidth(640 / 2), GUILayout.MaxHeight(480 / 2), GUILayout.MinWidth(640 / 6), GUILayout.MinHeight(480 / 6));
        else
            GUILayout.Box(leftStream, GUILayout.MaxWidth(640 / 2), GUILayout.MaxHeight(480 / 2), GUILayout.MinWidth(640 / 6), GUILayout.MinHeight(480 / 6));
        GUILayout.EndVertical();

        GUILayout.BeginVertical();

        if (GUILayout.Button(rightStreamLabel, GUILayout.Width(150)))
        {
            showContrast = !showContrast;
            if (showContrast)
                rightStreamLabel = "Processed Stream";
            else
                rightStreamLabel = "Depth Stream";
        }

        if (leftStream == null || !isStreaming)
            GUILayout.Box("", GUILayout.MaxWidth(640 / 2), GUILayout.MaxHeight(480 / 2), GUILayout.MinWidth(640 / 6), GUILayout.MinHeight(480 / 6));
        else
            GUILayout.Box(rightStream, GUILayout.MaxWidth(640 / 2), GUILayout.MaxHeight(480 / 2), GUILayout.MinWidth(640 / 6), GUILayout.MinHeight(480 / 6));
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();

        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Algorithm Log");
        GUILayout.FlexibleSpace();
        GUILayout.Label("Status: " + statusLabelText);
        GUILayout.EndHorizontal();
        GUILayout.TextArea(logText, GUILayout.MaxHeight(150));
        GUILayout.EndVertical();


        GUILayout.EndVertical();
    }

    //Reset all the goods
    public void OnRecompile()
    {
        //
        if (camera != null)
            camera.StopCapture();
        camera = null;
        algorithm = new ColorTrackingAlgorithm();
        lastCameraUpdate = new Stopwatch();
        logText = "";
        leftStream = null;
        rightStream = null;
        showAlgorithm = false;
        cameraImage = null;
        isStreaming = false;
        statusLabelText = "Idle";
        streamText = "Start Stream";
        captureText = "Place Objects";
        snapEnabled = true;
        scans = new Stack<GameObject>();
        scanCount = 0;
        justRecompiled = false;
        showContrast = false;
        rightStreamLabel = "Depth Stream";
    }

    private void updateCam()
    {
        if (camera != null)
        {
            if (lastCameraUpdate.ElapsedMilliseconds > CAPTURELIMIT || cameraImage == null)
            {
                cameraImage = camera.GetImage();

                leftStream = cameraImage.ColorImage;
                if (showContrast)
                    rightStream = algorithm.PreviewImage(cameraImage);
                else
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

    bool GUIKeyDown(KeyCode key)
    {
        if (Event.current.type == EventType.KeyDown)
            return (Event.current.keyCode == key);
        return false;
    }   

    private void Update()
    {
        if (justRecompiled)
            OnRecompile();

        if (isStreaming && camera != null)
        {
            updateGUI = true;
        }

        if (updateGUI)
        {
            EditorUtility.SetDirty(GetWindow(typeof(Scanner)));
        }

        if (runningAlgorithm)
        {
            logText = "Algorithm: Color Tracking\n";

            scanCount++;
            updateCam();
            algorithm.ProcessImage(cameraImage);

            IEnumerable<Shape> poseList = algorithm.GetShapes();

            //Set parent for group objects as empty GameObject
            var parent = new GameObject() { name = "Scanned Objects " + scanCount };

            Vector3 centerVector = new Vector3();
            bool vFlag = false;
            int i = 0;
            foreach (Shape p in poseList)
            {
                i++;
                if (!vFlag)
                {
                    centerVector = new Vector3(p.Translation.x,0.0f,p.Translation.z);
                    vFlag = true;
                }
                GameObject cube = GameObject.CreatePrimitive( p.Type == ShapeType.Cube ? PrimitiveType.Cube : PrimitiveType.Cylinder);
                cube.transform.rotation = p.Rotation;
                cube.transform.position = p.Translation - centerVector;
                cube.transform.localScale = new Vector3(70, 70, 70);
                cube.transform.parent = parent.transform;//grouping spawned objects

                logText += "Object " + i + " : ";
                logText += "Position: " + cube.transform.position.ToString() + " - Rotation: " + cube.transform.rotation + "\n";
            }

            logText += "Objects Found: " + i + "\n\n";

            scans.Push(parent);//pass to lastScanned for the undo button
            algorithm.ClearShapes();

            showAlgorithm = true;
            statusLabelText = "Showing Algorithm Result";
            runningAlgorithm = false;
            isStreaming = true;
        }
    }
}
